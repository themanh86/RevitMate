using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMate.Addin.UI;

namespace RevitMate.Addin.Commands
{
    /// <summary>
    /// Ribbon button command that opens the RevitMate settings dialog.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class OpenSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new SettingsWindow();
            var helper = new WindowInteropHelper(window)
            {
                Owner = commandData.Application.MainWindowHandle
            };

            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
