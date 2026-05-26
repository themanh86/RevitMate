# RevitMate — Screen-Recording Demo Script
## 3–5 Minute Version

---

### Pre-recording Checklist

- [ ] Revit 2026 open with a sample MEP project (electrical discipline, Level 1 floor plan)
- [ ] RevitMate panel docked on the right side, visible
- [ ] A room named "Office 101" with a ceiling, ready for fixture placement
- [ ] An electrical panel "LP-1" with circuit 3 already created (empty)
- [ ] Screen recorder capturing 1920×1080, microphone on
- [ ] Close all notifications and browser tabs

---

### 00:00–00:30 — Introduction

**[Screen: Revit with RevitMate panel visible]**

> "Welcome to RevitMate — a natural-language AI assistant embedded directly inside Autodesk Revit 2026, powered by the Claude API."

- Point to the RevitMate tab in the ribbon.
- Click **Open RevitMate** to show the dockable pane opening.
- Point out the chat panel: input box at the bottom, suggestion chips, teal status dot.

> "Instead of navigating menus and clicking through dialogs, I can describe what I want in plain English or Japanese, and RevitMate handles the Revit operations for me."

---

### 00:30–01:30 — Use Case 1: Place Emergency Lights in a Grid (English)

**[Screen: Floor plan with room "Office 101" selected]**

- Select the room element in the floor plan. The green **📌 1 element selected** pill appears in the panel.

> "I've selected a room. I'll give RevitMate a fully specified command so it can act immediately without asking any follow-up questions."

**[Type in the chat box]**

```
Place 4 instances of family "20501_K1-RS11_非常用照明器具_埋込天井灯" in a 2×2 grid
with 2 000 mm spacing, centered in the selected room.
Reply with the element IDs of every created instance.
```

- Press **Enter** (or click **Send**).
- The progress bar appears. Action bubbles appear in purple/italic:
  - *Executing get_selected_elements*
  - *Executing get_room_info*
  - *Executing create_light_fixture*

> "Notice the three-step trace. Claude reads the selection, measures the room to find its centre, then computes the four grid positions at ±1 000 mm from that centre before calling create_light_fixture."

- Final response bubble appears:

  > "✓ Placed 4 instances of '20501_K1-RS11_非常用照明器具_埋込天井灯' on Level 1, centred at the room midpoint.
  >
  > Grid layout (2 × 2, 2 000 mm spacing):
  > &nbsp; NW: **305411** &nbsp; NE: **305412**
  > &nbsp; SW: **305413** &nbsp; SE: **305414**"

- Pan to the floor plan to show the 4 fixtures placed symmetrically in the room centre.

> "Four fixtures, precisely centred, with element IDs returned immediately — ready to reference in the next command or to select directly in Revit. If the result isn't right, Ctrl+Z removes all four in one undo step."

- **Demonstrate Ctrl+Z** — all 4 fixtures disappear.
- **Ctrl+Y** — fixtures come back.

---

### 01:30–02:30 — Use Case 2: Connect to Circuit + Check Load (Japanese)

**[Screen: Select the 4 emergency light fixtures just placed]**

- Drag-select all 4 fixtures (or type the IDs into the Revit selection box). The pill updates to **📌 4 elements selected**.

> "Now I'll switch to Japanese — RevitMate is fully bilingual."

**[Type in the chat box]**

```
選択中の器具をパネルLP-1の回路3に接続して。
```

- Press **Enter**.
- Action bubbles:
  - *Executing get_selected_elements*
  - *Executing connect_to_circuit*
  - *Executing get_circuit_info*

- Final response (in Japanese):

  > "✓ 4台の器具を回路 LP-1 / 3 に接続しました。接続後の合計負荷: 480 VA（定格 2,000 VA の 24%）"

> "The AI replied in Japanese because that's the language I used. It connected all 4 fixtures and immediately reported the updated circuit load."

---

### 02:30–03:30 — Use Case 3: Check Circuit Overload (English)

**[Screen: Chat panel, no selection needed]**

> "Let me verify the circuit is not overloaded, this time in English."

**[Type in the chat box]**

```
Is circuit 3 on LP-1 overloaded?
```

- Press **Enter**.
- Single tool call: *Executing get_circuit_info*
- Response:

  > "Circuit LP-1 / 3 — 1,440 VA / 2,000 VA (72%). No overload. Headroom: 560 VA. Safe to add up to 4 more 100 W fixtures."

> "A read-only query — no changes to the model, instant answer. RevitMate even tells me how many more fixtures I can add before tripping the breaker."

---

### 03:30–04:30 — Behind the Scenes: Claude API Tool Use

**[Screen: Split — Revit on left, code editor or JSON viewer on right]**

> "Let me show you what's happening under the hood."

- Open `MainViewModel.cs` or a captured request/response JSON log.

> "When I send a message, RevitMate packages it with the seven tool schemas and posts it to the Anthropic Messages API."

**Show the outgoing payload structure:**

```json
{
  "model": "claude-sonnet-4-5",
  "system": "You are RevitMate, an AI assistant embedded in Revit 2026...",
  "tools": [ { "name": "create_light_fixture", "description": "...", "input_schema": {...} } ],
  "messages": [ { "role": "user", "content": "Place a 3×4 grid..." } ]
}
```

> "Claude responds with a `tool_use` block — a structured JSON call."

**Show the `tool_use` response:**

```json
{
  "type": "tool_use",
  "name": "create_light_fixture",
  "input": {
    "family_name": "Recessed Can Light",
    "level_name": "Level 1",
    "grid_x_mm": 1500,
    "grid_y_mm": 2000,
    "count": 12,
    "room_id": 305412
  }
}
```

> "RevitMate feeds this to the Executor, which dispatches to the Revit API on the main thread via ExternalEvent. The result JSON goes back to Claude as a `tool_result`, and the loop continues until Claude sends plain text."

---

### 04:30–05:00 — Outro

**[Screen: RevitMate chat panel, full view]**

> "RevitMate shows that the Anthropic tool-use API maps naturally onto a host application's command set. With seven tools and a well-crafted system prompt, complex multi-step MEP workflows become conversational."

> "The source code is available at [GitHub link]. Thank you for watching."

**[Fade to title card: RevitMate · github.com/… · Powered by Claude]**

---

### Post-recording Notes

- Edit out any typing mistakes; keep the full tool-call trace visible.
- Add captions for the Japanese segment (Use Case 2) for accessibility.
- Recommended export: MP4, 1080p, 30 fps, with a background music track faded to –20 dB.
