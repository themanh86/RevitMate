using System;
using Autodesk.Revit.UI;

namespace RevitMate.Addin.UI
{
    /// <summary>
    /// Stable identifiers for RevitMate's dockable panes. Revit persists
    /// docking layout per GUID, so these values must never change between releases.
    /// </summary>
    public static class PaneIds
    {
        public static readonly Guid MainPaneGuid =
            new Guid("9F4B1C8E-2A6D-4E93-B7F1-5C3A8D6E2F04");

        public static DockablePaneId MainPaneDockablePaneId =>
            new DockablePaneId(MainPaneGuid);
    }
}
