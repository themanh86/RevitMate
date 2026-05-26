using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMate.Addin.Executor.Commands;

namespace RevitMate.Addin.Executor
{
    public sealed class ToolCall
    {
        public string Name { get; set; }
        public JObject Input { get; set; }
    }

    public sealed class RevitExternalEventHandler : IExternalEventHandler
    {
        private static readonly IReadOnlyDictionary<string, ICommandHandler> Handlers =
            new Dictionary<string, ICommandHandler>(StringComparer.Ordinal)
            {
                ["get_selected_elements"] = new GetSelectedElementsCommand(),
                ["get_active_view_info"]  = new GetActiveViewInfoCommand(),
                ["get_room_info"]         = new GetRoomInfoCommand(),
                ["create_light_fixture"]  = new CreateLightFixtureCommand(),
                ["set_parameter"]         = new SetParameterCommand(),
                ["connect_to_circuit"]    = new ConnectToCircuitCommand(),
                ["get_circuit_info"]      = new GetCircuitInfoCommand(),
            };

        public ToolCall PendingCall { get; set; }
        public TaskCompletionSource<string> CompletionSource { get; set; }

        public void Execute(UIApplication app)
        {
            Application.SetRevitApp(app);

            string name = PendingCall?.Name ?? string.Empty;
            JObject input = PendingCall?.Input ?? new JObject();
            TaskCompletionSource<string> tcs = CompletionSource;

            try
            {
                Document doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    tcs?.SetResult(JsonError("No active Revit document."));
                    return;
                }

                if (!Handlers.TryGetValue(name, out ICommandHandler handler))
                {
                    tcs?.SetResult(JsonError($"Unknown tool '{name}'."));
                    return;
                }

                string result;
                if (handler.IsReadOnly)
                {
                    result = handler.Execute(doc, input);
                }
                else
                {
                    using (var t = new Transaction(doc, "RevitMate: " + name))
                    {
                        t.Start();
                        try
                        {
                            result = handler.Execute(doc, input);
                            if (IsErrorResult(result))
                                t.RollBack();
                            else
                                t.Commit();
                        }
                        catch
                        {
                            if (t.GetStatus() == TransactionStatus.Started)
                                t.RollBack();
                            throw;
                        }
                    }
                }

                tcs?.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs?.SetResult(JsonError(ex.Message));
            }
        }

        public string GetName() => "RevitMate Executor";

        internal static string JsonError(string message)
            => new JObject { ["error"] = message }.ToString(Newtonsoft.Json.Formatting.None);

        private static bool IsErrorResult(string json)
        {
            try { return JObject.Parse(json)["error"] != null; }
            catch { return false; }
        }
    }
}
