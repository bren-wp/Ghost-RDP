# Accessibility

Ghost RDP is a native WPF Windows application and treats keyboard access, visible focus, assistive-technology metadata, Windows accessibility settings, and predictable interaction timing as product behavior rather than decorative polish.

## Implemented behavior

- primary, secondary, destructive, sidebar, text-entry, selection, checkbox, and list-item controls expose a visible keyboard focus indicator;
- navigation and common actions use Windows access-key conventions so they can be reached without a mouse;
- important controls expose descriptive `AutomationProperties.Name` values for UI Automation clients and screen readers;
- status regions that change after validation or settings actions use polite live announcements where appropriate;
- control hit areas have practical minimum heights rather than relying on text-only bounds;
- concrete App, Host, Setup, and profile-editor WPF windows receive the intended foreground/background style directly rather than depending on base-class implicit-style lookup;
- ordinary text has an explicit semantic foreground, preventing unreadable Windows-default black text on dark Ghost RDP panels;
- text fields and dropdowns provide explicit dark, focused, disabled, selected, highlighted, and popup states instead of depending on an unrelated platform theme;
- Windows High Contrast is detected through `SystemParameters.HighContrast` and the main application switches its dynamic palette to Windows system colors;
- High Contrast changes are observed while the main window is running and do not require Ghost RDP to disable or override the Windows setting;
- state is communicated in text as well as color (for example Available/Unavailable, Ready/Warning, Enabled/Disabled).

## Keyboard model

Buttons retain normal WPF Enter/Space activation. Underscored access-key labels provide Alt-key access to navigation and common actions. Saved-computer operations remain available as explicit buttons, so mouse double-click is a convenience rather than the only editing path.

## Search timing and focus stability

Saved-computer text search uses a short 180 ms WPF dispatcher debounce before rebuilding the filtered/sorted view. This reduces repeated work while the user is typing without moving focus away from the search field. Explicit sort and favorites changes remain immediate.

When a visible selected computer remains in the new filtered/sorted result, its UUID is used to restore selection. This avoids unnecessary keyboard-context loss during refreshes. If a filter legitimately removes that profile, selection is cleared rather than silently selecting another computer.

The debounce timer is stopped and detached when the main window closes and does not run as an external/background service.

## High Contrast

The standard Ghost RDP dark palette remains the default. When Windows High Contrast is active, Ghost RDP maps its semantic brushes to Windows window, text, control, highlight, and gray-text colors. Application logic and security behavior are unchanged by the palette switch.

Custom controls continue to consume semantic dynamic brushes so the same text-entry and selection templates can follow the High Contrast palette instead of hard-coding a second inaccessible color scheme.

## Dependency boundary

Accessibility implementation uses WPF/Windows platform capabilities already present in the application. No third-party UI, icon, accessibility, or theme runtime package is required. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Scope

This milestone improves the accessibility foundation but does not claim a formal WCAG or EN 301 549 conformance audit. Authentic Windows UI testing with Narrator, keyboard-only navigation, DPI scaling, custom scaling, and High Contrast remains part of release validation.

## Documentation synchronization

Accessibility changes must be reviewed against README, CHANGELOG, ROADMAP, UI/UX, Windows, Security, and Release documentation in the same pull-request cycle. See [Documentation index](README.md).
