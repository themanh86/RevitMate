# RevitMate — Implementation Plan with Claude Code (English Edition)

> **Tagline**: *"Your drafting mate inside Revit, powered by Claude."*
>
> **Goal**: Build a Revit Add-in integrated with Claude API that allows users to operate MEP electrical elements in Revit using natural language commands (English or Japanese).
>
> **Delivery languages**: English + Japanese (i18n from day one)
>
> **Code language**: 100% English (classes, variables, comments, logs, commits, exceptions)
>
> **Documentation language (SPEC, README)**: English (with Japanese translations for user-facing docs)
>
> **Duration**: 6 working days
>
> **How to use this file**: This is the English version for project handover and reference. Each phase contains ready-to-use prompts you can paste directly into Claude Code.

---

## ⚠️ Important Notes on .NET 8 + Revit 2026

When moving from .NET Framework 4.8 to .NET 8, the following differences matter:

| Aspect | .NET Framework 4.8 | .NET 8 (chosen) |
|---|---|---|
| Project SDK | Legacy `<Project>` format | `Microsoft.NET.Sdk` (SDK-style) |
| TargetFramework | `net48` | `net8.0-windows` (the `-windows` suffix is required for WPF) |
| `UseWPF` | implicit | must set `<UseWPF>true</UseWPF>` |
| Config file | `app.config` / `App.config` | `appsettings.json` |
| `System.Configuration` | built in | requires `Microsoft.Extensions.Configuration.*` |
| `ProtectedData` (DPAPI) | built in | requires NuGet `System.Security.Cryptography.ProtectedData` |
| Resources (.resx) | `ResXFileCodeGenerator` | still works, generator is slightly different |
| HttpClient | available | available (improved) |
| Newtonsoft.Json | typical | still works; also `System.Text.Json` built in |

**Autodesk for Revit 2026**: The Revit 2026 SDK ships against .NET 8. When referencing `RevitAPI.dll` / `RevitAPIUI.dll` from `C:\Program Files\Autodesk\Revit 2026\`, target `net8.0-windows`.

**Recommendations**:
- All four projects target `net8.0-windows` (because of WPF + i18n + Revit interop)
- Use `System.Text.Json` for new serialization (lighter, faster) or keep `Newtonsoft.Json` if Claude API DTOs are simpler to map
- Configuration via `appsettings.json` + `Microsoft.Extensions.Configuration`

---

## Language Strategy

| Component | Language |
|---|---|
| Class, method, variable, file names | **English** |
| Code comments, logs, exceptions | **English** |
| Git commit messages | **English** |
| SPEC.md, README.md, CHANGELOG.md | **English** (+ Japanese translations) |
| UI labels, buttons, messages | **i18n: EN (default) + JP** |
| AI system prompt | **English** (Claude understands instructions best in English) |
| AI response language | **User-selectable in Settings** (EN / JP / Auto-detect) |
| Tool descriptions for Claude | **English** |

---

## Day 1 — Manual Setup (no AI)

This phase is done manually because it involves account registration and environment installation.

### Tasks

1. **Sign up for Claude API**
   - Visit `console.anthropic.com`, create a new account
   - Add billing — Anthropic provides $5 free credit
   - Generate an API key under **API Keys**, copy the `sk-ant-api03-...` string
   - Store it securely (do not commit to git)

2. **Verify the API key works**
   - Open PowerShell, run (replace YOUR_KEY):
   ```bash
   curl https://api.anthropic.com/v1/messages `
     -H "x-api-key: YOUR_KEY" `
     -H "anthropic-version: 2023-06-01" `
     -H "content-type: application/json" `
     -d '{\"model\": \"claude-sonnet-4-5\", \"max_tokens\": 100, \"messages\": [{\"role\": \"user\", \"content\": \"Hi\"}]}'
   ```
   - A JSON response means your key is active.

3. **Install Claude Code**
   - Install Node.js 18+
   - Run: `npm install -g @anthropic-ai/claude-code`
   - Run `claude` in your terminal and follow the login flow.

4. **Install Visual Studio 2022**
   - Workload: ".NET desktop development"
   - Install the Revit SDK 2026 (from the Autodesk Developer Network)
   - Create the project folder: `D:\Projects\RevitMate`

5. **Initialize a git repository**
   ```bash
   cd D:\Projects\RevitMate
   git init
   ```

### End-of-day deliverables
- Working API key
- Claude Code installed and authenticated
- Visual Studio and Revit SDK ready
- Empty project folder with git initialized

---

## Day 2 — Spec + Project Scaffold

### Prompt 1 — Generate SPEC.md

```
I am building "RevitMate" — a Revit Add-in integrated with Claude API
that lets users command MEP electrical operations in natural language
(English or Japanese). The add-in targets Revit 2026 MEP designers
working in a Japanese engineering company.

Tagline: "Your drafting mate inside Revit, powered by Claude."

Create a SPEC.md file in the current directory with the following sections,
written in English using clean markdown:

1. Project overview — name, tagline, problem statement, target users
2. Tech stack — .NET 8, Revit API 2026, Claude API
   (claude-sonnet-4-5), WPF, Newtonsoft.Json,
   System.Resources for i18n
3. System architecture — three layers:
   - UI layer (WPF DockablePane with i18n support for EN/JP)
   - AI layer (Claude API client)
   - Executor layer (Revit API command dispatcher)
4. Five concrete MEP electrical use cases:
   - Create a grid of downlights in a room
   - Connect multiple fixtures to a circuit
   - Place an electrical panel based on room boundary
   - Generate a lighting schedule
   - Check circuit overload
   Each use case: user input example (in both EN and JP),
   expected behavior, AI tool that will be invoked.
5. Tool definitions — list 7 tools Claude is allowed to call:
   get_selected_elements, get_active_view_info, get_room_info,
   create_light_fixture, set_parameter, connect_to_circuit, get_circuit_info
6. Internationalization strategy — resx files for EN (default) and JP,
   user-selectable response language for AI
7. Constraints — Revit 2026 only, English/Japanese only,
   no auto-undo yet, electrical MEP only (no structural/HVAC)
8. Future roadmap — multi-discipline support, voice input,
   batch operations, more languages

Keep it concise and professional, formatted with proper markdown headings.
```

### Prompt 2 — Solution scaffold

```
Create a Visual Studio solution structure for RevitMate targeting
.NET 8 and Revit 2026. All code, comments, class names, file names,
and git commits must be in English. Use modern SDK-style project files.

Solution: RevitMate.sln

Project 1: RevitMate.Core (Class Library)
- TargetFramework: net8.0-windows
- Purpose: Claude API client, tool definitions, models. No Revit references
  (so we can unit-test without opening Revit).
- Folders: Models/, Api/, Tools/
- NuGet packages:
  - System.Text.Json (built-in) OR Newtonsoft.Json — choose System.Text.Json
  - Microsoft.Extensions.Configuration
  - Microsoft.Extensions.Configuration.Json
  - Microsoft.Extensions.Configuration.Binder

Project 2: RevitMate.Addin (Class Library)
- TargetFramework: net8.0-windows
- Set <UseWPF>true</UseWPF> in csproj
- References: RevitMate.Core + RevitMate.Resources + RevitAPI.dll + RevitAPIUI.dll
  (Revit 2026 dlls at "C:\Program Files\Autodesk\Revit 2026\")
  Set <Private>false</Private> on Revit DLL references (they are loaded by Revit)
- Folders: Commands/, UI/, Executor/
- Main file: Application.cs (IExternalApplication)
- NuGet: System.Security.Cryptography.ProtectedData (for DPAPI encryption of API key)

Project 3: RevitMate.ConsoleTest (Console Application)
- TargetFramework: net8.0
- References: RevitMate.Core
- Purpose: smoke test for Claude API client without launching Revit
- File: Program.cs
- NuGet: same configuration packages as Core

Project 4: RevitMate.Resources (Class Library)
- TargetFramework: net8.0-windows
- Purpose: shared i18n resource files
- Files:
  - Strings.resx (default, English)
  - Strings.ja.resx (Japanese)
- In csproj, configure resx files with Generator = MSBuild:Compile
  and CustomToolNamespace = RevitMate.Resources so strings are
  accessible from other projects as Strings.SendButton.
- Make sure access modifier on generated class is Public.

Create a standard .NET .gitignore (bin/, obj/, .vs/, *.user).

Create README.md (English) describing the four projects and how to build.

Create appsettings.json template for ConsoleTest with placeholder
"ClaudeApiKey" and "Model" fields. Add appsettings.json to .gitignore,
but commit appsettings.template.json with the structure.

Only generate the scaffold (csproj, AssemblyInfo if needed, empty stubs).
No business logic yet. All four csproj files must use SDK-style format
starting with <Project Sdk="Microsoft.NET.Sdk">.

After generation, verify with: dotnet build RevitMate.sln
```

### End-of-day deliverables
- Complete `SPEC.md` in English
- Solution with 4 projects that build successfully
- Resource project ready for i18n
- README, `.gitignore`, and `appsettings.template.json`
- First git commit

---

## Day 3 — Claude API Client + Console Test

### Prompt 1 — DTOs and Client

```
In project RevitMate.Core, build a complete Claude API client.
All code, comments, and exception messages must be in English.

File 1: Models/ClaudeModels.cs — DTOs that map Claude API JSON
- MessageRequest: model, max_tokens, system, messages[], tools[]
- Message: role, content (string or List<ContentBlock>)
- ContentBlock: type ("text" | "tool_use" | "tool_result"),
  text, id, name, input, tool_use_id, content
- Tool: name, description, input_schema (JObject)
- MessageResponse: id, role, content[], stop_reason, usage
- Usage: input_tokens, output_tokens

Use [JsonProperty] attributes for snake_case mapping.
For ContentBlock flexibility, use custom JsonConverter or dynamic typing.

File 2: Api/ClaudeApiClient.cs
- Constructor: apiKey, model (default "claude-sonnet-4-5")
- Property: List<Message> ConversationHistory
- Method: Task<MessageResponse> SendMessageAsync(
    string userText,
    List<Tool> tools = null,
    string systemPrompt = null)
- Method: void AddToolResult(string toolUseId, string result)
  (appends tool_result to history after executor finishes)
- Method: void ClearHistory()
- Use a single static HttpClient (do not dispose per call)
- Endpoint: https://api.anthropic.com/v1/messages
- Headers: x-api-key, anthropic-version: 2023-06-01, content-type: application/json
- Throw ClaudeApiException with clear English message on non-200 status,
  log the raw response body for debugging.

File 3: Api/ClaudeApiException.cs — custom exception class

Write XML doc comments in English for all public methods.
```

### Prompt 2 — Console test app

```
In project RevitMate.ConsoleTest, write Program.cs to test the API client.
All output and logs must be in English.

Requirements:
1. Read API key and model from appsettings.json using Microsoft.Extensions.Configuration (ConfigurationBuilder().AddJsonFile("appsettings.json").Build()).
2. Construct ClaudeApiClient with the key.
3. REPL chat loop:
   - Console.Write("You: ")
   - Read user input
   - If input == "exit" → break
   - If input == "clear" → ClearHistory()
   - Call SendMessageAsync, print response text
   - Print token usage after each turn (input + output)
4. Catch exceptions, print errors clearly.

Then add a TOOL-USE TEST block:
- Define one dummy tool "get_current_time" (no parameters,
  description: "Returns the current server date and time")
- When Claude returns a tool_use block, print
  "AI wants to call tool: <name>" with the JSON input
- Auto-respond with DateTime.UtcNow.ToString("o") via AddToolResult
- Call SendMessageAsync again (no new user input) so Claude can
  continue and produce the final text answer
- Print the final response

Goal: verify the full tool-use loop works in isolation before
integrating with Revit. Try prompts like "What time is it?".
```

### End-of-day deliverables
- Console app that chats with Claude
- Working tool-use loop
- Git commit: "Phase 2 complete: API client + tool use"

---

## Day 4 — Revit Add-in Shell + WPF UI + i18n

### Prompt 1 — i18n resources

```
In project RevitMate.Resources, set up internationalization with English
(default) and Japanese.

Create Strings.resx (English, default) with these keys:

# Window / panel titles
AppName = RevitMate
PanelTitle = RevitMate Assistant
SettingsTitle = RevitMate Settings

# Ribbon
RibbonTabName = RevitMate
RibbonPanelName = AI Tools
OpenButtonText = Open RevitMate
SettingsButtonText = Settings

# Chat UI
InputPlaceholder = Type a command or question...
SendButton = Send
NewConversationButton = New conversation
CopyButton = Copy
ExecutingPrefix = Executing:
TokenUsageLabel = Tokens used:

# Suggestions
Suggestion1 = Create a downlight grid in the selected room
Suggestion2 = Check circuit DB-L1 load
Suggestion3 = Generate lighting schedule for this level

# Settings dialog
ApiKeyLabel = Claude API Key
ModelLabel = Model
MaxTokensLabel = Max tokens per response
ResponseLanguageLabel = AI response language
LanguageAuto = Auto-detect from input
LanguageEnglish = English
LanguageJapanese = Japanese
SaveButton = Save
CancelButton = Cancel

# Error messages
ErrorInvalidApiKey = Invalid API key. Please update it in Settings.
ErrorNoActiveDocument = Please open a Revit project first.
ErrorNetworkFailure = Network error. Check your internet connection.
ErrorGeneric = An error occurred: {0}

# Status
StatusReady = Ready
StatusThinking = Thinking...
StatusExecuting = Executing tool...

Then create Strings.ja.resx with the same keys translated to Japanese:

AppName = RevitMate
PanelTitle = RevitMate アシスタント
SettingsTitle = RevitMate 設定
RibbonTabName = RevitMate
RibbonPanelName = AI ツール
OpenButtonText = RevitMate を開く
SettingsButtonText = 設定
InputPlaceholder = コマンドまたは質問を入力...
SendButton = 送信
NewConversationButton = 新しい会話
CopyButton = コピー
ExecutingPrefix = 実行中:
TokenUsageLabel = 使用トークン数:
Suggestion1 = 選択中の部屋にダウンライトのグリッドを作成
Suggestion2 = 回路 DB-L1 の負荷を確認
Suggestion3 = この階の照明スケジュールを作成
ApiKeyLabel = Claude API キー
ModelLabel = モデル
MaxTokensLabel = 最大応答トークン数
ResponseLanguageLabel = AI 応答言語
LanguageAuto = 入力から自動判定
LanguageEnglish = 英語
LanguageJapanese = 日本語
SaveButton = 保存
CancelButton = キャンセル
ErrorInvalidApiKey = API キーが無効です。設定で更新してください。
ErrorNoActiveDocument = まず Revit プロジェクトを開いてください。
ErrorNetworkFailure = ネットワークエラーです。インターネット接続を確認してください。
ErrorGeneric = エラーが発生しました: {0}
StatusReady = 準備完了
StatusThinking = 考え中...
StatusExecuting = ツール実行中...

Make sure both resx files have access modifier set to "Public" so
Strings.SendButton etc. can be called from other projects.

Also create a helper class Resources/LocalizationManager.cs:
- Static property: CultureInfo CurrentCulture
- Method: void SetLanguage(string lang) — accepts "en" or "ja",
  sets Thread.CurrentThread.CurrentUICulture
- Method: string Get(string key) — wrapper around Strings.ResourceManager.GetString
- Load language preference from %AppData%\RevitMate\config.json on startup
```

### Prompt 2 — Add-in shell + WPF UI

```
In project RevitMate.Addin, build the Revit add-in scaffold and WPF panel.
All code, comments, and identifiers must be in English.
All user-facing strings MUST come from RevitMate.Resources.Strings
(do NOT hardcode English text in XAML or code-behind).

File 1: Application.cs implementing IExternalApplication
- OnStartup:
  - Call LocalizationManager.LoadFromConfig() to set language
  - Create ribbon tab using Strings.RibbonTabName
  - Create ribbon panel using Strings.RibbonPanelName
  - Add "Open" button (text = Strings.OpenButtonText, command = OpenRevitMateCommand)
  - Add "Settings" button (text = Strings.SettingsButtonText, command = OpenSettingsCommand)
  - Use placeholder PNG icons (32x32) at Resources/Icons/ if not yet available
  - Register DockablePane with a fixed GUID
- OnShutdown: cleanup

File 2: Commands/OpenRevitMateCommand.cs implementing IExternalCommand
- Get DockablePane by GUID, call Show()

File 3: Commands/OpenSettingsCommand.cs implementing IExternalCommand
- Open SettingsWindow as dialog

File 4: UI/MainPaneProvider.cs implementing IDockablePaneProvider
- SetupDockablePane returns MainPane (WPF UserControl)
- Default position: tab dock right

File 5: UI/MainPane.xaml + MainPane.xaml.cs
- WPF UserControl bound to MainViewModel
- All text uses x:Static binding to RevitMate.Resources.Strings,
  e.g. <Button Content="{x:Static res:Strings.SendButton}"/>
- Layout:
  - Top: header (PanelTitle) + status indicator + model name
  - Middle: ScrollViewer with ItemsControl bound to Messages
  - Bottom: input TextBox + Send button + suggestion chips
- Style: clean modern, Segoe UI, 10px padding
- Chat bubble: AI = white with grey border (left), User = light teal (right)

File 6: UI/MainViewModel.cs (MVVM)
- ObservableCollection<ChatMessage> Messages
- string CurrentInput (INotifyPropertyChanged)
- bool IsLoading
- ICommand SendCommand → for today, just append the user message;
  Claude integration comes tomorrow
- ICommand NewConversationCommand → clears Messages
- ICommand UseSuggestionCommand → fills input with suggestion text
- Implement INotifyPropertyChanged fully

File 7: UI/ChatMessage.cs
- enum Role { User, Assistant, System, Action }
- Properties: Role, Text, Timestamp

File 8: RevitMate.addin manifest
- Place at %ProgramData%\Autodesk\Revit\Addins\2026\
- Type: Application, assembly path to DLL, full class name, AddInId GUID

Post-build event: copy DLL + .addin file to Revit 2026 addins folder.

Verify: build, launch Revit 2026 → see "RevitMate" tab with two buttons,
click "Open RevitMate" → docked panel appears on the right with the chat UI
in English. Type and Send shows the message in the chat (Claude API not
connected yet — that's tomorrow).
```

### End-of-day deliverables
- Revit ribbon shows "RevitMate" tab with two buttons
- Panel renders cleanly, all text driven by resource files
- Changing the language in config and restarting Revit switches UI text
- Git commit: "Add-in shell + i18n complete"

---

## Day 5 — Revit Tools + Executor + End-to-End

### Prompt 1 — Define Revit tools

```
In RevitMate.Core/Tools/, define the Revit tools that Claude can call.
All code, comments, and tool descriptions must be in English
(Claude understands English tool definitions most reliably,
even when the user prompt is in Japanese).

File: RevitToolDefinitions.cs
- Public static class with method GetAllTools() returning List<Tool>
- Each Tool has: name, description, input_schema (JObject in JSON Schema form)

Define these 7 tools with detailed English descriptions:

1. get_selected_elements
   description: "Returns the elements the user has currently selected
                 in the Revit active view. Use this whenever the user
                 says 'this element', 'the selected items', 'these', etc."
   params: none

2. get_active_view_info
   description: "Returns metadata about the active Revit view: name,
                 view type, associated level, scale."
   params: none

3. get_room_info
   description: "Returns detailed information about a room: name, number,
                 boundary points, area in square meters, level. Use when
                 the user refers to a specific room by name or selection."
   params: room_id (integer, optional), room_name (string, optional)

4. create_light_fixture
   description: "Places light fixtures in a grid pattern within a room
                 or rectangular area. Returns IDs of created fixtures."
   params:
     family_name (string, required) — family symbol name
     grid_x_mm (number, required) — column spacing in millimeters
     grid_y_mm (number, required) — row spacing in millimeters
     count (integer, optional) — limit on number to place
     room_id (integer, optional)
     level_name (string, required)

5. set_parameter
   description: "Sets a parameter value on one or more elements."
   params:
     element_ids (array of integers, required)
     parameter_name (string, required)
     value (string|number, required)

6. connect_to_circuit
   description: "Connects electrical elements to a specified branch circuit
                 on a panel."
   params:
     element_ids (array of integers, required)
     panel_name (string, required)
     circuit_number (integer, required)

7. get_circuit_info
   description: "Returns information about a circuit: current load,
                 capacity, list of connected element IDs."
   params: panel_name (string, required), circuit_number (integer, required)

Make descriptions detailed enough that Claude can pick the right tool
from a vague natural-language command in either English or Japanese,
without the user having to name the tool.
```

### Prompt 2 — RevitCommandExecutor + ExternalEvent

```
In RevitMate.Addin/Executor/, build the executor that runs Revit API calls
in response to Claude tool_use blocks.

CRITICAL: Revit API only runs on Revit's main UI thread. Since Claude API
calls are async, we must marshal the tool execution back to the Revit
thread using ExternalEvent. All code in English.

File 1: Executor/RevitExternalEventHandler.cs implementing IExternalEventHandler
- Property: ToolCall PendingCall { Name (string), Input (JObject) }
- Property: TaskCompletionSource<string> CompletionSource
- Method Execute(UIApplication app):
  - doc = app.ActiveUIDocument.Document
  - Dispatch by PendingCall.Name to the correct command handler
  - Wrap mutating operations in
      using (var t = new Transaction(doc, "RevitMate: " + name))
      { t.Start(); ...; t.Commit(); }
  - Call CompletionSource.SetResult(jsonResultString) on success
  - On exception, set result to a JSON error object
- GetName() returns "RevitMate Executor"

File 2: Executor/RevitCommandDispatcher.cs
- Singleton holding the ExternalEvent + RevitExternalEventHandler
- Method Task<string> ExecuteAsync(string toolName, JObject input):
  - Set PendingCall
  - Create new TaskCompletionSource
  - Raise the ExternalEvent
  - await TaskCompletionSource.Task

File 3-9: Executor/Commands/ — one class per tool, all implementing
  interface ICommandHandler { string Execute(Document doc, JObject input); }

- GetSelectedElementsCommand.cs
- GetActiveViewInfoCommand.cs
- GetRoomInfoCommand.cs (FilteredElementCollector .OfCategory(BuiltInCategory.OST_Rooms))
- CreateLightFixtureCommand.cs
  + Find FamilySymbol by family_name (FilteredElementCollector.OfClass<FamilySymbol>)
  + Activate symbol if not active
  + Get room boundary, compute grid points
  + Call doc.Create.NewFamilyInstance for each point on the matching level
  + Return JSON { created_count, element_ids: [...] }
- SetParameterCommand.cs
- ConnectToCircuitCommand.cs
- GetCircuitInfoCommand.cs

Each command must catch exceptions internally and return a JSON object
{ "error": "<message>" } rather than throwing.

Add Trace.WriteLine at the start of each command with tool name and input
for debugging.
```

### Prompt 3 — End-to-end integration

```
Wire MainViewModel to ClaudeApiClient and RevitCommandDispatcher.
All code in English. All user-facing strings via RevitMate.Resources.Strings.

Update UI/MainViewModel.cs:
- Constructor injects: ClaudeApiClient, RevitCommandDispatcher
- Hardcoded system prompt (in English — Claude's tool-calling is most
  accurate with English instructions, even when user writes in Japanese):

  "You are RevitMate, an AI assistant embedded in Autodesk Revit 2026,
   specialized in MEP electrical design. Users may issue commands in
   English or Japanese. Analyze each command and invoke the appropriate
   tool. If a command is ambiguous, ask a clarifying question instead
   of guessing. After tools execute, summarize the result concisely in
   the same language the user used (or in the language configured in
   settings if set explicitly)."

- When user sends a message:
  1. Append to Messages as Role.User
  2. Set IsLoading = true
  3. Call await client.SendMessageAsync(text,
        RevitToolDefinitions.GetAllTools(), systemPrompt)
  4. Process response loop:
     a. For each text content block → append to Messages as Role.Assistant
     b. For each tool_use block:
        - Append "Executing: <tool_name>" as Role.Action
          (use Strings.ExecutingPrefix)
        - result = await dispatcher.ExecuteAsync(toolName, input)
        - client.AddToolResult(toolUseId, result)
        - Call client.SendMessageAsync(null, tools, systemPrompt)
          to let Claude process the tool result
        - Continue loop with new response
     c. Exit loop when no more tool_use blocks
  5. Set IsLoading = false
- Catch exceptions, append error message using Strings.ErrorGeneric

Update UI/MainPane.xaml:
- Add ProgressBar bound to IsLoading
- Style Action messages with light purple background and left border accent

Update Application.cs OnStartup:
- Load API key from %AppData%\RevitMate\config.json (using Microsoft.Extensions.Configuration or System.Text.Json)
  (encrypted with DPAPI via System.Security.Cryptography.ProtectedData)
- If no key found, show SettingsWindow first
- Construct ClaudeApiClient and RevitCommandDispatcher
- Pass both into MainPaneProvider to inject into MainViewModel

Test scenario in Revit:
1. Open a sample Revit project with a few rooms and electrical panels
2. Click "Open RevitMate" in the ribbon
3. Type in English: "Get info about the selected room"
   → AI calls get_selected_elements then get_room_info
4. Type in Japanese: "この部屋に2x2mのグリッドでダウンライトを4個配置して"
   → AI calls create_light_fixture, lights appear in Revit
5. Switch language to Japanese in Settings, repeat — UI labels switch to JP,
   AI responses come back in Japanese.
```

### End-of-day deliverables
- End-to-end working in both English and Japanese
- UI language switching works
- AI auto-detects user language and responds accordingly
- Git commit: "MVP working with bilingual support"

---

## Day 6 — Polish + Demo Materials

### Prompt 1 — Polish + Settings dialog

```
Polish RevitMate. All UI text via resource files. All code/comments in English.

1. Settings dialog (UI/SettingsWindow.xaml + .cs)
   - Title bound to Strings.SettingsTitle
   - Fields:
     * API key (PasswordBox, bound to Strings.ApiKeyLabel)
     * Model (ComboBox: claude-sonnet-4-5, claude-haiku-4-5)
     * Max tokens (NumericUpDown, default 4096)
     * AI response language (ComboBox: Auto / English / Japanese,
       bound to Strings.LanguageAuto / LanguageEnglish / LanguageJapanese)
     * UI language (ComboBox: English / Japanese)
   - Save button → write to %AppData%\RevitMate\config.json,
     API key encrypted with ProtectedData.Protect (DPAPI)
   - Save also re-applies UI language via LocalizationManager.SetLanguage
     and prompts user that restart may be needed for full effect

2. Robust error handling everywhere
   - Try-catch around every API call in MainViewModel
   - Show Strings.ErrorInvalidApiKey when 401
   - Show Strings.ErrorNetworkFailure on HttpRequestException
   - Show Strings.ErrorNoActiveDocument before any tool call if no doc
   - Generic Strings.ErrorGeneric formatted with exception message

3. Conversation features
   - "New conversation" button (Strings.NewConversationButton)
   - "Copy" button on each AI message (Strings.CopyButton)
   - Token usage indicator at footer (Strings.TokenUsageLabel)

4. Prompt suggestion chips (when Messages is empty)
   - Use Strings.Suggestion1/2/3
   - Click chip → fills CurrentInput

5. UI polish
   - Fade-in animation on new messages
   - Auto-scroll to bottom when message added
   - Animated "..." indicator while IsLoading
   - Render basic markdown in AI messages (bold, lists, code)
```

### Prompt 2 — Demo materials (EN + JP)

```
Produce documentation and demo materials for RevitMate.
Create both English and Japanese versions of user-facing docs.

1. README.md (English) — overwrite existing
   Sections: project description, screenshots (placeholders),
   tech stack, installation, configuration (how to get an API key),
   usage examples (3 use cases with screenshots), architecture
   diagram (Mermaid), roadmap, license (MIT).

2. README.ja.md (Japanese) — same content, fully translated.

3. PRESENTATION.md — outline for a 10-slide deck (English):
   Slide 1: Title — RevitMate, your name
   Slide 2: Problem — repetitive MEP design tasks, command memorization
   Slide 3: Solution — natural-language AI assistant inside Revit
   Slide 4: Live demo placeholder
   Slide 5: Three-layer architecture
   Slide 6: How Claude API tool use works
   Slide 7: Results — number of tools, lines of code, dev time
   Slide 8: Lessons applied from the seven Anthropic courses
   Slide 9: Roadmap
   Slide 10: Q&A and thank you

4. PRESENTATION.ja.md — same outline, all titles and bullets in Japanese.

5. DEMO_SCRIPT.md (English) — 3-5 minute screen-recording script:
   00:00-00:30 Open Revit, introduce RevitMate panel
   00:30-01:30 Use case 1: create downlight grid (English command)
   01:30-02:30 Use case 2: connect to circuit + check load (Japanese command,
                 demonstrating bilingual support)
   02:30-03:30 Use case 3: generate schedule
   03:30-04:30 Behind the scenes: show Claude API call + tool_use payload
   04:30-05:00 Outro: GitHub link, thanks

6. DEMO_SCRIPT.ja.md — Japanese version of the demo script.

7. CHANGELOG.md — version 0.1.0 entry with the feature list.

All English docs use clean markdown. Japanese docs use natural business
Japanese (keigo where appropriate, technical terms in katakana or English).
```

### Manual tasks (no AI)
- Record the demo video following DEMO_SCRIPT (both English and Japanese commands)
- Take UI screenshots in both languages for the README
- Convert PRESENTATION.md into a PowerPoint deck
- Push the code to GitHub

### End-of-day deliverables
- Polished app with full EN + JP support
- Documentation deliverable in EN + JP
- Bilingual demo video ready
- GitHub repository ready for submission

---

## Tips for Using Claude Code

1. **Open Claude Code in the solution root**: `cd D:\Projects\RevitMate && claude`
2. **Read and review code before accepting** — especially `ExternalEvent` plumbing
3. **When a build fails, paste the error to Claude Code** — it will fix it
4. **Test at the end of each day**, do not accumulate errors
5. **Commit to git after every successful prompt**
6. **To change a UI string**, edit the `.resx` file, not the code
7. **Test Japanese UI**: edit `config.json` to `"lang": "ja"`, restart Revit
8. **Test AI response language**: type Japanese in the prompt and verify the reply is Japanese

---

## Overall Checklist

- [ ] Day 1: Environment setup + API key + Claude Code
- [ ] Day 2: SPEC.md (EN) + 4-project solution scaffold
- [ ] Day 3: Claude API client + working console test
- [ ] Day 4: Add-in shell + WPF UI + i18n (EN + JP resx files)
- [ ] Day 5: Revit tools + executor + bilingual end-to-end MVP
- [ ] Day 6: Polish + settings dialog + demo materials (EN + JP)
- [ ] Bonus: Bilingual demo video, presentation slides

---

**Happy building! 🚀**
