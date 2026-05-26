# RevitMate — Plan triển khai với Claude Code (bản tiếng Việt)

> **Tagline**: *"Your drafting mate inside Revit, powered by Claude."*
>
> **Mục tiêu**: Build Revit Add-in tích hợp Claude API, cho phép user ra lệnh bằng ngôn ngữ tự nhiên để thao tác MEP elements trong Revit.
>
> **Ngôn ngữ deliver**: English + Japanese (i18n từ đầu)
>
> **Ngôn ngữ code**: 100% English (class, variable, comment, log, commit, exception)
>
> **Ngôn ngữ tài liệu (SPEC, README)**: English
>
> **Thời gian**: 6 ngày làm việc
>
> **Cách dùng file này**: Đây là bản tiếng Việt cho bạn dễ làm việc với AI. Mỗi prompt đã viết sẵn bằng tiếng Anh (vì Claude Code hiểu tốt hơn và code sinh ra chuẩn English). Copy paste prompt nguyên block vào Claude Code.

---

## Nguyên tắc ngôn ngữ trong toàn project

| Thành phần | Ngôn ngữ |
|---|---|
| Class, method, variable, file name | **English** |
| Code comment, log, exception | **English** |
| Git commit message | **English** |
| SPEC.md, README.md, CHANGELOG.md | **English** |
| UI labels, buttons, messages | **i18n: EN (default) + JP** |
| AI system prompt | **English** (Claude hiểu tốt hơn) |
| AI response language | **User chọn trong Settings** (EN/JP/auto-detect) |
| Tool descriptions cho Claude | **English** |

---

## Ngày 1 — Manual Setup (làm tay)

Phần này KHÔNG dùng Claude Code, phải tự làm vì liên quan đăng ký tài khoản và cài đặt môi trường.

### Việc cần làm

1. **Đăng ký Claude API**
   - Vào `console.anthropic.com`, đăng ký account mới
   - Add billing — Anthropic tặng $5 free credit
   - Tạo API key tại mục **API Keys**, copy key dạng `sk-ant-api03-...`
   - Lưu key vào notepad an toàn (không commit lên git)

2. **Test API hoạt động**
   - Mở PowerShell, chạy lệnh dưới (thay YOUR_KEY):
   ```bash
   curl https://api.anthropic.com/v1/messages `
     -H "x-api-key: YOUR_KEY" `
     -H "anthropic-version: 2023-06-01" `
     -H "content-type: application/json" `
     -d '{\"model\": \"claude-sonnet-4-20250514\", \"max_tokens\": 100, \"messages\": [{\"role\": \"user\", \"content\": \"Hi\"}]}'
   ```
   - Nếu nhận được JSON response → API key OK

3. **Cài Claude Code**
   - Cài Node.js 18+
   - Chạy: `npm install -g @anthropic-ai/claude-code`
   - Chạy `claude` trong terminal, đăng nhập theo hướng dẫn

4. **Cài Visual Studio 2022**
   - Workload: ".NET desktop development"
   - Cài Revit SDK 2024 (Autodesk Developer Network)
   - Tạo folder: `D:\Projects\RevitMate`

5. **Khởi tạo git repo**
   ```bash
   cd D:\Projects\RevitMate
   git init
   ```

### Output cuối ngày
- API key working
- Claude Code installed
- Visual Studio + Revit SDK ready
- Empty folder với git init

---

## Ngày 2 — Spec + Project scaffold

### Prompt 1 — Tạo SPEC.md (English)

```
I am building "RevitMate" — a Revit Add-in integrated with Claude API
that lets users command MEP electrical operations in natural language
(English or Japanese). The add-in targets Revit 2024 MEP designers
working in a Japanese engineering company.

Tagline: "Your drafting mate inside Revit, powered by Claude."

Create a SPEC.md file in the current directory with the following sections,
written in English using clean markdown:

1. Project overview — name, tagline, problem statement, target users
2. Tech stack — .NET Framework 4.8, Revit API 2024, Claude API
   (claude-sonnet-4-20250514), WPF, Newtonsoft.Json,
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
7. Constraints — Revit 2024 only, English/Japanese only,
   no auto-undo yet, electrical MEP only (no structural/HVAC)
8. Future roadmap — multi-discipline support, voice input,
   batch operations, more languages

Keep it concise and professional, formatted with proper markdown headings.
```

### Prompt 2 — Solution scaffold

```
Create a Visual Studio solution structure for RevitMate.
All code, comments, class names, file names, and git commits must be in English.

Solution: RevitMate.sln

Project 1: RevitMate.Core (Class Library, .NET Framework 4.8)
- Purpose: Claude API client, tool definitions, models. No Revit references
  (so we can unit-test without opening Revit).
- Folders: Models/, Api/, Tools/, Resources/
- NuGet: Newtonsoft.Json, System.Net.Http

Project 2: RevitMate.Addin (Class Library, .NET Framework 4.8)
- References: RevitMate.Core + RevitAPI.dll + RevitAPIUI.dll
  (Revit dlls at "C:\Program Files\Autodesk\Revit 2024\")
- Folders: Commands/, UI/, Executor/, Resources/
- Main file: Application.cs (IExternalApplication)

Project 3: RevitMate.ConsoleTest (Console Application, .NET Framework 4.8)
- References: RevitMate.Core
- Purpose: smoke test for Claude API client without launching Revit
- File: Program.cs

Project 4: RevitMate.Resources (Class Library, .NET Framework 4.8)
- Purpose: shared i18n resource files
- Files:
  - Strings.resx (default, English)
  - Strings.ja.resx (Japanese)
- Make sure resx files have PublicResXFileCodeGenerator so strings
  are accessible from other projects as Strings.SendButton, etc.

Create standard .gitignore for .NET (bin/, obj/, .vs/, *.user, packages/).

Create README.md (English) with brief description of the four projects
and how to build.

Create appsettings.json template for ConsoleTest with placeholder field
"ClaudeApiKey" (do NOT commit real key). Add appsettings.json to .gitignore,
but create appsettings.template.json that IS committed.

Only generate the scaffold (project files, AssemblyInfo, empty class stubs).
No business logic yet. Target .NET Framework 4.8 specifically.
```

### Output cuối ngày
- `SPEC.md` đầy đủ bằng tiếng Anh
- Solution với 4 project build được
- Resource project sẵn sàng cho i18n
- README + .gitignore + appsettings template
- Commit git lần đầu

---

## Ngày 3 — Claude API client + Console test

### Prompt 1 — DTOs và Client

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
- Constructor: apiKey, model (default "claude-sonnet-4-20250514")
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
1. Read API key from appsettings.json (use Newtonsoft.Json to parse).
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

### Output cuối ngày
- Console app chạy được, chat với Claude
- Tool use loop hoạt động
- Commit git "Phase 2 complete: API client + tool use"

---

## Ngày 4 — Revit Add-in shell + WPF UI + i18n

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
- Place at %ProgramData%\Autodesk\Revit\Addins\2024\
- Type: Application, assembly path to DLL, full class name, AddInId GUID

Post-build event: copy DLL + .addin file to Revit 2024 addins folder.

Verify: build, launch Revit 2024 → see "RevitMate" tab with two buttons,
click "Open RevitMate" → docked panel appears on the right with the chat UI
in English. Type and Send shows the message in the chat (Claude API not
connected yet — that's tomorrow).
```

### Output cuối ngày
- Revit ribbon có tab "RevitMate" với 2 button
- Panel hiển thị đẹp, mọi text qua resource file
- Đổi ngôn ngữ trong config → UI đổi ngay (test bằng cách sửa config)
- Commit git "Add-in shell + i18n complete"

---

## Ngày 5 — Revit tools + Executor + End-to-end

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

  "You are RevitMate, an AI assistant embedded in Autodesk Revit 2024,
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
- Load API key from %AppData%\RevitMate\config.json
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

### Output cuối ngày
- End-to-end working bằng cả tiếng Anh và tiếng Nhật
- Switch ngôn ngữ UI hoạt động
- AI tự detect ngôn ngữ user và reply tương ứng
- Commit git "MVP working with bilingual support"

---

## Ngày 6 — Polish + Demo materials

### Prompt 1 — Polish + Settings dialog

```
Polish RevitMate. All UI text via resource files. All code/comments in English.

1. Settings dialog (UI/SettingsWindow.xaml + .cs)
   - Title bound to Strings.SettingsTitle
   - Fields:
     * API key (PasswordBox, bound to Strings.ApiKeyLabel)
     * Model (ComboBox: claude-sonnet-4-20250514, claude-opus-4)
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

### Việc làm tay ngày 6 (không dùng AI)
- Quay video demo theo DEMO_SCRIPT (cả phần command tiếng Anh và tiếng Nhật)
- Screenshot UI cả 2 ngôn ngữ cho README
- Tạo file PowerPoint từ PRESENTATION.md
- Push code lên GitHub

### Output cuối ngày
- App polished, hỗ trợ EN + JP hoàn chỉnh
- Toàn bộ docs deliver được bằng EN + JP
- Demo video bilingual sẵn sàng
- GitHub repo ready để submit

---

## Tips khi dùng Claude Code

1. **Mở Claude Code trong folder root**: `cd D:\Projects\RevitMate && claude`
2. **Đọc & review code trước khi accept** — đặc biệt phần ExternalEvent dễ sai
3. **Khi build lỗi, paste error cho Claude Code** — nó tự sửa
4. **Test cuối mỗi ngày**, không dồn lỗi
5. **Commit git sau mỗi prompt thành công**
6. **Khi cần đổi UI string**, sửa trong file resx, không sửa code
7. **Test ngôn ngữ JP**: sửa config thành `"lang": "ja"`, restart Revit, kiểm tra
8. **Test AI reply ngôn ngữ**: gõ tiếng Nhật vào prompt, AI phải reply tiếng Nhật

---

## Checklist tổng thể

- [ ] Ngày 1: Setup môi trường + API key + Claude Code
- [ ] Ngày 2: SPEC.md (EN) + Solution scaffold 4 projects
- [ ] Ngày 3: Claude API client + Console test working
- [ ] Ngày 4: Add-in shell + WPF UI + i18n (EN + JP resx files)
- [ ] Ngày 5: Revit tools + Executor + End-to-end bilingual MVP
- [ ] Ngày 6: Polish + Settings dialog + Demo materials (EN + JP)
- [ ] Bonus: Quay video demo bilingual, chuẩn bị slide thuyết trình

---

**Chúc bạn build thành công! 🚀**
