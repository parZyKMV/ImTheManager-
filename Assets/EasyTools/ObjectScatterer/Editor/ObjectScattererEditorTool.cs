using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Scene View toolbar tool: paint scattered prefabs like a brush when Grounded Raycast mode is active.
    /// </summary>
    [EditorTool("Object Scatterer")]
    public sealed class ObjectScattererEditorTool : EditorTool
    {
        private static ObjectScattererEditorTool _activeInstance;

        private static GUIContent _toolbarIcon;

        public override GUIContent toolbarIcon
        {
            get
            {
                if (_toolbarIcon == null)
                {
                    _toolbarIcon = new GUIContent(
                        ObjectScattererEditorIcons.ToolbarOsLabel,
                        "Object Scatterer — paint prefabs on surfaces (Brush Paint mode, or Batch Scatter with Ground Raycast).");
                }

                return _toolbarIcon;
            }
        }

        public static bool IsActiveTool()
        {
            return _activeInstance != null;
        }

        public override void OnActivated()
        {
            _activeInstance = this;
        }

        public override void OnWillBeDeactivated()
        {
            if (_activeInstance == this)
                _activeInstance = null;
            ObjectScattererWindow.OnScatterEditorToolDeactivated();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView sceneView))
                return;

            var win = ObjectScattererWindow.GetOrCreateInstance();
            win.PerformSceneBrushGUI(sceneView, fromSceneTool: true);
        }
    }
}
