using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using RevitMate.Resources;

namespace RevitMate.Addin.UI
{
    /// <summary>
    /// Lifecycle of a confirmation card from creation to a terminal state.
    /// </summary>
    public enum PlanCardStatus
    {
        Pending,
        Approved,
        Rejected,
        Executed,
        Failed
    }

    /// <summary>
    /// A chat entry that asks the user to confirm a model-mutating action before
    /// it runs. Renders Confirm / Cancel buttons; the agentic loop awaits
    /// <see cref="Decision"/> and only executes the tool once the user approves.
    /// </summary>
    public sealed class PlanCardViewModel : ChatMessage, INotifyPropertyChanged
    {
        private readonly TaskCompletionSource<bool> _decision =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private PlanCardStatus _status = PlanCardStatus.Pending;

        public PlanCardViewModel(string toolName, string previewText)
            : base(Role.Plan, previewText)
        {
            ToolName = toolName ?? string.Empty;
            ApproveCommand = new RelayCommand(_ => Decide(true), _ => _status == PlanCardStatus.Pending);
            RejectCommand = new RelayCommand(_ => Decide(false), _ => _status == PlanCardStatus.Pending);
        }

        /// <summary>Name of the tool this card gates (e.g. <c>create_light_fixture</c>).</summary>
        public string ToolName { get; }

        /// <summary>Completes with the user's decision: <c>true</c> = confirm, <c>false</c> = cancel.</summary>
        public Task<bool> Decision => _decision.Task;

        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        public PlanCardStatus Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPending));
                OnPropertyChanged(nameof(IsDecided));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        /// <summary>True while awaiting the user's decision — buttons are shown.</summary>
        public bool IsPending => _status == PlanCardStatus.Pending;

        /// <summary>True once a decision has been made — a status line is shown.</summary>
        public bool IsDecided => _status != PlanCardStatus.Pending;

        /// <summary>Localized status line shown after the user decides.</summary>
        public string StatusText
        {
            get
            {
                switch (_status)
                {
                    case PlanCardStatus.Approved: return Strings.PlanRunning;
                    case PlanCardStatus.Executed: return Strings.PlanExecuted;
                    case PlanCardStatus.Failed:   return Strings.PlanFailed;
                    case PlanCardStatus.Rejected: return Strings.PlanRejected;
                    default:                      return string.Empty;
                }
            }
        }

        /// <summary>Marks the action as executed successfully.</summary>
        public void MarkExecuted() => Status = PlanCardStatus.Executed;

        /// <summary>Marks the action as failed during execution.</summary>
        public void MarkFailed() => Status = PlanCardStatus.Failed;

        private void Decide(bool approved)
        {
            if (_status != PlanCardStatus.Pending) return;
            Status = approved ? PlanCardStatus.Approved : PlanCardStatus.Rejected;
            _decision.TrySetResult(approved);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
