using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class GetActiveViewInfoCommand : ICommandHandler
    {
        public bool IsReadOnly => true;

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[get_active_view_info] input={input}");
            try
            {
                View view = doc.ActiveView;
                if (view == null)
                    return RevitExternalEventHandler.JsonError("No active view.");

                var result = new JObject
                {
                    ["view_name"]  = view.Name,
                    ["view_type"]  = view.ViewType.ToString(),
                    ["scale"]      = view.Scale,
                    ["discipline"] = view.Discipline.ToString(),
                    ["level_name"] = string.Empty,
                };

                if (view is ViewPlan vp)
                    result["level_name"] = vp.GenLevel?.Name ?? string.Empty;

                return result.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }
    }
}
