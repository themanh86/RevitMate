# RevitMate — Presentation Outline
## 10-Slide Deck

---

### Slide 1 — Title

**RevitMate**
*Your drafting mate inside Revit, powered by Claude*

- Presented by: Manh Nguyen
- Autodesk Revit 2026 Add-in · Claude API · .NET 8
- https://github.com/themanh86/RevitMate/tree/master

---

### Slide 2 — The Problem

**MEP electrical design is repetitive and click-heavy**

- Placing a 3×4 downlight grid manually: 20+ clicks, no undo if spacing is wrong
- Connecting 12 fixtures to a circuit: navigate panel browser → drag each fixture
- Checking circuit overload: open schedule, sum loads manually
- New hires and bilingual teams spend time learning Revit UI, not electrical design
- Commands are in English; many designers work primarily in Japanese

**The gap**: The designer's *intent* is a single sentence. Revit requires dozens of operations.

---

### Slide 3 — The Solution

**RevitMate: natural language → safe Revit operations**

- Dockable chat panel inside Revit — no external window, no copy-paste
- Type a command in English or Japanese; Claude plans and executes it
- Claude uses *tool use* to call well-defined, validated Revit API functions
- Every mutation is a named Revit transaction → fully undoable with Ctrl+Z
- Read-only queries return instant answers without touching the model

```
User:  Place a 3×4 grid of recessed downlights in the selected room.
↓
Claude calls: get_room_info → create_light_fixture (×12)
↓
"✓ Created 12 fixtures. Grid spacing: 1 200 mm × 900 mm."
```

---

### Slide 4 — Live Demo

*(Switch to Revit for live demonstration)*

**Demo sequence**

1. Select a room → "Place a 3×4 grid of 100 W downlights"
2. Select fixtures → *「選択中の器具をLP-1の回路3に接続して。」*
3. "Is circuit 3 on LP-1 overloaded?"
4. Show the tool-use trace in the chat panel

**Key points to highlight during demo**

- Selection pin persists when clicking the chat box (Win32 focus-steal fix)
- Tool-call action bubbles appear in real time (purple italic style)
- Ctrl+Z undoes each AI action as a single step

---

### Slide 5 — Three-Layer Architecture

```
┌─────────────────────────────────────────────────┐
│               UI Layer  (WPF)                   │
│  DockablePane · Chat view · EN/JP i18n · MVVM   │
└──────────────────────┬──────────────────────────┘
                       │  user message + selection context
                       ▼
┌─────────────────────────────────────────────────┐
│           AI Layer  (RevitMate.Core)             │
│  ClaudeApiClient · Tool schema · Agentic loop    │
└──────────────────────┬──────────────────────────┘
                       │  tool_use blocks (JSON)
                       ▼
┌─────────────────────────────────────────────────┐
│        Executor Layer  (RevitMate.Addin)         │
│  ToolDispatcher · Transactions · ExternalEvent   │
└──────────────────────┬──────────────────────────┘
                       │  Revit API calls
                       ▼
                 Revit Document
```

- **Core has zero Revit references** — independently unit-testable
- **Executor is the only layer** that may open a `Transaction`
- **All Revit calls are marshalled** through `ExternalEvent` (Revit API is single-threaded)

---

### Slide 6 — How Claude Tool Use Works

**The agentic loop inside `SendAsync()`**

```
1. User sends message
2. RevitMate → Claude: { messages, tools: [7 schemas], system_prompt }
3. Claude → RevitMate: { content: [ { type: "tool_use", name: "get_room_info", ... } ] }
4. RevitMate executes tool via ExternalEvent, gets result JSON
5. RevitMate → Claude: { tool_result: "{ area: 42.5, ... }" }
6. Claude → RevitMate: { content: [ { type: "tool_use", name: "create_light_fixture", ... } ] }
7. Repeat until Claude returns type: "text"  →  show in chat
```

**Tool schema example — `create_light_fixture`**

```json
{
  "name": "create_light_fixture",
  "description": "Places one or more light-fixture instances on the specified level...",
  "input_schema": {
    "type": "object",
    "required": ["family_name", "level_name", "grid_x_mm", "grid_y_mm"],
    "properties": {
      "family_name": { "type": "string" },
      "count":       { "type": "integer", "default": 1 }
    }
  }
}
```

- **7 tools** declared; Claude chooses which ones to call and in what order
- **Selection snapshot** injected into `get_selected_elements` input to survive focus-steal

---

### Slide 7 — Results

| Metric | Value |
|---|---|
| Tools implemented | 7 |
| Projects in solution | 4 |
| Lines of C# (approx.) | ~2 000 |
| Languages supported | English, Japanese |
| Supported Revit version | 2026 |
| Undo granularity | Per tool call (Revit Transaction) |
| API key storage | Windows DPAPI encrypted |
| Development time | ~3 weeks (solo) |

**What was hard**

- Win32 focus-steal bug: `SelectionChanged([])` fires inside `WM_KILLFOCUS` — before any WPF event. Fixed by calling `GetFocus()` P/Invoke directly in the event handler.
- Marshalling async Claude responses to the single-threaded Revit API thread via `ExternalEvent` + `TaskCompletionSource`.

---

### Slide 8 — Lessons from the Seven Anthropic Courses

| Course | Applied in RevitMate |
|---|---|
| **Intro to Claude** | System prompt design: role declaration, language instruction, ambiguity handling |
| **Prompt Engineering** | Injecting selection context into the system prompt; asking Claude to clarify before acting |
| **Tool Use** | All seven tools; agentic loop with `tool_result` fed back into the conversation |
| **Building with Claude API** | `ClaudeApiClient` wrapping the Messages API; conversation history management |
| **Agents** | Multi-step plans: Claude chains `get_room_info` → `create_light_fixture` autonomously |
| **Safety & Guardrails** | Read-only vs. mutating tool classification; transaction rollback on error result |
| **Production Patterns** | DPAPI key storage; graceful fallback when API key is absent; structured JSON error returns |

---

### Slide 9 — Roadmap

**Near term (v0.2)**

- Batch operations — run one instruction across multiple rooms or levels
- Richer model context — active level, view filters, recent edits injected into system prompt
- Safe-plan mode — wrap multi-tool plans in `TransactionGroup` for atomic undo

**Medium term (v0.3)**

- Multi-discipline tools — HVAC, plumbing, structural family placement
- Voice input — push-to-talk with EN/JP speech-to-text bridge

**Long term**

- Additional languages — Chinese (Simplified), Korean, Vietnamese
- Team prompt-template library — share approved AI recipes across an engineering office
- Revit 2027+ compatibility

---

### Slide 10 — Q&A and Thank You

**RevitMate**
*Natural language MEP design, powered by Claude*

- GitHub: https://github.com/themanh86/RevitMate/tree/master
- Contact: manh.nguyen@arentvn.com

*Thank you for your time.*

**Questions welcome.**
