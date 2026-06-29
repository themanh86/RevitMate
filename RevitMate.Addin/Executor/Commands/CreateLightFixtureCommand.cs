using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class CreateLightFixtureCommand : ICommandHandler, IPreviewable
    {
        // Default X-spacing when placing multiple fixtures in a row.
        private const double DefaultSpacingMm = 2000;

        public bool IsReadOnly => false;

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[create_light_fixture] input={input}");
            try
            {
                string familyName = input["family_name"]?.Value<string>();
                string levelName  = input["level_name"]?.Value<string>();
                double gridXmm    = input["grid_x_mm"]?.Value<double>() ?? 0;
                double gridYmm    = input["grid_y_mm"]?.Value<double>() ?? 0;
                int    count      = input["count"]?.Value<int>() ?? 1;

                if (string.IsNullOrWhiteSpace(familyName))
                    return RevitExternalEventHandler.JsonError("family_name is required.");
                if (string.IsNullOrWhiteSpace(levelName))
                    return RevitExternalEventHandler.JsonError("level_name is required.");
                if (count < 1)
                    return RevitExternalEventHandler.JsonError("count must be at least 1.");

                FamilySymbol symbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(s => string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));

                if (symbol == null)
                    return RevitExternalEventHandler.JsonError($"Family '{familyName}' not found in the project.");

                Level level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));

                if (level == null)
                    return RevitExternalEventHandler.JsonError($"Level '{levelName}' not found.");

                if (!symbol.IsActive)
                    symbol.Activate();

                double originX  = UnitUtils.ConvertToInternalUnits(gridXmm, UnitTypeId.Millimeters);
                double originY  = UnitUtils.ConvertToInternalUnits(gridYmm, UnitTypeId.Millimeters);
                double spacingX = UnitUtils.ConvertToInternalUnits(DefaultSpacingMm, UnitTypeId.Millimeters);

                var createdIds = new JArray();
                for (int i = 0; i < count; i++)
                {
                    var point = new XYZ(originX + i * spacingX, originY, level.Elevation);
                    FamilyInstance inst = doc.Create.NewFamilyInstance(
                        point, symbol, level, StructuralType.NonStructural);
                    createdIds.Add(inst.Id.Value);
                }

                return new JObject
                {
                    ["created_count"] = count,
                    ["element_ids"]   = createdIds,
                }.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }

        public string Preview(Document doc, JObject input)
        {
            string familyName = input["family_name"]?.Value<string>();
            string levelName  = input["level_name"]?.Value<string>();
            double gridXmm    = input["grid_x_mm"]?.Value<double>() ?? 0;
            double gridYmm    = input["grid_y_mm"]?.Value<double>() ?? 0;
            int    count      = input["count"]?.Value<int>() ?? 1;

            var sb = new StringBuilder();
            sb.Append($"Place {count} '{familyName}' fixture(s) on level '{levelName}', ");
            sb.Append($"grid starting at ({gridXmm:0.#}, {gridYmm:0.#}) mm.");

            // Read-only validation so the user can cancel before anything is committed.
            bool familyExists = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Any(s => string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));
            if (!familyExists)
                sb.Append($"\nWARNING: Family '{familyName}' was not found in this project.");

            bool levelExists = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Any(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            if (!levelExists)
                sb.Append($"\nWARNING: Level '{levelName}' was not found.");

            return sb.ToString();
        }
    }
}
