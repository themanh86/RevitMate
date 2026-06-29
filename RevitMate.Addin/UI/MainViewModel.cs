using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using RevitMate.Addin.Audit;
using RevitMate.Addin.Executor;
using RevitMate.Core.Api;
using RevitMate.Core.Conversation;
using RevitMate.Core.Models;
using RevitMate.Core.Tools;
using RevitMate.Resources;

namespace RevitMate.Addin.UI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string SystemPrompt =
            "You are RevitMate, an AI assistant embedded in Autodesk Revit 2026, specialized in MEP " +
            "electrical design. Users may issue commands in English or Japanese. Analyze each command " +
            "and invoke the appropriate tool. If a command is ambiguous, ask a clarifying question " +
            "instead of guessing. " +
            "Before using any tool that modifies the model (create_light_fixture, set_parameter, " +
            "connect_to_circuit), you MUST first call propose_plan with a concise summary and an ordered " +
            "list of the concrete changes you will make. Only after the user approves the plan may you " +
            "call the modifying tools. If the plan is rejected, stop and ask the user how to proceed. " +
            "Read-only query tools may be called at any time without a plan. " +
            "After tools execute, summarize the result concisely in the same " +
            "language the user used (or in the language configured in settings if set explicitly).";

        private readonly ClaudeApiClient _client;
        private readonly PlanSession _planSession = new PlanSession();
        private string _currentInput = string.Empty;
        private bool _isLoading;
        private ICollection<ElementId> _selectionSnapshot = new List<ElementId>();
        private string _selectionSnapshotSummary = string.Empty;
        private PlanCardViewModel _activePlanCard;
        private bool _planExecutionFailed;
        private bool _planGroupOpen;
        private string _currentTurnText = string.Empty;

        public MainViewModel()
            : this(null)
        {
        }

        public MainViewModel(ClaudeApiClient client)
        {
            _client = client;
            Messages = new ObservableCollection<ChatMessage>();
            SendCommand = new RelayCommand(_ => { _ = SendAsync(); }, _ => CanSend());
            NewConversationCommand = new RelayCommand(_ => NewConversation());
            UseSuggestionCommand = new RelayCommand(p => UseSuggestion(p as string));
        }

        public ObservableCollection<ChatMessage> Messages { get; }

        public string CurrentInput
        {
            get => _currentInput;
            set
            {
                if (_currentInput == value) return;
                _currentInput = value ?? string.Empty;
                OnPropertyChanged();
                (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged();
                (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICollection<ElementId> SelectionSnapshot => _selectionSnapshot;

        public string SelectionSnapshotSummary
        {
            get => _selectionSnapshotSummary;
            private set
            {
                if (_selectionSnapshotSummary == value) return;
                _selectionSnapshotSummary = value;
                OnPropertyChanged();
            }
        }

        public bool HasSelectionSnapshot => _selectionSnapshot?.Count > 0;

        public void UpdateSelectionSnapshot(ICollection<ElementId> ids)
        {
            var incoming = ids ?? new List<ElementId>();
            Trace.WriteLine($"[UpdateSelectionSnapshot] incoming={incoming.Count}");
            SetSnapshot(incoming);
        }

        private void SetSnapshot(ICollection<ElementId> ids)
        {
            _selectionSnapshot = ids;
            OnPropertyChanged(nameof(SelectionSnapshot));
            OnPropertyChanged(nameof(HasSelectionSnapshot));
            SelectionSnapshotSummary = _selectionSnapshot.Count > 0
                ? "📌 " + string.Format(Strings.SelectionStatus, _selectionSnapshot.Count)
                : string.Empty;
        }

        public ICommand SendCommand { get; }
        public ICommand NewConversationCommand { get; }
        public ICommand UseSuggestionCommand { get; }

        private bool CanSend() => !IsLoading && !string.IsNullOrWhiteSpace(_currentInput);

        private async Task SendAsync()
        {
            string text = _currentInput?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Messages.Add(new ChatMessage(Role.User, text));
            CurrentInput = string.Empty;

            if (_client == null)
            {
                Messages.Add(new ChatMessage(Role.Assistant, Strings.ErrorInvalidApiKey));
                return;
            }

            // Snapshot the current selection IDs so we can inject them into both
            // the system prompt and the get_selected_elements tool input — the
            // DockablePane gains focus before the user hits Send, clearing the
            // live Revit selection.
            ICollection<ElementId> snapshot = _selectionSnapshot;
            string effectiveSystemPrompt = BuildSystemPrompt(snapshot);

            // Approval is scoped to a single turn — start each turn unapproved.
            _planSession.Reset();
            _activePlanCard = null;
            _planExecutionFailed = false;
            _planGroupOpen = false;
            _currentTurnText = text;

            IsLoading = true;
            try
            {
                var tools = RevitToolDefinitions.GetAllTools();
                MessageResponse response = await _client.SendMessageAsync(text, tools, effectiveSystemPrompt);

                while (true)
                {
                    bool hasToolUse = false;
                    foreach (ContentBlock block in response.Content)
                    {
                        if (block.Type == "text" && !string.IsNullOrEmpty(block.Text))
                            Messages.Add(new ChatMessage(Role.Assistant, block.Text));
                        else if (block.Type == "tool_use")
                        {
                            hasToolUse = true;
                            await HandleToolUseAsync(block, snapshot);
                        }
                    }

                    if (!hasToolUse) break;

                    response = await _client.SendMessageAsync(null, tools, effectiveSystemPrompt);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MainViewModel] SendAsync error: {ex}");
                Messages.Add(new ChatMessage(Role.Assistant,
                    string.Format(Strings.ErrorGeneric, ex.Message)));
            }
            finally
            {
                // Always close the undo group so it never lingers open across turns,
                // collapsing the whole approved plan into one undo step.
                if (_planGroupOpen)
                {
                    try
                    {
                        await ToolDispatcher.EndPlanAsync();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[MainViewModel] EndPlan error: {ex}");
                    }
                    _planGroupOpen = false;
                }

                FinalizePlanCard();
                IsLoading = false;
            }
        }

        private async Task HandleToolUseAsync(ContentBlock block, ICollection<ElementId> snapshot)
        {
            // propose_plan is a confirmation signal, not a Revit operation.
            if (block.Name == "propose_plan")
            {
                await HandleProposePlanAsync(block);
                return;
            }

            JObject toolInput = block.Input ?? new JObject();
            if (block.Name == "get_selected_elements" && snapshot?.Count > 0)
            {
                toolInput = (JObject)toolInput.DeepClone();
                toolInput["snapshot_ids"] = new JArray(snapshot.Select(id => id.Value));
            }

            if (!ToolDispatcher.IsMutating(block.Name))
            {
                await ExecuteReadOnlyAsync(block, toolInput);
                return;
            }

            // A mutating tool with an approved plan runs directly; otherwise it
            // falls back to a per-action confirmation card so nothing ever runs
            // unconfirmed, even if the model skipped propose_plan.
            if (_planSession.IsApproved)
                await ExecuteApprovedAsync(block, toolInput);
            else
                await ExecuteWithConfirmationAsync(block, toolInput);
        }

        // Tool result returned to Claude when the user cancels a pending action.
        private const string DeclinedToolResult =
            "{\"declined\":true,\"reason\":\"The user rejected this action. " +
            "Do not retry it; ask the user how they would like to proceed.\"}";

        // Tool results returned to Claude after the user decides on a proposed plan.
        private const string PlanApprovedToolResult =
            "{\"status\":\"approved\",\"note\":\"The user approved the plan. " +
            "You may now call the modifying tools to carry it out.\"}";

        private const string PlanRejectedToolResult =
            "{\"status\":\"rejected\",\"note\":\"The user rejected the plan. " +
            "Do not modify the model; ask the user how they would like to proceed.\"}";

        private async Task HandleProposePlanAsync(ContentBlock block)
        {
            var card = new PlanCardViewModel(block.Name, BuildPlanText(block.Input));
            _activePlanCard = card;
            Messages.Add(card);

            string summary = block.Input?["summary"]?.Value<string>();

            bool approved = await card.Decision;
            if (approved)
            {
                _planSession.Approve();
                await ToolDispatcher.BeginPlanAsync(BuildGroupName(block.Input));
                _planGroupOpen = true;
                AuditLogger.RecordPlanDecision("approved", _currentTurnText, summary);
                _client.AddToolResult(block.Id, PlanApprovedToolResult);
            }
            else
            {
                AuditLogger.RecordPlanDecision("rejected", _currentTurnText, summary);
                _client.AddToolResult(block.Id, PlanRejectedToolResult);
            }
        }

        // Labels the undo-stack entry for an approved plan, derived from its summary.
        private static string BuildGroupName(JObject input)
        {
            string summary = input?["summary"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(summary))
                return "RevitMate AI plan";

            summary = summary.Trim();
            const int maxLength = 80;
            if (summary.Length > maxLength)
                summary = summary.Substring(0, maxLength).TrimEnd() + "...";
            return "RevitMate: " + summary;
        }

        private async Task ExecuteApprovedAsync(ContentBlock block, JObject toolInput)
        {
            Messages.Add(new ChatMessage(Role.Action, $"{Strings.ExecutingPrefix} {block.Name}"));
            string result = await ToolDispatcher.ExecuteAsync(block.Name, toolInput);
            if (IsErrorResult(result))
                _planExecutionFailed = true;
            _client.AddToolResult(block.Id, result);
        }

        // Reflects the final outcome of an approved plan on its card once the turn ends.
        private void FinalizePlanCard()
        {
            if (_activePlanCard == null || !_planSession.IsApproved)
                return;

            if (_planExecutionFailed)
                _activePlanCard.MarkFailed();
            else
                _activePlanCard.MarkExecuted();
        }

        private static string BuildPlanText(JObject input)
        {
            input = input ?? new JObject();
            var sb = new StringBuilder();

            string summary = input["summary"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(summary))
                sb.AppendLine(summary.Trim());

            if (input["steps"] is JArray steps)
            {
                foreach (JToken step in steps)
                {
                    string action = step["action"]?.Value<string>();
                    string detail = step["detail"]?.Value<string>();
                    string line = string.IsNullOrWhiteSpace(action)
                        ? detail
                        : $"{action}: {detail}";
                    if (!string.IsNullOrWhiteSpace(line))
                        sb.AppendLine("- " + line.Trim());
                }
            }

            return sb.ToString().TrimEnd();
        }

        private async Task ExecuteReadOnlyAsync(ContentBlock block, JObject toolInput)
        {
            Messages.Add(new ChatMessage(Role.Action, $"{Strings.ExecutingPrefix} {block.Name}"));
            string result = await ToolDispatcher.ExecuteAsync(block.Name, toolInput);
            _client.AddToolResult(block.Id, result);
        }

        private async Task ExecuteWithConfirmationAsync(ContentBlock block, JObject toolInput)
        {
            string preview = await ToolDispatcher.PreviewAsync(block.Name, toolInput);
            var card = new PlanCardViewModel(block.Name, preview);
            Messages.Add(card);

            bool approved = await card.Decision;
            if (!approved)
            {
                _client.AddToolResult(block.Id, DeclinedToolResult);
                return;
            }

            string result = await ToolDispatcher.ExecuteAsync(block.Name, toolInput);
            if (IsErrorResult(result))
                card.MarkFailed();
            else
                card.MarkExecuted();
            _client.AddToolResult(block.Id, result);
        }

        private static bool IsErrorResult(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try { return JObject.Parse(json)["error"] != null; }
            catch { return false; }
        }

        private static string BuildSystemPrompt(ICollection<ElementId> snapshot)
        {
            if (snapshot == null || snapshot.Count == 0)
                return SystemPrompt;

            string ids = string.Join(", ", snapshot.Select(id => id.Value.ToString()));
            return SystemPrompt +
                $"\n\nCurrent Revit selection snapshot (captured before focus change): Element IDs: [{ids}]";
        }

        private void NewConversation()
        {
            _client?.ClearHistory();
            Messages.Clear();
            CurrentInput = string.Empty;
        }

        private void UseSuggestion(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion)) return;
            CurrentInput = suggestion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
