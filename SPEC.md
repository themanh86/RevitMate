# RevitMate — Technical Specification

## 1. Project Overview

**Name:** RevitMate

**Tagline:** *Your drafting mate inside Revit, powered by Claude.*

**Problem Statement**

MEP electrical designers spend a significant share of their day on repetitive, click-heavy Revit operations: laying out downlights, wiring fixtures into circuits, placing panels, producing schedules, and verifying circuit loads. These tasks require deep familiarity with the Revit UI and are slow to perform manually, even though the underlying intent is usually expressible in a single sentence ("put a 3×4 grid of downlights in this room"). Junior designers and bilingual teams also lose time switching between Japanese and English documentation and tool naming.

RevitMate closes that gap by letting designers describe what they want — in English or Japanese — and letting Claude translate that intent into safe, parameterized Revit API calls.

**Target Users**

- MEP electrical designers at a Japanese engineering company
- Mixed EN/JP teams working in Revit 2024
- Junior designers who know electrical intent but not every Revit menu path

---

## 2. Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET Framework 4.8 |
| Host application | Autodesk Revit 2024 (MEP) |
| Revit integration | Revit API 2024 (`RevitAPI.dll`, `RevitAPIUI.dll`) |
| AI model | Claude API — `claude-sonnet-4-5` |
| HTTP / serialization | `HttpClient`, `Newtonsoft.Json` 13.x |
| UI | WPF (`PresentationFramework`, `PresentationCore`) hosted in a Revit `DockablePane` |
| Internationalization | `System.Resources` with `.resx` files (EN default, JP) |
| Logging | `Serilog` (file sink, rolling daily) |
| Packaging | `.addin` manifest + signed assembly, deployed to `%ProgramData%\Autodesk\Revit\Addins\2024\` |

---

## 3. System Architecture

RevitMate is structured as three loosely coupled layers. The UI never calls the Revit API directly; the AI layer never touches Revit document state. All Revit mutations are funneled through the Executor, which is the only layer permitted to open a `Transaction`.

```
+--------------------------------------------------------------+
|                       UI Layer (WPF)                         |
|  DockablePane • Chat view • Language switch (EN/JP) • i18n   |
+----------------------------+---------------------------------+
                             | user prompt + context
                             v
+--------------------------------------------------------------+
|                   AI Layer (Claude client)                   |
|  Prompt builder • Tool schema • Streaming • Tool-use parser  |
+----------------------------+---------------------------------+
                             | tool_use blocks (JSON)
                             v
+--------------------------------------------------------------+
|             Executor Layer (Revit API dispatcher)            |
|  Tool registry • Validation • Transactions • Result mapper   |
+--------------------------------------------------------------+
```

### 3.1 UI Layer (WPF)

- Hosted as a Revit `IDockablePaneProvider` so it docks like a native panel.
- Chat-style view: prompt input, message history, tool-call trace, and a result preview.
- Language toggle (EN / 日本語) bound to a `LocalizationService` that swaps the active `ResourceManager` and raises `PropertyChanged` on all bound strings.
- All user-visible strings come from `Strings.resx` / `Strings.ja-JP.resx` — no hardcoded literals.

### 3.2 AI Layer (Claude API client)

- Single `ClaudeClient` wrapping `HttpClient` with the Anthropic Messages API.
- Sends the user prompt plus a system prompt that declares: the role (Revit MEP assistant), the active view/selection context, the response language, and the available tools.
- Uses Anthropic tool use: the seven tools in §5 are declared with JSON Schema; Claude returns `tool_use` blocks that the Executor consumes.
- Supports a multi-turn agentic loop: tool result → next model turn → next tool call, until Claude returns a final `text` block.
- Prompt caching enabled on the system prompt and tool definitions to control cost.

### 3.3 Executor Layer (Revit API dispatcher)

- `IToolHandler` per tool, registered in a `ToolDispatcher` keyed by tool name.
- Each handler:
  1. Validates arguments against the tool schema.
  2. Acquires the active `Document` from the cached `UIApplication`.
  3. Opens a `Transaction` only when the tool mutates the model.
  4. Returns a strongly typed result that is serialized back to Claude as the `tool_result`.
- All Revit calls are marshaled to the Revit API thread via `ExternalEvent` (Revit API is single-threaded).

---

## 4. MEP Electrical Use Cases

Each use case below shows a representative user prompt in both languages, the expected behavior, and the primary tool Claude will invoke. Tools may be chained — e.g. most creation tools begin with `get_room_info` or `get_active_view_info`.

### 4.1 Create a grid of downlights in a room

- **EN:** *"Place a 3×4 grid of 100W downlights in the selected room."*
- **JP:** *「選択した部屋に100Wのダウンライトを3×4のグリッドで配置して。」*
- **Expected behavior:** Read the room boundary, compute a uniform N×M grid inset from walls by a default margin, and insert downlight family instances at the ceiling level. Report count and average spacing.
- **Primary tool:** `create_light_fixture` (preceded by `get_room_info`, `get_active_view_info`).

### 4.2 Connect multiple fixtures to a circuit

- **EN:** *"Connect the selected fixtures to circuit L-1 on panel LP-1."*
- **JP:** *「選択中の器具をパネルLP-1の回路L-1に接続して。」*
- **Expected behavior:** Validate that the selection is electrical, ensure the target panel exists, and add each fixture to the named circuit. Skip and report any fixture already on another circuit.
- **Primary tool:** `connect_to_circuit` (preceded by `get_selected_elements`).

### 4.3 Place an electrical panel based on room boundary

- **EN:** *"Place panel LP-2 on the longest wall of this electrical room, 1500 mm from the floor."*
- **JP:** *「この電気室の一番長い壁に、床から1500mmの高さでLP-2を設置して。」*
- **Expected behavior:** Identify the longest boundary segment of the room, pick its midpoint, and host an electrical panel family there at the specified elevation.
- **Primary tool:** `create_light_fixture` with the panel family category (or a panel-specific overload), preceded by `get_room_info`.

### 4.4 Generate a lighting schedule

- **EN:** *"Generate a lighting fixture schedule for level 2."*
- **JP:** *「2階の照明器具の集計表を作成して。」*
- **Expected behavior:** Create a `ViewSchedule` for category `OST_LightingFixtures` filtered by the chosen level, with columns for Type, Count, Wattage, and Circuit Number. Open the schedule in a new tab.
- **Primary tool:** `set_parameter` and schedule creation routines (an internal `create_schedule` helper composed on top of the tool set), preceded by `get_active_view_info`.

### 4.5 Check circuit overload

- **EN:** *"Check if circuit L-1 on panel LP-1 is overloaded."*
- **JP:** *「パネルLP-1の回路L-1が過負荷になっていないか確認して。」*
- **Expected behavior:** Sum the apparent load of all elements on the circuit, compare against the breaker rating, and report load percentage. Flag if above 80% (warning) or 100% (overload).
- **Primary tool:** `get_circuit_info`.

---

## 5. Tool Definitions

Claude is allowed to call exactly the following seven tools. Each is declared with a JSON Schema in the AI layer and routed to a handler in the Executor.

| # | Tool name | Purpose | Mutates model |
|---|-----------|---------|---------------|
| 1 | `get_selected_elements` | Return ids, categories, and key parameters of the current selection. | No |
| 2 | `get_active_view_info` | Return the active view's name, type, level, scale, and discipline. | No |
| 3 | `get_room_info` | Return a room's boundary loop, area, level, and ceiling height. | No |
| 4 | `create_light_fixture` | Place a lighting fixture (or family instance of given category) at a point or set of points. | Yes |
| 5 | `set_parameter` | Set a parameter value on one or more elements by id. | Yes |
| 6 | `connect_to_circuit` | Add one or more electrical elements to a named circuit on a named panel. | Yes |
| 7 | `get_circuit_info` | Return a circuit's elements, total connected load, rating, and overload status. | No |

Each mutating tool opens its own `Transaction` with a localized name (e.g. `RevitMate: Create downlights`) so the user can undo a single AI action as a single step.

---

## 6. Internationalization Strategy

- All UI strings live in `Resources/Strings.resx` (English, default) and `Resources/Strings.ja-JP.resx` (Japanese).
- A `LocalizationService` exposes a current `CultureInfo` and notifies bound views when it changes. The current language is persisted per user in `%AppData%\RevitMate\settings.json`.
- The AI response language is independent of the UI language and is sent to Claude as part of the system prompt (`Respond in: English` / `Respond in: Japanese`). This lets a designer keep the UI in Japanese while receiving English explanations, or vice versa.
- Tool descriptions and parameter descriptions sent to Claude remain in English (one source of truth for the model); only the surface response is localized.
- Numbers and units follow the project's unit settings, not the UI culture, to avoid mismatches with Revit's display units.

---

## 7. Constraints

- **Revit version:** Revit 2024 only. No support for 2023 or earlier; no forward commitment to 2025 until tested.
- **Languages:** English and Japanese only for both UI and AI response.
- **Undo:** No automatic rollback. Each mutating tool is a single Revit transaction the user can undo manually; multi-step AI plans are not automatically reverted on partial failure.
- **Discipline:** Electrical MEP only. Structural, HVAC, plumbing, and architectural categories are out of scope for tool execution.
- **Threading:** All Revit API calls are dispatched through `ExternalEvent`; no background threads touch the document.
- **Network:** Requires outbound HTTPS to `api.anthropic.com`. An API key must be configured in settings before the panel becomes interactive.

---

## 8. Future Roadmap

- **Multi-discipline support** — extend the tool set and family resolvers to HVAC, plumbing, and structural.
- **Voice input** — push-to-talk in the dockable panel, using a local speech-to-text bridge for EN/JP.
- **Batch operations** — run a single instruction across multiple rooms, levels, or selected sets in one agent session.
- **More languages** — add Chinese (Simplified), Korean, and Vietnamese resx bundles and AI response presets.
- **Automatic undo / safe-plan mode** — wrap a full multi-tool plan in a single `TransactionGroup` so users can roll back an entire AI action.
- **Workspace context** — feed Claude a richer model summary (active level, view filters, recent edits) for higher-quality plans.
- **Team library** — share prompt templates and approved tool recipes across the engineering office.
