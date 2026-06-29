using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Newtonsoft.Json.Linq;
#pragma warning disable CS0618 // ElementSet is the required API type for AddToCircuit

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class ConnectToCircuitCommand : ICommandHandler, IPreviewable
    {
        public bool IsReadOnly => false;

        public string Preview(Document doc, JObject input)
        {
            List<long> elementIds = input["element_ids"]?.Values<long>()?.ToList();
            string panelName      = input["panel_name"]?.Value<string>();
            int circuitNumber     = input["circuit_number"]?.Value<int>() ?? 0;
            int count = elementIds?.Count ?? 0;

            var sb = new StringBuilder();
            sb.Append($"Connect {count} element(s) to circuit {circuitNumber} on panel '{panelName}'.");

            // Read-only lookup so the user sees the current load before confirming.
            ElectricalSystem circuit = FindCircuit(doc, panelName, circuitNumber);
            if (circuit == null)
                sb.Append($"\nWARNING: Circuit {circuitNumber} on panel '{panelName}' was not found.");
            else
                sb.Append($"\nCurrent circuit load: {circuit.ApparentLoad:0} VA.");

            return sb.ToString();
        }

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[connect_to_circuit] input={input}");
            try
            {
                List<long> elementIds = input["element_ids"]?.Values<long>()?.ToList();
                string panelName      = input["panel_name"]?.Value<string>();
                int circuitNumber     = input["circuit_number"]?.Value<int>() ?? 0;

                if (elementIds == null || elementIds.Count == 0)
                    return RevitExternalEventHandler.JsonError("element_ids is required and must not be empty.");
                if (string.IsNullOrWhiteSpace(panelName))
                    return RevitExternalEventHandler.JsonError("panel_name is required.");

                ElectricalSystem circuit = FindCircuit(doc, panelName, circuitNumber);
                if (circuit == null)
                    return RevitExternalEventHandler.JsonError(
                        $"Circuit {circuitNumber} on panel '{panelName}' not found.");

                var elementSet  = new ElementSet();
                var notFoundIds = new JArray();

                foreach (long idVal in elementIds)
                {
                    Element el = doc.GetElement(new ElementId(idVal));
                    if (el == null) { notFoundIds.Add(idVal); continue; }
                    elementSet.Insert(el);
                }

                if (elementSet.IsEmpty)
                    return RevitExternalEventHandler.JsonError(
                        "No valid elements found among the supplied ids.");

                circuit.AddToCircuit(elementSet);

                return new JObject
                {
                    ["panel_name"]       = circuit.PanelName,
                    ["circuit_number"]   = circuit.CircuitNumber,
                    ["apparent_load_va"] = circuit.ApparentLoad,
                    ["connected_count"]  = elementSet.Size,
                    ["not_found_ids"]    = notFoundIds,
                }.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }

        internal static ElectricalSystem FindCircuit(Document doc, string panelName, int circuitNumber)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ElectricalSystem))
                .Cast<ElectricalSystem>()
                .FirstOrDefault(es =>
                    string.Equals(es.PanelName, panelName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(es.CircuitNumber, circuitNumber.ToString(), StringComparison.Ordinal));
        }

    }
}
