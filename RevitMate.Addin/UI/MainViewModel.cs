using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using RevitMate.Addin.Executor;
using RevitMate.Core.Api;
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
            "instead of guessing. After tools execute, summarize the result concisely in the same " +
            "language the user used (or in the language configured in settings if set explicitly).";

        private readonly ClaudeApiClient _client;
        private string _currentInput = string.Empty;
        private bool _isLoading;
        private ICollection<ElementId> _selectionSnapshot = new List<ElementId>();
        private string _selectionSnapshotSummary = string.Empty;

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
                            Messages.Add(new ChatMessage(Role.Action,
                                $"{Strings.ExecutingPrefix} {block.Name}"));

                            JObject toolInput = block.Input ?? new JObject();
                            if (block.Name == "get_selected_elements" && snapshot?.Count > 0)
                            {
                                toolInput = (JObject)toolInput.DeepClone();
                                toolInput["snapshot_ids"] = new JArray(snapshot.Select(id => id.Value));
                            }

                            string result = await ToolDispatcher.ExecuteAsync(block.Name, toolInput);
                            _client.AddToolResult(block.Id, result);
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
                IsLoading = false;
            }
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
