# Features - Pomodoro Time Tracker

## Pomodoro Timer

**Workflow Pattern**
```
Work #1 → Short Break → Work #2 → Short Break →
Work #3 → Short Break → Work #4 → Long Break → (cycle repeats)
```

**Session Start Flow**
1. Select Client (optional, remembers last selection)
2. Select Project from that client (optional, remembers last selection)
3. Enter Objective (required, 90 characters max)
4. Adjust Duration if needed (defaults from settings)
5. Click Start (enabled only when Objective is filled)

**Timer States**
- **Setup**: Configuring new session
- **Running**: Timer actively counting down work period
- **Paused**: Timer stopped, can be resumed
- **WrapUp**: Wrap up period after work completes (allows finishing current thought)
- **Break**: Automatic break period (no pause/stop controls available)

**State Flow**: Setup → Running → WrapUp → Break → Setup (repeat)

**Stop Button Behavior**
When Stop is pressed during a running session:
1. Timer automatically pauses
2. Dialog appears with 3 options:
   - **Resume**: Continue as if only paused
   - **Save**: Save partial session with "Stopped early at XX:XX" note
   - **Discard**: Delete session entirely from database

---

## Wrap Up Period

**Purpose**: Extra time after work period ends to finish your current thought without counting as overtime.

**Behavior**
- Work period ends at intended duration (e.g., 25 minutes)
- **Wrap up notification** plays (gentle sound, low volume)
- **Wrap up period** begins (default: 3 minutes)
- Timer counts down: 3:00 → 2:59 → ... → 0:00
- **Main alarm** plays when wrap up period expires
- Break must start after wrap up period expires

**UI During Wrap Up**
- InfoBar message: "Work time complete! You can continue working during this wrap up period to finish your current thought."
- Session label shows "Wrap Up Period"
- Progress ring resets and counts down wrap up period
- Pause/Resume/Stop buttons remain active

---

## Timer Window

**Design**
- Ultra-compact always-on-top window (150x50 pixels)
- Truly borderless design using `DwmExtendFrameIntoClientArea` and `WM_NCCALCSIZE`
- Horizontal layout: narrow vertically, wide horizontally
- Draggable via entire window surface
- Resizable from all edges and corners

**Features**
- Timer text centered (Consolas, 24pt)
- Objective shown only on hover via tooltip
- Rectangular progress bar (red #E74C3C, 30% opacity)
- **Right-Click Context Menu**:
  - Pause/Resume timer
  - Stop with submenu (Save/Discard/Resume)
  - Add Time (+1, +2, +5 minutes)

---

## Settings

**Timer Durations**
- Work duration: 1-120 minutes (default: 25)
- Short break: manual or auto-calculated (default: 5)
- Long break: manual or auto-calculated (default: 15)
- Long break interval: pomodoros before long break (default: 4)
- Wrap up period: extra time after work ends (default: 3)

**Audio Settings**
- Wrap up notification volume: 0-100% (default: 50)
- Main alarm volume: 0-100% (default: 50)
- Sound selection dropdowns with test buttons

**Auto-Calculate Feature**
- Short break = work duration ÷ 5
- Long break = (work duration ÷ 5) × 3
- Example: 25 min work → 5 min short, 15 min long

---

## Client & Project Management

- Full CRUD operations for Clients and Projects
- Client list with search/filter
- Project list filtered by client
- One-to-many relationship (Client → Projects)
- Optional client association for projects
- Cascade behavior: deleting client sets project's ClientId to NULL

---

## Report View

- Combined statistics from Pomodoro sessions and Time entries
- Time period options: Daily, Weekly, Monthly, Custom date range
- Client and Project filter dropdowns (cascading)
- Summary cards: Total Time, Pomodoro Sessions, Time Entries
- Project breakdown with progress bars

---

## Time Entry Management

- Manual time entry creation
- Separate from Pomodoro sessions
- Tracks start/end time and duration
