using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    /// <summary>
    /// Optional capability for command handlers that mutate the model and can
    /// describe — read-only — what an execution <em>would</em> do, so the user
    /// can confirm before any change is committed. Read-only query handlers do
    /// not implement this interface.
    /// </summary>
    public interface IPreviewable
    {
        /// <summary>
        /// Produces a short, human-readable summary of the pending change without
        /// modifying the document. May append validation warnings (e.g. a missing
        /// family or level) so the user can cancel before committing.
        /// </summary>
        string Preview(Document doc, JObject input);
    }
}
