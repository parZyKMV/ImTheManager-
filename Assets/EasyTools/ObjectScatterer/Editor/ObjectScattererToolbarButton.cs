using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Toolbars;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Scene View toolbar button (Unity 6 Overlays / ToolbarOverlay). Activates the scatter brush EditorTool.
    /// </summary>
    [EditorToolbarElement(Id, typeof(SceneView))]
    public sealed class ObjectScattererToolbarButton : EditorToolbarButton
    {
        public const string Id = "EasyTools/ObjectScatterer/ToolbarButton";

        public ObjectScattererToolbarButton()
        {
            text = ObjectScattererEditorIcons.ToolbarOsLabel;
            tooltip = "Object Scatterer — activate the Scene brush tool and open the settings window.";
            icon = null;

            clicked += OnClicked;
        }

        private static void OnClicked()
        {
            ToolManager.SetActiveTool<ObjectScattererEditorTool>();
            ObjectScattererWindow.GetOrCreateInstance();
        }
    }
}
