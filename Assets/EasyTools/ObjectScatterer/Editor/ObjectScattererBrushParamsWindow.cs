using UnityEditor;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Floating input window for brush / physics toolbar parameters (opened from the Scene overlay).
    /// </summary>
    internal sealed class ObjectScattererBrushParamsWindow : EditorWindow
    {
        private SerializedObject _so;

        public static void OpenOrFocus()
        {
            var w = GetWindow<ObjectScattererBrushParamsWindow>(true, "Object Scatterer — Brush", true);
            w.minSize = new Vector2(300, 260);
            w.ShowUtility();
        }

        private void OnEnable()
        {
            RefreshSerializedObject();
        }

        private void RefreshSerializedObject()
        {
            var scatter = ObjectScattererWindow.GetOrCreateInstance();
            if (scatter != null)
                _so = new SerializedObject(scatter);
        }

        private void OnGUI()
        {
            if (_so == null || _so.targetObject == null)
                RefreshSerializedObject();
            if (_so == null || _so.targetObject == null)
            {
                EditorGUILayout.HelpBox("Object Scatterer window data is unavailable.", MessageType.Warning);
                return;
            }

            _so.Update();
            EditorGUILayout.LabelField("Brush & drop simulation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty("brushRadius"));
            EditorGUILayout.PropertyField(_so.FindProperty("brushSpacing"));
            EditorGUILayout.PropertyField(_so.FindProperty("brushStrokeMode"));
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_so.FindProperty("simulateDropOnSpawn"));
            EditorGUILayout.PropertyField(_so.FindProperty("physicsVisualPlayback"));
            EditorGUILayout.PropertyField(_so.FindProperty("physicsStepsPerEditorFrame"));
            EditorGUILayout.PropertyField(_so.FindProperty("maxDropSimulateSeconds"));
            _so.ApplyModifiedProperties();
        }
    }
}
