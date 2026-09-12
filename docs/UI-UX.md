# UI and UX

Ghost RDP uses a Windows-native WPF interface with a compact dark visual system, explicit security wording, keyboard accessibility, predictable interaction timing, and low background activity. The UI is designed to remain readable and consistent across the main App, Host diagnostics, and Setup applications.

## Visual system

The standard palette uses a dark window background, slightly elevated panels, restrained borders, high-contrast primary text, muted secondary text, and a blue interaction accent. Semantic success, warning, and danger brushes are reserved for status rather than decoration.

The application window style is applied directly to each concrete WPF window type. This avoids relying on an implicit base `Window` style that can be skipped for derived window classes and previously allowed Windows defaults to leak into the interface.

Ordinary `TextBlock` content receives an explicit application foreground. Labels may override this with the muted semantic brush. Dark cards therefore never depend on the operating-system default text color.

## Controls and icons

Text fields use a rounded dark template with explicit border, caret, text, disabled, and keyboard-focus states. Dropdowns use a Ghost RDP-owned template and popup surface so they do not fall back to a white platform theme inside the dark application.

Sidebar navigation and window branding use project-owned vector geometry/SVG assets. The icons scale cleanly with DPI and avoid bitmap-heavy UI decoration. Primary, secondary, and destructive actions keep the same minimum hit target, focus treatment, and disabled-state behavior.

The UI does not depend on a third-party theme, icon, control, or image-rendering runtime library.

## Layout

Layout rounding and device-pixel snapping remain enabled for production windows. The desktop shell uses a fixed navigation rail and flexible content area, while individual views use scrolling where their form content can exceed the available height.

The interface avoids dense decorative chrome. Panels, borders, and spacing are used to communicate grouping and interaction hierarchy.

## Saved-computer recovery UX

A saved-computer load failure is treated as a data-integrity state, not as an empty list. The main App disables profile mutations and reports that the existing store will not be overwritten.

If `computers.json.bak` exists and passes the same schema/profile validation as the primary store, the App offers one explicit Yes/No recovery dialog after the main window is rendered. The prompt identifies the local backup path and states that the unreadable primary will be preserved separately before restoration. Recovery is never started merely because a backup file exists.

Choosing No leaves the current files untouched and the saved-computer store read-only for the running process. Choosing Yes invokes the Core recovery transaction; after a successful restore the in-memory list and favorite count are rebuilt from the validated recovered profiles and editing is re-enabled. The status text identifies the preserved original path when one was created. A recovery failure leaves editing disabled and reports the error without claiming success.

The prompt is shown at most once per application process. This prevents a persistent corrupted store from causing repeated modal interruptions while still making a valid recovery path visible on the next launch.

## Performance and memory

Ghost RDP does not use continuous UI polling, telemetry workers, an always-on Windows service, or a central network relay. Saved-computer `ListBox` controls enable WPF virtualization and recycling so off-screen rows can reuse item containers rather than keeping a full rendered row tree for every profile.

Saved-computer text search uses a 180 ms UI-thread debounce before applying filtering and sorting. Sort and favorites changes remain immediate. This prevents a full filter/sort/materialization cycle for every intermediate keystroke while keeping search responsive.

The view restores the selected computer after refresh when that UUID remains visible and caches the favorites total instead of recounting the full collection on each view refresh. Filter and sort behavior is isolated in `ComputerProfileViewQuery`, a deterministic component covered by App tests.

Vector icons are preferred to bitmap-heavy decoration. Runtime status, Host diagnostics, and saved-computer recovery availability are evaluated on explicit lifecycle/actions rather than by an aggressive background timer.

Actual process memory depends on Windows, .NET, architecture, DPI, graphics state, and profile count. The project therefore validates behavior and avoids unnecessary allocations rather than claiming a fixed RAM number.

## Accessibility

Keyboard focus remains visible, access keys remain available, important controls keep UI Automation names, and status updates use text in addition to color. Windows High Contrast continues to map application semantic brushes to system colors instead of disabling the user's accessibility setting.

Search debounce does not remove focus from the text field. Selection is restored only when the same computer remains visible, avoiding an unexpected selection change. The recovery prompt uses standard Windows dialog semantics and explicit text rather than color-only status.

## Runtime dependency policy

UI implementation uses only WPF/Windows functionality and repository-owned assets. No third-party UI/theme/icon runtime package is allowed under the current dependency policy. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Screenshot policy

README screenshots must come from a real Ghost RDP Windows build that has passed the corresponding CI/release validation. Generated or design-only mockups may be used as design references only when explicitly labeled as such; they are never presented as runtime evidence. Until authentic captures are committed, repository-owned branding and technical diagrams are used.

## Documentation synchronization

UI changes must update README, CHANGELOG, ROADMAP, Accessibility, Windows, Release documentation, and this file whenever those documents are affected. See [Documentation index](README.md).
