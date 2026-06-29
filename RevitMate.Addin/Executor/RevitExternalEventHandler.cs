using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMate.Addin.Audit;
using RevitMate.Addin.Executor.Commands;

namespace RevitMate.Addin.Executor
{
    public enum ToolCallMode
    {
        Execute,
        Preview,
        BeginGroup,
        CommitGroup
    }

    public sealed class ToolCall
    {
        public string Name { get; set; }
        public JObject Input { get; set; }
        public ToolCallMode Mode { get; set; } = ToolCallMode.Execute;
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

        // Open TransactionGroup spanning one approved plan so all of its mutations
        // collapse into a single undo step. Lives across multiple external events
        // (one per tool) and is assimilated when the plan finishes.
        private TransactionGroup _activeGroup;

        public void Execute(UIApplication app)
        {
            Application.SetRevitApp(app);

            string name = PendingCall?.Name ?? string.Empty;
            JObject input = PendingCall?.Input ?? new JObject();
            ToolCallMode mode = PendingCall?.Mode ?? ToolCallMode.Execute;
            TaskCompletionSource<string> tcs = CompletionSource;

            try
            {
                Document doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    tcs?.SetResult(JsonError("No active Revit document."));
                    return;
                }

                if (mode == ToolCallMode.BeginGroup)
                {
                    BeginGroup(doc, name);
                    tcs?.SetResult(OkResult);
                    return;
                }

                if (mode == ToolCallMode.CommitGroup)
                {
                    EndGroup(commit: true);
                    tcs?.SetResult(OkResult);
                    return;
                }

                if (!Handlers.TryGetValue(name, out ICommandHandler handler))
                {
                    tcs?.SetResult(JsonError($"Unknown tool '{name}'."));
                    return;
                }

                // Preview mode: describe the pending change read-only, never mutating.
                if (mode == ToolCallMode.Preview)
                {
                    string preview = handler is IPreviewable previewable
                        ? previewable.Preview(doc, input)
                        : null;
                    tcs?.SetResult(string.IsNullOrEmpty(preview) ? name : preview);
                    return;
                }

                string result;
                if (handler.IsReadOnly)
                {
                    result = handler.Execute(doc, input);
                }
                else
                {
                    string status;
                    using (var t = new Transaction(doc, "RevitMate: " + name))
                    {
                        t.Start();
                        try
                        {
                            result = handler.Execute(doc, input);
                            if (IsErrorResult(result))
                            {
                                t.RollBack();
                                status = "rolled_back";
                            }
                            else
                            {
                                t.Commit();
                                status = "committed";
                            }
                        }
                        catch
                        {
                            if (t.GetStatus() == TransactionStatus.Started)
                                t.RollBack();
                            throw;
                        }
                    }

                    AuditLogger.RecordMutation(doc.Title, name, input, result, status);
                }

                tcs?.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs?.SetResult(JsonError(ex.Message));
            }
        }

        public string GetName() => "RevitMate Executor";

        /// <summary>
        /// True when the named tool maps to a handler that modifies the model
        /// (i.e. is not read-only). Unknown tools are treated as non-mutating.
        /// </summary>
        public static bool IsMutating(string toolName)
            => Handlers.TryGetValue(toolName, out ICommandHandler handler) && !handler.IsReadOnly;

        internal static string JsonError(string message)
            => new JObject { ["error"] = message }.ToString(Newtonsoft.Json.Formatting.None);

        private const string OkResult = "{\"ok\":true}";

        // Opens a new undo group, discarding any group left over from an aborted
        // plan so a stale group never swallows unrelated edits.
        private void BeginGroup(Document doc, string name)
        {
            DiscardActiveGroup();
            string label = string.IsNullOrWhiteSpace(name) ? "RevitMate AI plan" : name;
            _activeGroup = new TransactionGroup(doc, label);
            _activeGroup.Start();
        }

        // Closes the active group: assimilate merges its transactions into a single
        // undo step; rollback discards them. Always disposes the group.
        private void EndGroup(bool commit)
        {
            if (_activeGroup == null)
                return;

            try
            {
                if (_activeGroup.GetStatus() == TransactionStatus.Started)
                {
                    if (commit)
                        _activeGroup.Assimilate();
                    else
                        _activeGroup.RollBack();
                }
            }
            finally
            {
                _activeGroup.Dispose();
                _activeGroup = null;
            }
        }

        private void DiscardActiveGroup() => EndGroup(commit: false);

        private static bool IsErrorResult(string json)
        {
            try { return JObject.Parse(json)["error"] != null; }
            catch { return false; }
        }
    }
}
