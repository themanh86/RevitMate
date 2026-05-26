using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace RevitMate.Addin.UI
{
    public partial class MainPane : UserControl
    {
        public MainPane()
            : this(new MainViewModel())
        {
        }

        public MainPane(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.Messages.CollectionChanged += OnMessagesChanged;
            Loaded += (s, e) =>
            {
                if (PresentationSource.FromVisual(this) is HwndSource src)
                    Application.SetPaneHwnd(src.Handle);
            };
            Unloaded += (s, e) => Application.SetPaneHwnd(System.IntPtr.Zero);
        }

        public MainViewModel ViewModel => DataContext as MainViewModel;

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                ConversationScroll.ScrollToEnd();
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                return;

            if (DataContext is MainViewModel vm && vm.SendCommand.CanExecute(null))
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
