# Changelog

All notable changes to RevitMate are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [0.1.0] — 2026-05-26

### Added

**Core AI integration**
- `ClaudeApiClient` wrapping the Anthropic Messages API with multi-turn agentic loop support
- Seven Revit tool schemas declared with JSON Schema and registered in `RevitToolDefinitions`
- `ToolDispatcher` routing `tool_use` blocks to typed `ICommandHandler` implementations
- Conversation history management with `ClearHistory()` for new-conversation sessions

**Revit tools**
- `get_selected_elements` — returns element IDs, categories, family/type names, and up to 30 parameters per element
- `get_active_view_info` — returns view name, type, level, scale, and discipline
- `get_room_info` — returns room boundary, area, level, ceiling height, and contained MEP element IDs
- `create_light_fixture` — places one or more lighting fixture instances in a grid on a specified level
- `set_parameter` — sets a named built-in or shared parameter on one or more elements
- `connect_to_circuit` — adds electrical elements to an existing circuit on a named panel
- `get_circuit_info` — returns circuit load (VA and amps), rating, phase configuration, and overload status

**UI**
- WPF `DockablePane` hosted via `IDockablePaneProvider`, docked like a native Revit panel
- Chat message list with role-differentiated bubbles (user / assistant / action)
- Action bubbles (purple left-border, italic) showing tool names in real time during execution
- Indeterminate progress bar while the Claude API call is in flight
- Suggestion chips for common commands (three per locale)
- Selection snapshot pill (teal) showing `📌 N elements selected`, persists across pane focus changes
- Input box with placeholder text, `Enter` to send, `Shift+Enter` for newline
- **New Conversation** button to clear history and reset the Claude conversation

**Selection snapshot fix**
- Win32 `GetFocus()` P/Invoke in `Application.SelectionChanged` handler prevents the snapshot being cleared when the DockablePane steals Win32 focus from the Revit viewport
- `MainPane.Loaded` registers the WPF `HwndSource` handle so the guard condition has the correct HWND

**Settings**
- `SettingsWindow` (WPF dialog) for API key entry and Claude model selection
- API key encrypted at rest with Windows DPAPI (`DataProtectionScope.CurrentUser`)
- Model selection persisted to `%AppData%\RevitMate\settings.json`
- Settings accessible from the ribbon **Settings** button at any time

**Localisation**
- Full EN/JP string resource coverage via `RevitMate.Resources` (`.resx` + `.ja.resx`)
- `LocalizationManager` reads locale from `settings.json`; defaults to system locale
- Claude responds in the language the user wrote in (auto-detected per message)

**Infrastructure**
- `RevitExternalEventHandler` marshals all Revit API calls to the Revit main thread via `IExternalEvent` + `TaskCompletionSource<string>`
- Read-only tools run without a `Transaction`; mutating tools each open a named `Transaction` for single-step undo
- `Application.SetRevitApp()` subscribes to `SelectionChanged` exactly once to track live selection
- `RevitMate.ConsoleTest` smoke-test project validates the Claude API round-trip without Revit
- SDK-style `.csproj` for all four projects; .NET 8 target (`net8.0-windows`, x64)
- `RevitMate.addin` manifest for deployment to `%ProgramData%\Autodesk\Revit\Addins\2026\`

### Architecture

```
RevitMate.Core        Claude API client, tool schemas, models — no Revit references
RevitMate.Addin       IExternalApplication, WPF UI, Revit executor
RevitMate.Resources   Shared EN/JP string resources
RevitMate.ConsoleTest Standalone API smoke test
```
