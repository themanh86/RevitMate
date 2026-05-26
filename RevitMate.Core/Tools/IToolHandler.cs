namespace RevitMate.Core.Tools
{
    /// <summary>
    /// Marker interface for a tool that can be invoked by Claude through the executor layer.
    /// </summary>
    public interface IToolHandler
    {
        string Name { get; }
    }
}
