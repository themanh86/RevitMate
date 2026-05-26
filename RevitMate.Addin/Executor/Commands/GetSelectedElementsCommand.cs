using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class GetSelectedElementsCommand : ICommandHandler
    {
        public bool IsReadOnly => true;

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[get_selected_elements] input={input}");
            try
            {
                ICollection<ElementId> selectedIds = ResolveIds(doc, input);

                var elements = new JArray();
                foreach (ElementId id in selectedIds)
                {
                    Element el = doc.GetElement(id);
                    if (el == null) continue;

                    var obj = new JObject
                    {
                        ["element_id"]  = id.Value,
                        ["category"]    = el.Category?.Name ?? string.Empty,
                        ["name"]        = el.Name,
                        ["family_name"] = el is FamilyInstance fi ? fi.Symbol?.FamilyName ?? string.Empty : string.Empty,
                        ["type_name"]   = doc.GetElement(el.GetTypeId())?.Name ?? string.Empty,
                    };

                    var paramData = new JObject();
                    int collected = 0;
                    foreach (Parameter p in el.Parameters)
                    {
                        if (collected >= 30) break;
                        if (!p.HasValue) continue;
                        string val = ParameterAsString(p);
                        if (val != null)
                        {
                            paramData[p.Definition.Name] = val;
                            collected++;
                        }
                    }
                    obj["parameters"] = paramData;
                    elements.Add(obj);
                }

                return new JObject
                {
                    ["count"]    = elements.Count,
                    ["elements"] = elements,
                }.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }

        private static ICollection<ElementId> ResolveIds(Document doc, JObject input)
        {
            var snapshotArray = input?["snapshot_ids"] as JArray;
            if (snapshotArray != null && snapshotArray.Count > 0)
            {
                var ids = new List<ElementId>(snapshotArray.Count);
                foreach (var token in snapshotArray)
                {
                    if (long.TryParse(token.ToString(), out long val))
                        ids.Add(new ElementId(val));
                }
                if (ids.Count > 0) return ids;
            }

            return new UIDocument(doc).Selection.GetElementIds();
        }

        private static string ParameterAsString(Parameter p)
        {
            switch (p.StorageType)
            {
                case StorageType.String:    return p.AsString();
                case StorageType.Integer:   return p.AsInteger().ToString();
                case StorageType.Double:    return Math.Round(p.AsDouble(), 4).ToString();
                case StorageType.ElementId: return p.AsElementId()?.Value.ToString();
                default:                   return null;
            }
        }
    }
}
