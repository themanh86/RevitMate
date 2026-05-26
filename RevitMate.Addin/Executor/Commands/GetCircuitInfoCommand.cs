using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class GetCircuitInfoCommand : ICommandHandler
    {
        public bool IsReadOnly => true;

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[get_circuit_info] input={input}");
            try
            {
                string panelName  = input["panel_name"]?.Value<string>();
                int circuitNumber = input["circuit_number"]?.Value<int>() ?? 0;

                if (string.IsNullOrWhiteSpace(panelName))
                    return RevitExternalEventHandler.JsonError("panel_name is required.");

                ElectricalSystem circuit = ConnectToCircuitCommand.FindCircuit(doc, panelName, circuitNumber);
                if (circuit == null)
                    return RevitExternalEventHandler.JsonError(
                        $"Circuit {circuitNumber} on panel '{panelName}' not found.");

                var connectedIds = new JArray();
                foreach (Element el in circuit.Elements)
                    connectedIds.Add(el.Id.Value);

                return new JObject
                {
                    ["panel_name"]            = circuit.PanelName,
                    ["circuit_number"]        = circuit.CircuitNumber,
                    ["system_type"]           = circuit.SystemType.ToString(),
                    ["voltage"]               = circuit.Voltage,
                    ["apparent_load_va"]      = circuit.ApparentLoad,
                    ["rating_amps"]           = GetRatingAmps(circuit),
                    ["connected_count"]       = connectedIds.Count,
                    ["connected_element_ids"] = connectedIds,
                }.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }

        private static double GetRatingAmps(ElectricalSystem circuit)
        {
            Parameter p = circuit.LookupParameter("Rating");
            if (p != null && p.HasValue && p.StorageType == StorageType.Double)
                return Math.Round(p.AsDouble(), 2);
            return 0;
        }
    }
}
