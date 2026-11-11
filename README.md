# ST TimerQt — C# Windows Desktop Application

A lightweight, high-precision Windows desktop timer application developed with **C# and WPF on .NET**, styled in accordance with the **StartToday Technologies** branding and design system.

![StartToday Logo](STStopwatch/Assets/st_logo.png)

---

## 1. Branding and Design System

The application strictly reflects StartToday Technologies' visual reference:
- **ST Logo & Emblem:** Featured as the primary identity mark on the frameless window, Windows taskbar, and system tray.
- **Color Palette:**
  - **Clean White Canvas (`#FFFFFF`):** High-contrast, pristine background cards.
  - **Pale-Blue Panels (`#F0F4F8` / `#EAF0F6`):** Soft, elegant structural containers and data pill badges.
  - **Crisp Black Borders (`#111111`):** Distinctive 1–1.5px architectural outline framing cards and interactive buttons.
  - **Vivid Blue Accent (`#0052FF`):** High-energy cobalt accent for primary actions, active highlights, and progress tracking.
- **Typography Hierarchy:**
  - **Headings & Titles:** Bold italic serif (`Georgia, Cambria, serif`, Bold & Italic) delivering a refined editorial feel.
  - **Body & Tabular Digits:** Clean geometric sans-serif (`Segoe UI`, Bold / SemiBold) with fixed tabular proportions for the timer display.
- **Minimalist Aesthetic:** Clean whitespace, no distracting cartoonish animations, gradients, or decorative clutter.

---

## 2. Timer Interface & Features

### Single Timer Interface
- **One Mode Only:** A clean circular stopwatch with an adjustable target duration.
- **Circular Progress Ring:** Large outer ring displaying elapsed progress toward the configured target duration.
- **Inside the Ring:**
  - **Session Label:** Displayed in bold italic serif above the time (e.g., `Session 01`).
  - **Prominent Elapsed Time:** Formatted in high-legibility geometric sans-serif digits (`HH:mm:ss`).
  - **Target Subtext:** Subtle target readout below the timer (e.g., `target 01:00:00`).
- **Session Indicator:** Minimalist pill showing the current session number from **01 to 12** (`01 / 12`), with no extraneous dates or time zones.
- **Dual Display Modes:**
  - **Normal Mode:** Full interface with header controls, session badge, circular ring, playback controls, and quick access to statistics.
  - **Minimal Mode:** Sleek, floating desktop widget hiding window chrome while preserving the circular ring, session label, timer display, and 3 essential controls.

### Continuous Duration Adjustment & Full Revolution Calibration
- **Setting Mode (At the Beginning):**
  - When the stopwatch is stopped at zero (`Elapsed == 00:00:00`), the circular adjustment node is **draggable**.
  - Dragging the node around the ring smoothly adjusts the target duration from **0 to 16 hours** (snapped to 1-minute precision).
  - A dynamic floating badge displays the selected target (e.g., `Target: 02h 30m`).
  - Scrolling the mouse wheel over the ring also fine-tunes the target duration.
- **Active Session Mode (After Selection & Running/Paused):**
  - Once started, the ring duration is updated so **one full revolution ($360^\circ$) precisely matches the selected target duration**.
  - The node starts at the **start** ($0^\circ$, 12 o'clock) and travels smoothly along the ring towards the **end** ($360^\circ$).
  - When elapsed time reaches the target duration, the node has completed **one full revolution**, allowing the user to instantly see what part of the duration is completed (e.g. 25%, 50%, 75%, 100%).
  - **Node Locking:** The node is locked and **not draggable** while running or paused, ensuring the target duration cannot be accidentally modified during a session.
  - Clicking **Reset** resets elapsed time to `00:00:00` and immediately unlocks the node back into Setting Mode.

### Three Essential Controls (Icon-Only Design)
The three controls feature a minimalist, icon-only layout without any text:
1. **Reset Button (`↺`):** Clean white rounded button with crisp black border resetting elapsed time back to zero and unlocking duration setting.
2. **Start / Pause / Resume Button:** Primary StartToday vivid blue button (`#0052FF`) with crisp white vector icons:
   - Stopped / Paused $\rightarrow$ **`▶`** (Play triangle)
   - Running $\rightarrow$ **`❚❚`** (Two vertical pause bars)
3. **Next Session Button (`⏭`):** Pale blue rounded button (`#F0F4F8`) with crisp next-track glyph advancing to the next session (01 $\rightarrow$ 12). Automatically commits the completed session to history before advancing.

### 12 Sessions with Distinct Colours
Each of the 12 sessions possesses its own distinctive theme colour from the StartToday palette:
1. **Session 01:** `#0052FF` (StartToday Vivid Cobalt)
2. **Session 02:** `#00A389` (Persian Teal)
3. **Session 03:** `#6366F1` (Radiant Indigo)
4. **Session 04:** `#FF5C35` (Coral Tangerine)
5. **Session 05:** `#059669` (Emerald Forest)
6. **Session 06:** `#7C3AED` (Royal Violet)
7. **Session 07:** `#D97706` (Solar Amber)
8. **Session 08:** `#0284C7` (Electric Azure)
9. **Session 09:** `#E11D48` (Crimson Rose)
10. **Session 10:** `#475569` (Slate Titanium)
11. **Session 11:** `#16A34A` (Vibrant Spring)
12. **Session 12:** `#C026D3` (Vivid Fuchsia)

Advancing sessions dynamically shifts the ring stroke, session badges, and UI accents to the session's assigned color.

---

## 3. High-Resolution Monotonic Timing Engine

To ensure timing accuracy remains independent of UI thread load, background throttling, or window minimization:
- Uses `System.Diagnostics.Stopwatch.GetTimestamp()` referencing hardware `QueryPerformanceCounter` clocks.
- Wall-clock monotonic elapsed time is calculated mathematically at any instant:
  $$\Delta t = \frac{\text{Timestamp}_{\text{current}} - \text{Timestamp}_{\text{start}}}{\text{Frequency}_{\text{counter}}}$$
- UI rendering operates on an independent `DispatcherTimer` polling the monotonic engine at ~30 FPS.
- Zero CPU consumption when paused or stopped.

---

## 4. Statistics

Accessible via the header button (📊) in Normal Mode, providing persistent tracking across application launches:

### Day View
- **Date Selector:** `<` `>` arrows and `Today` quick jump button.
- **Daily Metrics Cards:** Total Time Recorded, Total Sessions, and Average Session Duration.
- **Session Results Table:** Chronological listing of sessions for the selected day:
  - Session badge (`Session 01`–`12`)
  - Time window (e.g. `09:15 - 10:45`)
  - Target duration
  - Actual elapsed duration
  - Completion status badge (`Completed` / `% Done`)

### Month View
- **Month Selector:** `<` `>` arrows and `Current` month button.
- **Monthly Metrics Cards:** Month Total Duration, Active Days Count, and Daily Average.
- **Grouped Date Table:** Shows each active day in the month with its session count and daily total duration.
- **Dedicated Week Total Column:** Displays the cumulative total duration for that entire calendar week (e.g., `W44: 12:30:00`).
- **Weekly Summary Breakdown:** Comprehensive rollup of each week in the month.

---

## 5. System Tray & Window Controls

- **Frameless Floating Window:** Drag any empty border or header to reposition.
- **Always on Top (📌):** Pins the stopwatch floating window on top of code editors, IDEs, and browser windows.
- **System Tray (`app.ico`):**
  - Left/Double-click to restore window.
  - Right-click context menu: `Show ST TimerQt`, `Minimal Mode`, `Always on Top`, `Exit`.
  - Target achievement balloon notification when session goal is completed.
- **Local Persistence:** Automatically preserves window coordinates, mode states, active session number, custom target durations, and session logs in `%APPDATA%\StartToday\STStopwatch\`.

---

## 6. Project Structure

```
c:\Users\sekoz\StartToday\
├── STStopwatch.sln                      # Visual Studio Solution
├── build.bat                            # Double-clickable Windows build script
├── build.ps1                            # PowerShell automated build & test script
├── README.md                            # Comprehensive documentation
│
├── STStopwatch.Core/                    # .NET Framework 4.8 class library
│   ├── STStopwatch.Core.csproj
│   ├── Common/
│   │   ├── ObservableObject.cs          # MVVM INotifyPropertyChanged base
│   │   └── RelayCommand.cs               # ICommand implementation
│   ├── Models/                           # Session, settings, record and statistics data
│   ├── Services/                         # Timer engine and JSON storage abstractions
│   └── ViewModels/                       # Stopwatch and statistics application logic
│
├── STStopwatch/                         # Main WPF Application Project (references Core)
│   ├── STStopwatch.csproj
│   ├── App.xaml / App.xaml.cs           # Application entry & Tray lifecycle
│   ├── MainWindow.xaml / .xaml.cs       # Frameless floating container
│   ├── Assets/
│   │   ├── app.ico                      # Multi-resolution ST application icon
│   │   └── st_logo.png                  # High-res StartToday logo
│   ├── Common/
│   │   └── Converters.cs                # UI value converters (Visibility, Brush, Format)
│   ├── Controls/
│   │   ├── CircularProgressRing.xaml    # Circular arc and interactive drag node
│   │   └── CircularProgressRing.xaml.cs
│   ├── Services/
│   │   ├── FreeRegionFinderService.cs   # Windows screen-positioning helper
│   │   └── TrayService.cs               # Windows notification-area integration
│   ├── Views/
│   │   ├── DayViewControl.xaml / .cs    # Day view statistics table
│   │   ├── MonthViewControl.xaml / .cs  # Month view table with week total column
│   │   └── StatisticsView.xaml / .cs    # Segmented statistics container
│   ├── Styles/
│   │   └── BrandTheme.xaml              # StartToday color palette & control styles
│   └── Properties/
│       └── AssemblyInfo.cs              # Assembly branding & metadata
│
└── STStopwatch.Tests/                   # Automated Verification Test Suite
    ├── STStopwatch.Tests.csproj
    └── Program.cs                       # Tests reference STStopwatch.Core, not the WPF executable
```

---

## 7. Build and Run Instructions

### Prerequisites
- Windows 10 / 11 / Server.
- Any standard .NET environment:
  - Native Windows .NET Framework 4.8 MSBuild (built into Windows under `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`)
  - OR modern Visual Studio 2019 / 2022.

### One-Click Build & Test (Batch)
Double-click or execute from Command Prompt:
```cmd
build.bat
```

### PowerShell Build
```powershell
.\build.ps1 -Configuration Release
```

### Direct MSBuild Command
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" STStopwatch.sln /p:Configuration=Release
```

The solution builds `STStopwatch.Core.dll` separately. Both the WPF application and the test project reference this class library; the test project does not reference the application executable.

### Running the Application
Launch the compiled executable directly:
```cmd
STStopwatch\bin\Release\STTimerQt.exe
```
Or for debug mode:
```cmd
STStopwatch\bin\Debug\STTimerQt.exe
```

### Running Automated Tests
```powershell
& "STStopwatch.Tests\bin\Release\STStopwatch.Tests.exe"
```
Output:
```
==================================================
ST TimerQt - Comprehensive Verification Test Suite
==================================================
--> Testing HighResolutionTimerEngine... [All Passed]
--> Testing 12-Session Definitions and Colours... [All Passed]
--> Testing JsonStorageService persistence... [All Passed]
--> Testing MainViewModel controls and session advancement... [All Passed]
--> Testing Statistics Day and Month Views... [All Passed]
--> Testing Target Duration Adjustment up to 16 Hours... [All Passed]
--------------------------------------------------
Test Results: 111 Passed, 0 Failed
--------------------------------------------------
```

---

## 8. Persistence Data Locations

User settings and historical session logs are saved locally in JSON format at:
```
%APPDATA%\StartToday\STStopwatch\
├── settings.json       # Window position, minimal mode, topmost, target durations
└── sessions.json       # Complete history of completed and logged sessions
```
Writes are performed atomically via staging files to prevent data corruption during unexpected shutdowns.
