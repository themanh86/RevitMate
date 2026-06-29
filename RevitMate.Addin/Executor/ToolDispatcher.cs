using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor
{
    /// <summary>
    /// Thin facade over <see cref="RevitCommandDispatcher"/> used by the UI
    /// and ViewModel layers to execute Claude tool_use blocks without taking
    /// a direct dependency on the dispatcher singleton.
    /// </summary>
    public static class ToolDispatcher
    {
        public static Task<string> ExecuteAsync(string toolName, JObject input)
            => RevitCommandDispatcher.Instance.ExecuteAsync(toolName, input);

        public static Task<string> PreviewAsync(string toolName, JObject input)
            => RevitCommandDispatcher.Instance.PreviewAsync(toolName, input);

        /// <summary>True when the tool modifies the model and therefore needs confirmation.</summary>
        public static bool IsMutating(string toolName)
            => RevitExternalEventHandler.IsMutating(toolName);

        /// <summary>Opens an undo group so an approved plan undoes as a single step.</summary>
        public static Task<string> BeginPlanAsync(string groupName)
            => RevitCommandDispatcher.Instance.BeginPlanAsync(groupName);

        /// <summary>Assimilates the open undo group at the end of an approved plan.</summary>
        public static Task<string> EndPlanAsync()
            => RevitCommandDispatcher.Instance.EndPlanAsync();
    }
}
