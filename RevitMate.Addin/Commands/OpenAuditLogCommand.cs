using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMate.Addin.UI;

namespace RevitMate.Addin.Commands
{
    /// <summary>
    /// Ribbon button command that opens the RevitMate audit-log viewer.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class OpenAuditLogCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new AuditLogWindow();
            new WindowInteropHelper(window)
            {
                Owner = commandData.Application.MainWindowHandle
            };

            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
