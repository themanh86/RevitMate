using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMate.Addin.UI;

namespace RevitMate.Addin.Commands
{
    /// <summary>
    /// Ribbon button command that shows the RevitMate dockable pane.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class OpenRevitMateCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            DockablePane pane = commandData.Application.GetDockablePane(PaneIds.MainPaneDockablePaneId);
            if (pane == null)
            {
                message = "RevitMate pane is not registered.";
                return Result.Failed;
            }

            Application.SetRevitApp(commandData.Application);
            pane.Show();
            return Result.Succeeded;
        }
    }
}
