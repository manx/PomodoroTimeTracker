using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PomodoroTimeTracker.WinUI3.ViewModels;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;

namespace PomodoroTimeTracker.WinUI3.Views;

/// <summary>
/// Compact always-on-top timer window with time-timer style progress meter.
/// </summary>
public sealed partial class TimerWindow : WindowEx
{
    // Win32 API constants for window messages
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_SIZING = 0x0214;
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_NCRBUTTONDOWN = 0x00A4;
    private const int WM_NCRBUTTONUP = 0x00A5;
    private const int WM_PARENTNOTIFY = 0x0210;
    private const int WM_POINTERDOWN = 0x0246;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    // RECT structure for WM_SIZING
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    // NCCALCSIZE_PARAMS structure
    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public RECT rgrc0;
        public RECT rgrc1;
        public RECT rgrc2;
        public IntPtr lppos;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // DWM window attributes
    private const int DWMWA_NCRENDERING_ENABLED = 1;
    private const int DWMWA_NCRENDERING_POLICY = 2;
    private const int DWMWA_CAPTION_BUTTON_BOUNDS = 5;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;

    // DWM non-client rendering policy values
    private const int DWMNCRP_USEWINDOWSTYLE = 0;
    private const int DWMNCRP_DISABLED = 1;
    private const int DWMNCRP_ENABLED = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private const int GWL_WNDPROC = -4;
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    // Window styles
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_SYSMENU = 0x00080000;
    private const int WS_BORDER = 0x00800000;
    private const int WS_DLGFRAME = 0x00400000;

    // Extended window styles
    private const int WS_EX_DLGMODALFRAME = 0x00000001;
    private const int WS_EX_WINDOWEDGE = 0x00000100;
    private const int WS_EX_CLIENTEDGE = 0x00000200;
    private const int WS_EX_STATICEDGE = 0x00020000;
    private const int WS_EX_COMPOSITED = 0x02000000;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate? _wndProcDelegate;
    private IntPtr _oldWndProc;
    private IntPtr _hWnd;
    private MenuFlyout? _stopSubmenu;

    public PomodoroViewModel ViewModel { get; }

    public TimerWindow(PomodoroViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        // Subscribe to progress changes to update the visual arc
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Add right-tapped handler for context menu
        RootGrid.RightTapped += RootGrid_RightTapped;

        InitializeWindow();
    }

    private void RootGrid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Update the Pause menu item text based on current state
        PauseMenuItem?.Text = ViewModel.IsPausedState ? "Resume" : "Pause";

        // Context menu will show automatically via Grid.ContextFlyout
    }

    private void PauseMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Execute the pause/resume command
        if (ViewModel.PauseResumeCommand.CanExecute(null))
        {
            ViewModel.PauseResumeCommand.Execute(null);
        }
    }

    private void StopMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Stop the timer (pause it)
        if (ViewModel.State == PomodoroState.Running && ViewModel.PauseResumeCommand.CanExecute(null))
        {
            ViewModel.PauseResumeCommand.Execute(null);
        }

        // Calculate elapsed time for the menu text
        var elapsedSeconds = ViewModel.ElapsedSeconds;
        var minutes = elapsedSeconds / 60;
        var seconds = elapsedSeconds % 60;

        // Hide the main context menu first
        if (RootGrid.ContextFlyout is MenuFlyout mainMenu)
        {
            mainMenu.Hide();
        }

        // Create the submenu dynamically
        _stopSubmenu = new MenuFlyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
        };

        var saveMenuItem = new MenuFlyoutItem
        {
            Text = $"Save {minutes:D2}:{seconds:D2} work item"
        };
        saveMenuItem.Click += StopAndSave_Click;
        _stopSubmenu.Items.Add(saveMenuItem);

        var discardMenuItem = new MenuFlyoutItem
        {
            Text = "Discard work item"
        };
        discardMenuItem.Click += StopAndDiscard_Click;
        _stopSubmenu.Items.Add(discardMenuItem);

        var resumeMenuItem = new MenuFlyoutItem
        {
            Text = "Resume"
        };
        resumeMenuItem.Click += StopAndResume_Click;
        _stopSubmenu.Items.Add(resumeMenuItem);

        // Show the submenu at the same location (RootGrid) where the main menu was
        _stopSubmenu.ShowAt(RootGrid);
    }

    private async void StopAndSave_Click(object sender, RoutedEventArgs e)
    {
        // Save the session (stop with save)
        await ViewModel.SaveAndStopAsync();
    }

    private async void StopAndDiscard_Click(object sender, RoutedEventArgs e)
    {
        // Discard the session
        await ViewModel.DiscardAndStopAsync();
    }

    private void StopAndResume_Click(object sender, RoutedEventArgs e)
    {
        // Resume the timer (just unpause if paused, otherwise do nothing)
        if (ViewModel.IsPausedState && ViewModel.PauseResumeCommand.CanExecute(null))
        {
            ViewModel.PauseResumeCommand.Execute(null);
        }
    }

    private void AddOneMinute_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddMinutes(1);
    }

    private void AddTwoMinutes_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddMinutes(2);
    }

    private void AddFiveMinutes_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddMinutes(5);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Handle right-click messages to show context menu
        // WM_NCRBUTTONUP because SetTitleBar treats the area as non-client
        // WM_PARENTNOTIFY when child controls receive right-click
        if (msg == WM_RBUTTONDOWN || msg == WM_NCRBUTTONDOWN || msg == WM_NCRBUTTONUP ||
            (msg == WM_PARENTNOTIFY && (wParam.ToInt32() & 0xFFFF) == WM_RBUTTONDOWN))
        {
            // Dispatch to UI thread to show the context menu
            DispatcherQueue.TryEnqueue(() =>
            {
                // Get the menu flyout from RootGrid
                if (RootGrid.ContextFlyout is MenuFlyout menuFlyout)
                {
                    // Update the Pause menu item text
                    if (PauseMenuItem != null)
                    {
                        PauseMenuItem.Text = ViewModel.IsPausedState ? "Resume" : "Pause";
                    }

                    // Show the menu at the pointer position
                    menuFlyout.ShowAt(RootGrid);
                }
            });

            // Let the default handler process it too
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        // Handle sizing to enforce square aspect ratio
        if (msg == WM_SIZING)
        {
            // Get the proposed window rectangle
            var rect = Marshal.PtrToStructure<RECT>(lParam);

            // Calculate the larger dimension
            var size = Math.Max(rect.Width, rect.Height);

            // Adjust the rectangle to be square based on which corner is being dragged
            var edge = wParam.ToInt32();

            // Determine which edges to adjust based on resize direction
            switch (edge)
            {
                case 1: // WMSZ_LEFT
                case 4: // WMSZ_TOPLEFT
                case 7: // WMSZ_BOTTOMLEFT
                    rect.Left = rect.Right - size;
                    rect.Bottom = rect.Top + size;
                    break;

                case 2: // WMSZ_RIGHT
                case 5: // WMSZ_TOPRIGHT
                case 8: // WMSZ_BOTTOMRIGHT
                    rect.Right = rect.Left + size;
                    rect.Bottom = rect.Top + size;
                    break;

                case 3: // WMSZ_TOP
                case 6: // WMSZ_BOTTOM
                    // For top/bottom only (shouldn't happen with corner-only resize)
                    rect.Right = rect.Left + size;
                    rect.Bottom = rect.Top + size;
                    break;
            }

            // Write the adjusted rectangle back
            Marshal.StructureToPtr(rect, lParam, true);
            return new IntPtr(1); // TRUE - we handled it
        }

        // Intercept hit testing for dragging and corner-only resizing
        if (msg == WM_NCHITTEST)
        {
            // Let the default handler run first to get resize handles
            var result = CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
            var hitTest = result.ToInt32();

            // Block edge resize handles, only allow corners
            if (hitTest == HTLEFT || hitTest == HTRIGHT || hitTest == HTTOP || hitTest == HTBOTTOM)
            {
                // Convert edge hits to caption (allows dragging instead of resizing)
                return new IntPtr(2); // HTCAPTION
            }

            // If it's client area, make it draggable
            if (hitTest == 1) // HTCLIENT
            {
                return new IntPtr(2); // HTCAPTION - allows dragging
            }

            // Allow corner resizing and everything else
            return result;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private void InitializeWindow()
    {
        // WinUIEx solution for transparent borderless window
        _hWnd = WindowNative.GetWindowHandle(this);

        // Disable window border using WinUIEx
        HwndExtensions.ToggleWindowStyle(_hWnd, false, WindowStyle.TiledWindow);

        // Set transparent backdrop
        this.SystemBackdrop = new TransparentTintBackdrop();

        // Configure window properties using WinUIEx helper methods
        this.IsAlwaysOnTop = true;
        this.IsResizable = true;
        this.IsMaximizable = false;
        this.IsMinimizable = false;

        // Set initial size
        this.Width = 200;
        this.Height = 200;

        // Position in top-right corner
        var displayArea = DisplayArea.Primary;
        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var x = workArea.Width - 220; // 200px width + 20px margin
            var y = 20; // 20px from top
            this.Move(x, y);
        }

        // Use WinUI 3's built-in dragging support
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(RootGrid); // Make entire window draggable

        // Hook into Win32 window procedure for square aspect ratio and corner resize
        _wndProcDelegate = new WndProcDelegate(WndProc);
        _oldWndProc = GetWindowLongPtr(_hWnd, GWL_WNDPROC);
        SetWindowLongPtr(_hWnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
    }


    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update the progress arc when progress percentage changes
        if (e.PropertyName == nameof(ViewModel.ProgressPercentage))
        {
            UpdateProgressArc();
        }
    }

    private void UpdateProgressArc()
    {
        // Time Timer style: arc decreases clockwise from top (12 o'clock position)
        // At 100% (start), full circle is shown
        // At 0% (end), no arc is shown

        var percentage = ViewModel.ProgressPercentage;
        var angle = (percentage / 100.0) * 360.0; // Convert to degrees

        // Build the geometry programmatically using WinUI 3 API
        var geometry = BuildArcGeometry(90, 90, 80, angle);
        ProgressPath.Data = geometry;
    }

    private PathGeometry BuildArcGeometry(double centerX, double centerY, double radius, double angleDegrees)
    {
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure();

        // If angle is 0 or less, show nothing (empty path)
        if (angleDegrees <= 0)
        {
            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        // Convert to radians (start from top, -90 degrees offset)
        var startAngleRad = -Math.PI / 2; // -90 degrees (12 o'clock)
        var endAngleRad = startAngleRad + (angleDegrees * Math.PI / 180.0);

        // Calculate start and end points
        var startX = centerX + radius * Math.Cos(startAngleRad);
        var startY = centerY + radius * Math.Sin(startAngleRad);
        var endX = centerX + radius * Math.Cos(endAngleRad);
        var endY = centerY + radius * Math.Sin(endAngleRad);

        // Start the path at the start point
        pathFigure.StartPoint = new Point(startX, startY);

        // Add arc segment
        var arcSegment = new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = angleDegrees > 180
        };
        pathFigure.Segments.Add(arcSegment);

        // Add line to center
        var lineSegment = new LineSegment { Point = new Point(centerX, centerY) };
        pathFigure.Segments.Add(lineSegment);

        // Close the path
        pathFigure.IsClosed = true;

        pathGeometry.Figures.Add(pathFigure);
        return pathGeometry;
    }
}
