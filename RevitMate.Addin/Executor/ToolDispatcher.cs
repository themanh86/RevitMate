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
    }
}
