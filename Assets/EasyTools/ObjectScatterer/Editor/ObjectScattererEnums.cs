using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Which settings group is shown in the editor window (Scatter batch vs Brush painting).
    /// </summary>
    public enum ObjectScattererUiMode
    {
        [InspectorName("Batch Scatter")]
        Scatter,
        [InspectorName("Brush Paint")]
        Brush
    }

    /// <summary>
    /// Brush input: continuous stroke with spacing vs single stamp per click.
    /// </summary>
    public enum BrushStrokeMode
    {
        [InspectorName("Stroke")]
        Stroke,
        [InspectorName("Click")]
        Click
    }

    /// <summary>
    /// World-space scatter region for batch (Grounded) placement.
    /// </summary>
    public enum ScatterVolumeShape
    {
        [InspectorName("Sphere")]
        Sphere,
        [InspectorName("Box")]
        Box
    }

    /// <summary>
    /// Apply random euler in world space or align up to surface normal first.
    /// </summary>
    public enum RotationAlignment
    {
        [InspectorName("World")]
        World,
        [InspectorName("Align to Normal")]
        Normal
    }
}
