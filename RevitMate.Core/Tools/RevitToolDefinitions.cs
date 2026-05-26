using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RevitMate.Core.Models;

namespace RevitMate.Core.Tools
{
    public static class RevitToolDefinitions
    {
        public static List<Tool> GetAllTools()
        {
            return new List<Tool>
            {
                GetSelectedElements(),
                GetActiveViewInfo(),
                GetRoomInfo(),
                CreateLightFixture(),
                SetParameter(),
                ConnectToCircuit(),
                GetCircuitInfo(),
            };
        }

        private static Tool GetSelectedElements()
        {
            return new Tool
            {
                Name = "get_selected_elements",
                Description =
                    "Returns a list of elements currently selected in Revit. " +
                    "Each entry includes the element id, category, family name, type name, " +
                    "and a dictionary of key parameter values. " +
                    "Use this tool first when the user refers to 'this', 'these', or 'selected' elements.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {},
                    ""required"": []
                }"),
            };
        }

        private static Tool GetActiveViewInfo()
        {
            return new Tool
            {
                Name = "get_active_view_info",
                Description =
                    "Returns metadata about the currently active Revit view: " +
                    "view name, view type (FloorPlan, Section, 3D, etc.), associated level name, " +
                    "scale, and discipline. " +
                    "Use this to understand the user's working context before placing or querying elements.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {},
                    ""required"": []
                }"),
            };
        }

        private static Tool GetRoomInfo()
        {
            return new Tool
            {
                Name = "get_room_info",
                Description =
                    "Returns detailed information about one or more rooms: name, number, area (m²), " +
                    "level, bounding-box center point (mm), and the element ids of all MEP elements " +
                    "located inside the room. " +
                    "Provide either room_id or room_name to target a specific room; " +
                    "omit both to return info for all rooms on the active level.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""room_id"": {
                            ""type"": ""integer"",
                            ""description"": ""Revit element id of the target room.""
                        },
                        ""room_name"": {
                            ""type"": ""string"",
                            ""description"": ""Name of the target room (case-insensitive substring match).""
                        }
                    },
                    ""required"": []
                }"),
            };
        }

        private static Tool CreateLightFixture()
        {
            return new Tool
            {
                Name = "create_light_fixture",
                Description =
                    "Places one or more light-fixture instances on the specified level. " +
                    "Fixtures are arranged in a regular grid starting at (grid_x_mm, grid_y_mm) " +
                    "relative to the room or level origin. " +
                    "Returns the element ids of all newly created instances.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""family_name"": {
                            ""type"": ""string"",
                            ""description"": ""Exact Revit family name of the light fixture to place (e.g. \""Recessed Can Light\"").""
                        },
                        ""level_name"": {
                            ""type"": ""string"",
                            ""description"": ""Name of the level on which the fixtures will be hosted.""
                        },
                        ""grid_x_mm"": {
                            ""type"": ""number"",
                            ""description"": ""X coordinate of the first fixture in millimetres, measured from the project origin.""
                        },
                        ""grid_y_mm"": {
                            ""type"": ""number"",
                            ""description"": ""Y coordinate of the first fixture in millimetres, measured from the project origin.""
                        },
                        ""count"": {
                            ""type"": ""integer"",
                            ""description"": ""Number of fixture instances to place. Defaults to 1."",
                            ""default"": 1
                        },
                        ""room_id"": {
                            ""type"": ""integer"",
                            ""description"": ""Element id of the room to constrain placement within. Optional.""
                        }
                    },
                    ""required"": [""family_name"", ""level_name"", ""grid_x_mm"", ""grid_y_mm""]
                }"),
            };
        }

        private static Tool SetParameter()
        {
            return new Tool
            {
                Name = "set_parameter",
                Description =
                    "Sets a named parameter to a new value on one or more Revit elements. " +
                    "Works with both built-in parameters and shared parameters. " +
                    "For numeric parameters supply a number; for text parameters supply a string. " +
                    "Returns a per-element success/failure report.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""element_ids"": {
                            ""type"": ""array"",
                            ""items"": { ""type"": ""integer"" },
                            ""description"": ""List of Revit element ids whose parameter will be modified.""
                        },
                        ""parameter_name"": {
                            ""type"": ""string"",
                            ""description"": ""Exact name of the parameter to set (e.g. \""Comments\"", \""Voltage\"").""
                        },
                        ""value"": {
                            ""description"": ""New value for the parameter. Use a number for numeric parameters, a string for text parameters."",
                            ""oneOf"": [
                                { ""type"": ""string"" },
                                { ""type"": ""number"" }
                            ]
                        }
                    },
                    ""required"": [""element_ids"", ""parameter_name"", ""value""]
                }"),
            };
        }

        private static Tool ConnectToCircuit()
        {
            return new Tool
            {
                Name = "connect_to_circuit",
                Description =
                    "Adds one or more electrical elements to an existing circuit on a panel. " +
                    "The circuit must already exist in the specified panel. " +
                    "Returns the updated circuit load (VA) after the connection.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""element_ids"": {
                            ""type"": ""array"",
                            ""items"": { ""type"": ""integer"" },
                            ""description"": ""List of Revit element ids to connect to the circuit.""
                        },
                        ""panel_name"": {
                            ""type"": ""string"",
                            ""description"": ""Name of the electrical panel that owns the circuit (e.g. \""LP-1\"").""
                        },
                        ""circuit_number"": {
                            ""type"": ""integer"",
                            ""description"": ""Circuit number within the panel (e.g. 3 for circuit 3).""
                        }
                    },
                    ""required"": [""element_ids"", ""panel_name"", ""circuit_number""]
                }"),
            };
        }

        private static Tool GetCircuitInfo()
        {
            return new Tool
            {
                Name = "get_circuit_info",
                Description =
                    "Returns detailed information about a specific electrical circuit: " +
                    "panel name, circuit number, phase configuration, voltage, " +
                    "current load (VA and amps), rating (amps), and the element ids of all " +
                    "connected devices.",
                InputSchema = JObject.Parse(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""panel_name"": {
                            ""type"": ""string"",
                            ""description"": ""Name of the electrical panel (e.g. \""LP-1\"").""
                        },
                        ""circuit_number"": {
                            ""type"": ""integer"",
                            ""description"": ""Circuit number within the panel.""
                        }
                    },
                    ""required"": [""panel_name"", ""circuit_number""]
                }"),
            };
        }
    }
}
