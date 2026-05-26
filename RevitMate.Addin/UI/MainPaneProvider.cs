using Autodesk.Revit.UI;
using RevitMate.Core.Api;
using RevitMate.Resources;

namespace RevitMate.Addin.UI
{
    /// <summary>
    /// Hosts <see cref="MainPane"/> inside a Revit dockable pane.
    /// Revit constructs the provider once per session and reuses the
    /// returned <see cref="System.Windows.FrameworkElement"/>, so we keep a single pane instance.
    /// </summary>
    public class MainPaneProvider : IDockablePaneProvider
    {
        private readonly MainPane _pane;

        public MainPaneProvider(ClaudeApiClient client)
        {
            _pane = new MainPane(new MainViewModel(client));
        }

        public MainViewModel ViewModel => _pane.ViewModel;

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = _pane;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right
            };
            data.VisibleByDefault = false;
            data.EditorInteraction = new EditorInteraction(EditorInteractionType.Dismiss);
        }

        public static string Title => Strings.PanelTitle;
    }
}
