using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class SetParameterCommand : ICommandHandler, IPreviewable
    {
        public bool IsReadOnly => false;

        public string Preview(Document doc, JObject input)
        {
            List<long> elementIds = input["element_ids"]?.Values<long>()?.ToList();
            string paramName      = input["parameter_name"]?.Value<string>();
            JToken valueToken     = input["value"];

            int count = elementIds?.Count ?? 0;
            string value = valueToken?.ToString() ?? string.Empty;
            return $"Set parameter '{paramName}' = '{value}' on {count} element(s).";
        }

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[set_parameter] input={input}");
            try
            {
                List<long> elementIds = input["element_ids"]?.Values<long>()?.ToList();
                string paramName      = input["parameter_name"]?.Value<string>();
                JToken valueToken     = input["value"];

                if (elementIds == null || elementIds.Count == 0)
                    return RevitExternalEventHandler.JsonError("element_ids is required and must not be empty.");
                if (string.IsNullOrWhiteSpace(paramName))
                    return RevitExternalEventHandler.JsonError("parameter_name is required.");
                if (valueToken == null)
                    return RevitExternalEventHandler.JsonError("value is required.");

                var results = new JArray();
                foreach (long idVal in elementIds)
                {
                    Element el = doc.GetElement(new ElementId(idVal));
                    if (el == null)
                    {
                        results.Add(Fail(idVal, $"Element {idVal} not found."));
                        continue;
                    }

                    Parameter param = el.LookupParameter(paramName);
                    if (param == null)
                    {
                        results.Add(Fail(idVal, $"Parameter '{paramName}' not found."));
                        continue;
                    }
                    if (param.IsReadOnly)
                    {
                        results.Add(Fail(idVal, $"Parameter '{paramName}' is read-only."));
                        continue;
                    }

                    try
                    {
                        ApplyValue(param, valueToken);
                        results.Add(new JObject { ["element_id"] = idVal, ["success"] = true });
                    }
                    catch (Exception ex)
                    {
                        results.Add(Fail(idVal, ex.Message));
                    }
                }

                return new JObject { ["results"] = results }.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }

        private static void ApplyValue(Parameter param, JToken value)
        {
            switch (param.StorageType)
            {
                case StorageType.String:
                    param.Set(value.Value<string>());
                    break;
                case StorageType.Integer:
                    param.Set(value.Value<int>());
                    break;
                case StorageType.Double:
                    param.Set(value.Value<double>());
                    break;
                default:
                    throw new NotSupportedException(
                        $"Cannot set parameter of storage type {param.StorageType}.");
            }
        }

        private static JObject Fail(long id, string message)
            => new JObject { ["element_id"] = id, ["success"] = false, ["error"] = message };
    }
}
