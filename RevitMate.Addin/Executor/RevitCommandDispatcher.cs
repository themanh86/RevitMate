using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor
{
    /// <summary>
    /// Singleton that marshals Claude tool_use calls onto the Revit UI thread
    /// via <see cref="ExternalEvent"/>. Must be initialized from
    /// <c>Application.OnStartup</c> (the Revit UI thread) by calling
    /// <see cref="Initialize"/> before the first <see cref="ExecuteAsync"/> call.
    /// </summary>
    public sealed class RevitCommandDispatcher
    {
        private static RevitCommandDispatcher _instance;

        private readonly RevitExternalEventHandler _handler;
        private readonly ExternalEvent _externalEvent;

        // Serializes concurrent async callers so PendingCall is never trampled.
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private RevitCommandDispatcher()
        {
            _handler = new RevitExternalEventHandler();
            _externalEvent = ExternalEvent.Create(_handler);
        }

        /// <summary>
        /// Creates the singleton. Must be called once from the Revit UI thread
        /// (e.g. <c>Application.OnStartup</c>) because <see cref="ExternalEvent.Create"/>
        /// requires the add-in context.
        /// </summary>
        public static void Initialize()
        {
            if (_instance == null)
                _instance = new RevitCommandDispatcher();
        }

        public static RevitCommandDispatcher Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "RevitCommandDispatcher has not been initialized. " +
                        "Call RevitCommandDispatcher.Initialize() from Application.OnStartup.");
                return _instance;
            }
        }

        /// <summary>
        /// Schedules <paramref name="toolName"/> for execution on the Revit UI thread
        /// and asynchronously returns the JSON result string. Mutating tools run
        /// inside a named <see cref="Transaction"/> for single-step undo.
        /// </summary>
        public Task<string> ExecuteAsync(string toolName, JObject input)
            => RunAsync(toolName, input, ToolCallMode.Execute);

        /// <summary>
        /// Schedules a read-only preview of <paramref name="toolName"/> on the Revit
        /// UI thread and asynchronously returns a human-readable description of the
        /// change it would make, without modifying the model.
        /// </summary>
        public Task<string> PreviewAsync(string toolName, JObject input)
            => RunAsync(toolName, input, ToolCallMode.Preview);

        /// <summary>
        /// Opens an undo group on the Revit UI thread. Mutating tools executed
        /// afterwards nest inside it until <see cref="EndPlanAsync"/> assimilates
        /// them into a single undo step. <paramref name="groupName"/> labels the
        /// resulting entry in the undo stack.
        /// </summary>
        public Task<string> BeginPlanAsync(string groupName)
            => RunAsync(groupName, null, ToolCallMode.BeginGroup);

        /// <summary>
        /// Assimilates the open undo group so the whole plan is a single Ctrl+Z.
        /// Safe to call when no group is open.
        /// </summary>
        public Task<string> EndPlanAsync()
            => RunAsync(string.Empty, null, ToolCallMode.CommitGroup);

        private async Task<string> RunAsync(string toolName, JObject input, ToolCallMode mode)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

                _handler.PendingCall = new ToolCall { Name = toolName, Input = input ?? new JObject(), Mode = mode };
                _handler.CompletionSource = tcs;

                ExternalEventRequest status = _externalEvent.Raise();
                if (status != ExternalEventRequest.Accepted)
                {
                    return RevitExternalEventHandler.JsonError(
                        $"Revit external event was not accepted (status: {status}). " +
                        "Ensure Revit is in a command-ready idle state.");
                }

                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
