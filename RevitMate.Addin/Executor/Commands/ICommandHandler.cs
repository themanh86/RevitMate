using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.Executor.Commands
{
    public interface ICommandHandler
    {
        bool IsReadOnly { get; }
        string Execute(Document doc, JObject input);
    }
}
