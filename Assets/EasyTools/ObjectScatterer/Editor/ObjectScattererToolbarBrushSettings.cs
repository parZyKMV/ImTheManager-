using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Brush and physics options bound to ObjectScattererWindow (SerializedProperty paths).
    /// </summary>
    [EditorToolbarElement(Id, typeof(SceneView))]
    public sealed class ObjectScattererToolbarBrushSettings : VisualElement
    {
        public const string Id = "EasyTools/ObjectScatterer/BrushSettings";

        private SerializedObject _so;
        private FloatField _radius;
        private FloatField _spacing;
        private EnumField _strokeMode;
        private Toggle _sim;
        private Toggle _play;
        private IntegerField _stepsPerFrame;
        private FloatField _maxT;

        public ObjectScattererToolbarBrushSettings()
        {
            style.flexDirection = FlexDirection.Row;
            style.flexWrap = Wrap.Wrap;
            style.alignItems = Align.Center;
            style.flexShrink = 0;
            style.flexGrow = 0;
            // Toolbar row is narrow; do not cap width or FloatField inputs collapse to 0 width.
            style.maxWidth = StyleKeyword.None;
            style.overflow = Overflow.Visible;

            RegisterCallback<AttachToPanelEvent>(_ => RebuildBindings());
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseBrushPropertyBindings());
        }

        private void RebuildBindings()
        {
            ReleaseBrushPropertyBindings();
            Clear();

            var win = ObjectScattererWindow.GetOrCreateInstance();
            _so = new SerializedObject(win);
            _so.Update();

            _radius = CreateToolbarFloatField("R", "Brush radius (world units).");
            _spacing = CreateToolbarFloatField("S", "Minimum distance between stamps along a stroke (Stroke mode).");
            _strokeMode = new EnumField(BrushStrokeMode.Stroke)
            {
                tooltip = "Stroke: drag with spacing. Click: one stamp per mouse down.",
                style = { width = 120 }
            };
            _sim = new Toggle("Drop sim") { tooltip = "Simulate rigidbody drop after spawn." };
            _play = new Toggle("Preview") { tooltip = "Advance physics over multiple editor frames in the Scene view." };
            _stepsPerFrame = CreateToolbarIntegerField("Steps", "Physics.Simulate steps per editor update when Preview is on.");
            _maxT = CreateToolbarFloatField("MaxT", "Upper bound on simulate time (seconds).");

            _radius.BindProperty(_so.FindProperty("brushRadius"));
            _spacing.BindProperty(_so.FindProperty("brushSpacing"));
            _strokeMode.BindProperty(_so.FindProperty("brushStrokeMode"));
            _sim.BindProperty(_so.FindProperty("simulateDropOnSpawn"));
            _play.BindProperty(_so.FindProperty("physicsVisualPlayback"));
            _stepsPerFrame.BindProperty(_so.FindProperty("physicsStepsPerEditorFrame"));
            _maxT.BindProperty(_so.FindProperty("maxDropSimulateSeconds"));

            _spacing.style.marginLeft = 4;
            _strokeMode.style.marginLeft = 4;
            _sim.style.marginLeft = 4;
            _play.style.marginLeft = 2;
            _stepsPerFrame.style.marginLeft = 4;
            _maxT.style.marginLeft = 4;

            Add(_radius);
            Add(_spacing);
            Add(_strokeMode);
            Add(_sim);
            Add(_play);
            Add(_stepsPerFrame);
            Add(_maxT);
        }

        private void ReleaseBrushPropertyBindings()
        {
            _radius?.Unbind();
            _spacing?.Unbind();
            _strokeMode?.Unbind();
            _sim?.Unbind();
            _play?.Unbind();
            _stepsPerFrame?.Unbind();
            _maxT?.Unbind();
            _so = null;
            _radius = null;
            _spacing = null;
            _strokeMode = null;
            _sim = null;
            _play = null;
            _stepsPerFrame = null;
            _maxT = null;
            Clear();
        }

        /// <summary>
        /// Scene overlay toolbar rows are very tight; long labels steal flex space and the numeric input can end up with 0 width.
        /// </summary>
        private static FloatField CreateToolbarFloatField(string shortLabel, string tooltipText)
        {
            var f = new FloatField(shortLabel)
            {
                tooltip = tooltipText
            };
            f.style.flexShrink = 0;
            f.style.minWidth = 108;
            f.labelElement.style.flexShrink = 0;
            f.labelElement.style.minWidth = 22;
            f.labelElement.style.maxWidth = 36;
            return f;
        }

        private static IntegerField CreateToolbarIntegerField(string shortLabel, string tooltipText)
        {
            var f = new IntegerField(shortLabel)
            {
                tooltip = tooltipText
            };
            f.style.flexShrink = 0;
            f.style.minWidth = 118;
            f.labelElement.style.flexShrink = 0;
            f.labelElement.style.minWidth = 36;
            f.labelElement.style.maxWidth = 52;
            return f;
        }
    }
}
