using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public sealed class GetRoomInfoCommand : ICommandHandler
    {
        public bool IsReadOnly => true;

        public string Execute(Document doc, JObject input)
        {
            Trace.WriteLine($"[get_room_info] input={input}");
            try
            {
                long? roomId   = input["room_id"]?.Value<long>();
                string roomName = input["room_name"]?.Value<string>();

                IEnumerable<Room> rooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>();

                if (roomId.HasValue)
                    rooms = rooms.Where(r => r.Id.Value == roomId.Value);
                else if (!string.IsNullOrEmpty(roomName))
                    rooms = rooms.Where(r => r.Name.IndexOf(roomName, StringComparison.OrdinalIgnoreCase) >= 0);

                var array = new JArray();
                foreach (Room room in rooms)
                {
                    var obj = new JObject
                    {
                        ["room_id"] = room.Id.Value,
                        ["name"]    = room.Name,
                        ["number"]  = room.Number,
                        ["area_m2"] = Math.Round(
                            UnitUtils.ConvertFromInternalUnits(room.Area, UnitTypeId.SquareMeters), 3),
                        ["level"]   = doc.GetElement(room.LevelId)?.Name ?? string.Empty,
                    };

                    BoundingBoxXYZ bb = room.get_BoundingBox(null);
                    if (bb != null)
                    {
                        XYZ center = (bb.Min + bb.Max).Multiply(0.5);
                        obj["center_mm"] = new JObject
                        {
                            ["x"] = Math.Round(UnitUtils.ConvertFromInternalUnits(center.X, UnitTypeId.Millimeters), 1),
                            ["y"] = Math.Round(UnitUtils.ConvertFromInternalUnits(center.Y, UnitTypeId.Millimeters), 1),
                            ["z"] = Math.Round(UnitUtils.ConvertFromInternalUnits(center.Z, UnitTypeId.Millimeters), 1),
                        };
                    }

                    array.Add(obj);
                }

                return new JObject
                {
                    ["count"] = array.Count,
                    ["rooms"] = array,
                }.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                return RevitExternalEventHandler.JsonError(ex.Message);
            }
        }
    }
}
