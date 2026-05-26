# RevitMate — 画面収録デモスクリプト
## 3〜5 分版

---

### 収録前チェックリスト

- [ ] Revit 2026 を起動し、MEP サンプルプロジェクト（電気分野、1階平面図）を開いておく
- [ ] RevitMate パネルを右側にドッキングして表示しておく
- [ ] 天井がある「Office 101」という部屋を器具配置用に準備しておく
- [ ] 電気パネル「LP-1」に回路 3 を作成済み（空の状態）にしておく
- [ ] 1920×1080 で画面収録、マイクをオンにする
- [ ] すべての通知とブラウザタブを閉じておく

---

### 00:00〜00:30 — イントロ

**[画面: RevitMate パネルが表示された Revit]**

> 「こちらは RevitMate です。Anthropic の Claude API を活用して、Autodesk Revit 2026 の中に直接組み込まれた自然言語 AI アシスタントです。」

- リボンの「RevitMate」タブを指し示す。
- **RevitMate を開く** ボタンをクリックし、ドッキングペインが開く様子を見せる。
- チャットパネルを指し示す: 下部の入力ボックス、サジェストチップ、ティール色のステータスドット。

> 「メニューを探したりダイアログをクリックしたりする代わりに、やりたいことを英語か日本語で伝えるだけで、RevitMate が Revit の操作を代行してくれます。」

---

### 00:30〜01:30 — ユースケース 1: 非常用照明器具をグリッド配置（英語コマンド）

**[画面: 「Office 101」が表示された平面図]**

- 平面図で部屋要素を選択する。パネルに緑色の **📌 1 つの要素が選択されました** ピルが表示される。

> 「部屋を選択しました。AI が確認なしで即座に実行できるよう、必要な情報をすべて含んだコマンドを入力します。」

**[チャットボックスに入力]**

```
Place 4 instances of family "20501_K1-RS11_非常用照明器具_埋込天井灯" in a 2×2 grid
with 2 000 mm spacing, centered in the selected room.
Reply with the element IDs of every created instance.
```

- **Enter** キー（または **送信** ボタン）を押す。
- プログレスバーが表示される。アクションバブルが紫色・斜体で順に現れる:
  - *Executing get_selected_elements*
  - *Executing get_room_info*
  - *Executing create_light_fixture*

> 「3 ステップのトレースに注目してください。Claude がまず選択内容を読み取り、次に部屋を測定して中心点を求め、そこから ±1,000 mm の 4 点を計算してから create_light_fixture を呼び出しています。」

- 最終的な応答バブルが表示される:

  > "✓ Placed 4 instances of '20501_K1-RS11_非常用照明器具_埋込天井灯' on Level 1, centred at the room midpoint.
  >
  > Grid layout (2 × 2, 2 000 mm spacing):
  > &nbsp; NW: **305411** &nbsp; NE: **305412**
  > &nbsp; SW: **305413** &nbsp; SE: **305414**"

- 平面図にパンして、4 台の器具が部屋の中心に対称配置されていることを確認する。

> 「4 台の器具が部屋の中心に正確に配置され、Element ID もすぐに返ってきました。この ID は次のコマンドで参照したり、Revit で直接選択したりするのに使えます。Ctrl+Z で 4 台まとめて取り消せます。」

- **Ctrl+Z** を押して器具が消えることを実演する。
- **Ctrl+Y** で器具が戻ることを確認する。

---

### 01:30〜02:30 — ユースケース 2: 回路への接続 + 負荷確認（日本語コマンド）

**[画面: 先ほど配置した 4 台の器具を選択]**

- 先ほど返ってきた Element ID を使って 4 台の器具を選択する（ID 指定選択または範囲選択）。ピルが **📌 4 つの要素が選択されました** に更新される。

> 「次は日本語に切り替えます。RevitMate は完全なバイリンガル対応です。」

**[チャットボックスに入力]**

```
選択中の器具をパネルLP-1の回路3に接続して。
```

- **Enter** キーを押す。
- アクションバブル:
  - *Executing get_selected_elements*
  - *Executing connect_to_circuit*
  - *Executing get_circuit_info*

- 最終応答（日本語）:

  > 「✓ 4台の器具を回路 LP-1 / 3 に接続しました。接続後の合計負荷: 480 VA（定格 2,000 VA の 24%）」

> 「入力した言語が日本語だったため、AI も日本語で返答しました。12 台すべての器具を接続し、回路負荷を即座にレポートしています。定格の 72% です。」

---

### 02:30〜03:30 — ユースケース 3: 回路の過負荷チェック（英語）

**[画面: チャットパネル、選択操作は不要]**

> 「回路が過負荷になっていないか、今度は英語で確認してみます。」

**[チャットボックスに入力]**

```
Is circuit 3 on LP-1 overloaded?
```

- **Enter** キーを押す。
- ツール呼び出しは 1 回: *Executing get_circuit_info*
- 応答:

  > "Circuit LP-1 / 3 — 1,440 VA / 2,000 VA (72%). No overload. Headroom: 560 VA. Safe to add up to 4 more 100 W fixtures."

> 「読み取り専用クエリなのでモデルへの変更はなく、即座に回答が返ってきます。さらに、ブレーカーが落ちる前にあと何台追加できるかまで教えてくれています。」

---

### 03:30〜04:30 — 舞台裏: Claude API ツールユースの仕組み

**[画面: 左に Revit、右にコードエディタまたは JSON ビューア]**

> 「裏側で何が起きているかお見せします。」

- `MainViewModel.cs` またはキャプチャしたリクエスト/レスポンスの JSON ログを開く。

> 「メッセージを送信すると、RevitMate はメッセージと 7 つのツールスキーマをまとめて Anthropic の Messages API に送信します。」

**送信ペイロードの構造を表示:**

```json
{
  "model": "claude-sonnet-4-5",
  "system": "You are RevitMate, an AI assistant embedded in Revit 2026...",
  "tools": [ { "name": "create_light_fixture", "description": "...", "input_schema": {...} } ],
  "messages": [ { "role": "user", "content": "Place a 3×4 grid..." } ]
}
```

> 「Claude は `tool_use` ブロック — 構造化された JSON 形式の呼び出し — で応答します。」

**`tool_use` レスポンスを表示:**

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

> 「RevitMate はこれをエグゼキューターに渡し、ExternalEvent 経由でメインスレッドの Revit API に送ります。結果の JSON は `tool_result` として Claude に返り、Claude がテキストを返すまでこのループが続きます。」

---

### 04:30〜05:00 — アウトロ

**[画面: RevitMate チャットパネル、全体表示]**

> 「RevitMate は、Anthropic のツールユース API がホストアプリケーションのコマンドセットに自然にマッピングできることを示しています。7 つのツールと丁寧に設計されたシステムプロンプトだけで、複雑なマルチステップの MEP ワークフローが会話形式で実現できます。」

> 「ソースコードは [GitHub リンク] で公開しています。ご視聴ありがとうございました。」

**[タイトルカードにフェードアウト: RevitMate · github.com/… · Powered by Claude]**

---

### 収録後のメモ

- タイピングミスはカット。ツール実行トレースは全体を見せること。
- 英語コマンドのセグメント（ユースケース 1・3）には日本語字幕を追加することを推奨。
- 推奨エクスポート設定: MP4、1080p、30fps、BGM は −20 dB にフェードダウン。
