using UnityEditor;
using UnityEditor.Overlays;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Registers the Scatter button on the Scene View toolbar (enable under Scene view Overlays if hidden).
    /// </summary>
    [Overlay(typeof(SceneView), "EasyTools/ObjectScatterer/ToolbarOverlay", "Object Scatterer")]
    public sealed class ObjectScattererToolbarOverlay : ToolbarOverlay
    {
        public ObjectScattererToolbarOverlay()
            : base(ObjectScattererToolbarButton.Id, ObjectScattererToolbarBrushSettings.Id)
        {
        }
    }
}
