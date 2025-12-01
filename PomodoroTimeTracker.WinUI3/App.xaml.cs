using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;
using PomodoroTimeTracker.Infrastructure.Repositories;

namespace PomodoroTimeTracker.WinUI3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Microsoft.UI.Xaml.Application
    {
        private readonly IHost _host;
        internal Window? _window;

        public static IServiceProvider Services => ((App)Current)._host.Services;

        public static T GetService<T>() where T : class
        {
            return Services.GetRequiredService<T>();
        }

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug(); // Output to Visual Studio Debug window
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configure database
                    var appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "PomodoroTimeTracker"
                    );
                    Directory.CreateDirectory(appDataPath);
                    var dbPath = Path.Combine(appDataPath, "pomodoro.db");

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));

                    // Register Unit of Work and Repositories
                    services.AddScoped<IUnitOfWork, UnitOfWork>();
                    services.AddScoped<IClientRepository, ClientRepository>();
                    services.AddScoped<IProjectRepository, ProjectRepository>();
                    services.AddScoped<IPomodoroSessionRepository, PomodoroSessionRepository>();
                    services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();

                    // Register Application Services
                    services.AddScoped<IClientService, ClientService>();
                    services.AddScoped<IProjectService, ProjectService>();
                    services.AddScoped<IPomodoroSessionService, PomodoroSessionService>();
                    services.AddScoped<ITimeEntryService, TimeEntryService>();
                    services.AddScoped<IStatisticsService, StatisticsService>();
                    services.AddScoped<IPomodoroSettingsService, PomodoroSettingsService>();

                    // Register ViewModels
                    services.AddTransient<ViewModels.MainWindowViewModel>();
                    services.AddTransient<ViewModels.ClientListViewModel>();
                    services.AddTransient<ViewModels.ClientDetailViewModel>();
                    services.AddTransient<ViewModels.ProjectListViewModel>();
                    services.AddTransient<ViewModels.ProjectDetailViewModel>();
                    services.AddTransient<ViewModels.TimeEntryListViewModel>();
                    services.AddTransient<ViewModels.TimeEntryDetailViewModel>();
                    services.AddTransient<ViewModels.PomodoroSettingsViewModel>();
                    services.AddTransient<ViewModels.PomodoroViewModel>();

                    // Register UI services
                    services.AddSingleton<Services.INavigationService, Services.NavigationService>();
                    services.AddSingleton<Services.IDialogService, Services.DialogService>();
                    services.AddSingleton<IAudioService, Services.AudioService>();
                    services.AddTransient<IDispatcherTimer, Services.DispatcherTimerWrapper>();
                })
                .Build();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            var logger = Services.GetRequiredService<ILogger<App>>();

            try
            {
                logger.LogInformation("Application starting...");
                await _host.StartAsync();

                // Apply database migrations
                logger.LogInformation("Applying database migrations...");
                using (var scope = _host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Database.MigrateAsync();
                }
                logger.LogInformation("Database migrations completed successfully");

                // Create and activate main window
                _window = new MainWindow();
                _window.Activate();
                logger.LogInformation("Application started successfully");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Fatal error during application startup");

                // Show error to user
                var dialogService = Services.GetService<Services.IDialogService>();
                if (dialogService != null)
                {
                    await dialogService.ShowErrorAsync(
                        "A critical error occurred during startup. Please check the logs and try restarting the application.",
                        "Startup Error");
                }
                throw;
            }
        }
    }
}
