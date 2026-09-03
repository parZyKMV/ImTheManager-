using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// How random euler angles are applied: yaw-only or full XYZ ranges.
    /// </summary>
    public enum RotationRandomMode
    {
        [InspectorName("Yaw Only")]
        YawOnly,
        [InspectorName("Full XYZ")]
        FullXYZ
    }

    /// <summary>
    /// Grounded placement uses raycasts; volume mode spawns inside a box then runs rigidbody simulation.
    /// </summary>
    public enum SpawnPlacementMode
    {
        [InspectorName("Ground Raycast")]
        GroundedRaycast,
        [InspectorName("Random Volume + Drop")]
        RandomVolumeThenDrop
    }

    /// <summary>
    /// Editor window for scattering prefab or scene object instances. Placement uses physics raycasts,
    /// terrain, and mesh-triangle fallback when no collider is present.
    /// </summary>
    public sealed class ObjectScattererWindow : EditorWindow
    {
        private const string MenuPath = "Window/EasyTools/Object Scatterer";
        private const float RaycastMaxDistance = 5000f;

        [SerializeField]
        [FormerlySerializedAs("prefabs")]
        [Tooltip("Project prefabs or scene/hierarchy GameObjects. Each spawn picks one at random. Scene references are duplicated with Instantiate.")]
        private List<GameObject> scatterSources = new List<GameObject>();

        [SerializeField]
        [Tooltip("Center of the scatter region in world space. If empty, uses world origin (0,0,0).")]
        private Transform scatterCenter;

        [SerializeField]
        [Tooltip("Ground Raycast: snap to colliders. Random Volume + Drop: spawn inside a box then run physics.")]
        private SpawnPlacementMode spawnPlacementMode = SpawnPlacementMode.GroundedRaycast;

        [SerializeField]
        [Tooltip("Batch Scatter: place many instances at once. Brush Paint: paint in the Scene view with the Object Scatterer tool.")]
        private ObjectScattererUiMode editorUiMode = ObjectScattererUiMode.Scatter;

        [SerializeField]
        [Tooltip("Radius on XZ around Scatter Center (Ground Raycast + sphere region). World units.")]
        private float scatterRadius = 10f;

        /// <summary>
        /// Half size of the axis-aligned spawn box (world space, centered at Scatter Center or origin).
        /// </summary>
        [SerializeField]
        [Tooltip("Half-extents of the axis-aligned drop volume (world units). Center = Scatter Center or world origin.")]
        private Vector3 randomVolumeHalfExtents = new Vector3(5f, 3f, 5f);

        [SerializeField]
        [Tooltip("How many instances to create in one Batch Scatter click.")]
        private int objectCount = 50;

        [SerializeField]
        [Tooltip("Parent for new instances. If empty, a new \"Scattered Objects\" root is created.")]
        private Transform parentContainer;

        [SerializeField]
        [Tooltip("Minimum random factor multiplied onto the source root local scale (per axis, uniform).")]
        private float minScale = 0.8f;

        [SerializeField]
        [Tooltip("Maximum random factor multiplied onto the source root local scale (per axis, uniform).")]
        private float maxScale = 1.2f;

        [SerializeField]
        [Tooltip("Apply random rotation from the ranges below.")]
        private bool applyRandomRotation = true;

        [SerializeField]
        [Tooltip("Yaw Only: random Y. Full XYZ: independent ranges per axis.")]
        private RotationRandomMode rotationRandomMode = RotationRandomMode.YawOnly;

        [SerializeField]
        [Tooltip("Minimum euler angles in degrees (per-axis when Full XYZ; Yaw Only uses Y).")]
        private Vector3 rotationMinDeg = new Vector3(0f, 0f, 0f);

        [SerializeField]
        [Tooltip("Maximum euler angles in degrees (per-axis when Full XYZ; Yaw Only uses Y).")]
        private Vector3 rotationMaxDeg = new Vector3(0f, 360f, 0f);

        [SerializeField]
        [Tooltip("Add a random offset after placement. On surfaces, offset is projected onto the tangent plane and re-raycast.")]
        private bool usePositionJitter;

        [SerializeField]
        [Tooltip("Minimum jitter offset in world units (per axis).")]
        private Vector3 positionJitterMin = Vector3.zero;

        [SerializeField]
        [Tooltip("Maximum jitter offset in world units (per axis).")]
        private Vector3 positionJitterMax = Vector3.zero;

        [SerializeField]
        [Tooltip("Layers considered by placement raycasts (Ground Raycast and Brush).")]
        private LayerMask raycastLayers = ~0;

        [SerializeField]
        [Tooltip("Draw scatter region and jitter bounds in the Scene view.")]
        private bool showRangeGizmos = true;

        [SerializeField]
        [Tooltip("Brush stamp radius on the surface tangent plane (world units).")]
        private float brushRadius = 3f;

        [SerializeField]
        [Tooltip("Minimum distance between stamps along a stroke (Stroke mode). World units.")]
        private float brushSpacing = 1.5f;

        [SerializeField]
        [Tooltip("Stroke: drag to paint with spacing. Click: one stamp per mouse down.")]
        private BrushStrokeMode brushStrokeMode = BrushStrokeMode.Stroke;

        [SerializeField]
        [Tooltip("After spawn, run rigidbody simulation so props settle (Ground Raycast / Brush).")]
        private bool simulateDropOnSpawn;

        /// <summary>
        /// When true, Physics.Simulate runs across editor frames (visible motion). When false, solved in one frame.
        /// </summary>
        [SerializeField]
        [Tooltip("When enabled, physics advances over multiple editor frames so you see motion in the Scene view.")]
        private bool physicsVisualPlayback = true;

        /// <summary>
        /// How many fixed steps to run per EditorApplication.update when physicsVisualPlayback is on.
        /// </summary>
        [SerializeField]
        [Tooltip("Physics.Simulate steps per editor update when preview playback is enabled.")]
        private int physicsStepsPerEditorFrame = 2;

        [SerializeField]
        [Tooltip("Upper bound on drop simulation time in seconds (per instance or batch step).")]
        private float maxDropSimulateSeconds = 2f;

        /// <summary>
        /// When brush painting with physics drop, offset spawn position along the hit normal before simulation (world units).
        /// </summary>
        [SerializeField]
        [Tooltip("Offset along the surface normal before drop (world units). Only when Brush + Simulate Drop.")]
        private float brushPhysicsSpawnHeight = 10f;

        /// <summary>
        /// Grounded scatter: when true, sample surfaces within the volume (raycast); when false, random positions inside the volume only.
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("attachToSurface")]
        [Tooltip("When enabled: random point in region + ray to snap to a surface. When disabled: random points in the volume only (no surface raycast).")]
        private bool surfaceFitMode = true;

        [SerializeField]
        [Tooltip("Sphere: scatter within a horizontal disc (radius). Box: scatter within an axis-aligned box (half-extents).")]
        private ScatterVolumeShape scatterVolumeShape = ScatterVolumeShape.Sphere;

        /// <summary>
        /// Half extents of the Grounded scatter box (world space, centered at Scatter Center or origin).
        /// </summary>
        [SerializeField]
        [Tooltip("Half-extents of the Ground Raycast region (world units). Center = Scatter Center or world origin.")]
        private Vector3 scatterBoxHalfExtents = new Vector3(5f, 5f, 5f);

        /// <summary>
        /// When non-empty, only hits on these roots (and their children) are used. Empty = no include restriction.
        /// </summary>
        [SerializeField]
        [Tooltip("Optional: only hits under these roots (includes children). Empty = no include filter (after Layer).")]
        private List<GameObject> scatterIncludeGameObjects = new List<GameObject>();

        /// <summary>
        /// Hits on these roots (and their children) are ignored after layer checks.
        /// </summary>
        [SerializeField]
        [Tooltip("Hits on these roots and their children are ignored after Layer checks.")]
        private List<GameObject> scatterExcludeGameObjects = new List<GameObject>();

        [SerializeField]
        [Tooltip("World: random euler in world space. Align to Normal: apply random yaw/XYZ after aligning up to the surface.")]
        private RotationAlignment rotationAlignment = RotationAlignment.Normal;

        private SerializedObject _serializedObject;
        private SerializedProperty _scatterSourcesProperty;
        private Vector2 _scroll;

        private static bool _modeHelpExpanded;

        private Vector3 _lastSceneHitPoint;
        private Vector3 _lastSceneHitNormal = Vector3.up;
        private bool _hasLastSceneHit;
        private Vector3 _lastBrushStampPosition;
        private bool _hasLastBrushStamp;
        private bool _isBrushing;
        private int _brushUndoGroup;
        private Transform _brushStrokeParent;

        private static readonly int BrushSceneGuiControlHash = "EasyTools.ObjectScatterer.Brush".GetHashCode();

        /// <summary>
        /// Live value for editor physics playback (read each <see cref="EditorApplication.update"/> tick).
        /// </summary>
        internal int GetPhysicsStepsPerEditorFrame() => Mathf.Max(1, physicsStepsPerEditorFrame);

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<ObjectScattererWindow>("Object Scatterer");
            window.minSize = new Vector2(400f, 560f);
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _scatterSourcesProperty = _serializedObject.FindProperty(nameof(scatterSources));
            SceneView.duringSceneGui += OnDuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnDuringSceneGui;
        }

        private void OnDuringSceneGui(SceneView sceneView)
        {
            // Brush painting runs only while the Object Scatterer Scene tool is active (EditorTool.OnToolGUI).
            if (ObjectScattererEditorTool.IsActiveTool())
                return;

            if (showRangeGizmos)
                DrawRangeGizmos();
        }

        /// <summary>
        /// Called when switching to Move/Rotate/etc. so an in-progress LMB stroke does not leave bad undo state.
        /// </summary>
        internal static void OnScatterEditorToolDeactivated()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<ObjectScattererWindow>())
            {
                if (w != null)
                    w.AbortBrushStrokeForToolSwitch();
            }
        }

        private void AbortBrushStrokeForToolSwitch()
        {
            if (!_isBrushing)
                return;

            EndBrushStrokeUndo();
            _isBrushing = false;
            _hasLastBrushStamp = false;
            _brushStrokeParent = null;
        }

        /// <summary>
        /// Gets an open window or creates/shows one so Scene toolbar tool can read settings and paint.
        /// </summary>
        public static ObjectScattererWindow GetOrCreateInstance()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<ObjectScattererWindow>())
            {
                if (w != null)
                    return w;
            }

            return GetWindow<ObjectScattererWindow>();
        }

        /// <summary>
        /// Draws range gizmos, brush preview, and handles brush input (used by the window or by ObjectScattererEditorTool).
        /// </summary>
        internal void PerformSceneBrushGUI(SceneView sceneView, bool fromSceneTool)
        {
            var allowBrushSceneGui = editorUiMode == ObjectScattererUiMode.Brush ||
                                     spawnPlacementMode == SpawnPlacementMode.GroundedRaycast;
            if (!allowBrushSceneGui)
            {
                if (fromSceneTool)
                    DrawSceneToolPlacementModeHint();
                return;
            }

            RefreshBrushHitFromMouse();
            if (showRangeGizmos)
                DrawRangeGizmos();

            if (ShouldDrawBrushPreview())
                DrawBrushPlacementPreview();

            HandleBrushInput(Event.current, sceneView);
        }

        private bool BrushInteractionActive()
        {
            if (!ObjectScattererEditorTool.IsActiveTool())
                return false;
            if (editorUiMode == ObjectScattererUiMode.Brush)
                return true;
            return spawnPlacementMode == SpawnPlacementMode.GroundedRaycast;
        }

        private void DrawSceneToolPlacementModeHint()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10f, 10f, 420f, 56f));
            EditorGUILayout.HelpBox(
                "Object Scatterer tool: set Spawn Placement Mode to Ground Raycast in the Object Scatterer window.",
                MessageType.Warning);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        /// <summary>
        /// Updates brush preview raycast from the current Scene GUI event so wire/solid discs track the cursor continuously.
        /// </summary>
        private void RefreshBrushHitFromMouse()
        {
            var e = Event.current;
            if (e == null)
                return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            _hasLastSceneHit = ObjectScattererSceneRaycast.TryRaycast(ray, RaycastMaxDistance, raycastLayers,
                ScatterHitTransformPasses, out var hit);
            if (_hasLastSceneHit)
            {
                _lastSceneHitPoint = hit.point;
                _lastSceneHitNormal = hit.normal;
            }
        }

        private void DrawRangeGizmos()
        {
            if (editorUiMode == ObjectScattererUiMode.Brush)
            {
                if (!usePositionJitter)
                    return;
            }

            var center = scatterCenter != null ? scatterCenter.position : Vector3.zero;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            if (editorUiMode == ObjectScattererUiMode.Brush)
            {
                var size = positionJitterMax - positionJitterMin;
                Handles.color = new Color(1f, 0.75f, 0.2f, 0.65f);
                Handles.DrawWireCube(center, new Vector3(
                    Mathf.Max(Mathf.Abs(size.x), 0.01f),
                    Mathf.Max(Mathf.Abs(size.y), 0.01f),
                    Mathf.Max(Mathf.Abs(size.z), 0.01f)));
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                return;
            }

            if (spawnPlacementMode == SpawnPlacementMode.RandomVolumeThenDrop)
            {
                Handles.color = new Color(0.9f, 0.35f, 0.85f, 0.92f);
                Handles.DrawWireCube(center, randomVolumeHalfExtents * 2f);
            }
            else
            {
                Handles.color = new Color(0.2f, 0.85f, 0.35f, 0.9f);
                if (scatterVolumeShape == ScatterVolumeShape.Sphere)
                    DrawWireSphereApprox(center, scatterRadius);
                else
                    Handles.DrawWireCube(center, scatterBoxHalfExtents * 2f);

            }

            if (usePositionJitter)
            {
                var size = positionJitterMax - positionJitterMin;
                Handles.color = new Color(1f, 0.75f, 0.2f, 0.65f);
                Handles.DrawWireCube(center, new Vector3(
                    Mathf.Max(Mathf.Abs(size.x), 0.01f),
                    Mathf.Max(Mathf.Abs(size.y), 0.01f),
                    Mathf.Max(Mathf.Abs(size.z), 0.01f)));
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }

        /// <summary>
        /// Wire sphere without relying on Handles.DrawWireSphere (not available in all Unity versions).
        /// </summary>
        private static void DrawWireSphereApprox(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private Vector3 GetScatterCenter()
        {
            return scatterCenter != null ? scatterCenter.position : Vector3.zero;
        }

        private bool ScatterHitTransformPasses(Transform t)
        {
            return ObjectScattererHitFilter.TransformPasses(t, scatterIncludeGameObjects, scatterExcludeGameObjects);
        }

        /// <summary>
        /// Light blue translucent fill + wire ring for brush radius (updates every frame while the cursor moves).
        /// </summary>
        private void DrawBrushPlacementPreview()
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            var fill = new Color(0.2f, 0.45f, 0.75f, 0.38f);
            var wire = new Color(0.12f, 0.35f, 0.65f, 0.98f);
            Handles.color = fill;
            Handles.DrawSolidDisc(_lastSceneHitPoint, _lastSceneHitNormal, brushRadius);
            Handles.color = wire;
            Handles.DrawWireDisc(_lastSceneHitPoint, _lastSceneHitNormal, brushRadius);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }

        private bool ShouldDrawBrushPreview()
        {
            return BrushInteractionActive() && _hasLastSceneHit;
        }

        private void HandleBrushInput(Event e, SceneView sceneView)
        {
            var controlId = GUIUtility.GetControlID(BrushSceneGuiControlHash, FocusType.Passive);
            switch (e.type)
            {
                case EventType.Layout:
                    // Ensures SceneView keeps delivering mouse-move events while Brush is active (preview tracks the cursor).
                    HandleUtility.AddDefaultControl(controlId);
                    break;

                case EventType.MouseMove:
                    // Keep preview in sync while hovering; repaint so the Scene view updates every frame the cursor moves.
                    if (sceneView != null)
                        sceneView.Repaint();
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        if (brushStrokeMode == BrushStrokeMode.Click)
                        {
                            if (_hasLastSceneHit)
                            {
                                BeginBrushStrokeUndo();
                                TryBrushStamp(_lastSceneHitPoint);
                                EndBrushStrokeUndo();
                            }

                            e.Use();
                            break;
                        }

                        BeginBrushStrokeUndo();
                        _brushStrokeParent = null;
                        _isBrushing = true;
                        _hasLastBrushStamp = false;
                        if (_hasLastSceneHit)
                            TryBrushStamp(_lastSceneHitPoint);
                        SceneView.RepaintAll();
                        e.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (brushStrokeMode == BrushStrokeMode.Stroke && _isBrushing && e.button == 0)
                    {
                        if (_hasLastSceneHit)
                            TryBrushStampAlongStroke(_lastSceneHitPoint);
                        SceneView.RepaintAll();
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (brushStrokeMode == BrushStrokeMode.Stroke && _isBrushing && e.button == 0)
                    {
                        EndBrushStrokeUndo();
                        _isBrushing = false;
                        _hasLastBrushStamp = false;
                        _brushStrokeParent = null;
                        SceneView.RepaintAll();
                        e.Use();
                    }

                    break;
            }
        }

        private void BeginBrushStrokeUndo()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Brush Scatter");
            _brushUndoGroup = Undo.GetCurrentGroup();
        }

        private void EndBrushStrokeUndo()
        {
            Undo.CollapseUndoOperations(_brushUndoGroup);
        }

        private void TryBrushStampAlongStroke(Vector3 hitPoint)
        {
            if (!_hasLastBrushStamp)
            {
                TryBrushStamp(hitPoint);
                return;
            }

            var delta = hitPoint - _lastBrushStampPosition;
            if (delta.sqrMagnitude < brushSpacing * brushSpacing)
                return;

            // Step along the stroke so spacing is respected on fast cursor movement.
            var dir = delta.normalized;
            var dist = delta.magnitude;
            var steps = Mathf.Max(1, Mathf.FloorToInt(dist / brushSpacing));
            var stepLen = dist / steps;
            for (var s = 1; s <= steps; s++)
            {
                var p = _lastBrushStampPosition + dir * (stepLen * s);
                TryBrushStamp(p);
            }
        }

        private void TryBrushStamp(Vector3 surfaceHint)
        {
            if (!ValidatePrefabListForAction(showDialog: false))
                return;

            if (!ValidateScaleAndJitter(showDialog: false))
                return;

            if (brushRadius <= 0f)
                return;

            if (brushStrokeMode == BrushStrokeMode.Stroke && brushSpacing <= 0f)
                return;

            var random = new System.Random();
            var normal = _hasLastSceneHit ? _lastSceneHitNormal : Vector3.up;
            var tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
            var bitangent = Vector3.Cross(normal, tangent).normalized;
            var angle = (float)(random.NextDouble() * System.Math.PI * 2.0);
            var discR = brushRadius * Mathf.Sqrt((float)random.NextDouble());
            var offset = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * discR;
            var samplePoint = surfaceHint + offset;
            const float lift = 0.08f;
            var ray = new Ray(samplePoint + normal * lift, -normal);
            if (!ObjectScattererSceneRaycast.TryRaycast(ray, lift + 2.5f, raycastLayers, ScatterHitTransformPasses,
                    out var hit))
                return;

            var parent = GetOrCreateParentForBrushStroke();
            if (parent == null)
                return;

            var validPrefabs = BuildValidPrefabList();
            var prefab = validPrefabs[random.Next(0, validPrefabs.Count)];

            if (!TrySpawnOneInstance(prefab, parent, hit, random, registerUndo: true, spawnFromBrush: true))
                return;

            _lastBrushStampPosition = hit.point;
            _hasLastBrushStamp = true;
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_scatterSourcesProperty, new GUIContent("Scatter Sources"), true);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(parentContainer)));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(editorUiMode)));

            if (editorUiMode == ObjectScattererUiMode.Scatter)
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterCenter)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(spawnPlacementMode)));

                if (spawnPlacementMode == SpawnPlacementMode.GroundedRaycast)
                {
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterVolumeShape)));
                    if (scatterVolumeShape == ScatterVolumeShape.Sphere)
                        EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterRadius)));
                    else
                        EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterBoxHalfExtents)));

                    EditorGUILayout.PropertyField(
                        _serializedObject.FindProperty(nameof(surfaceFitMode)),
                        new GUIContent(
                            "Snap to surfaces in region",
                            "When enabled: random point in the scatter region plus a random-direction ray to snap to a collider. " +
                            "When disabled: random positions only inside the region (no surface raycast)."));
                    EditorGUILayout.LabelField("Surface filter (roots)", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterIncludeGameObjects)),
                        new GUIContent("Include Only"), true);
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterExcludeGameObjects)),
                        new GUIContent("Exclude"), true);
                    EditorGUILayout.HelpBox(
                        "Include: empty = all roots allowed (after Layer). Otherwise only hits under listed roots (children count). " +
                        "Exclude: listed roots and their children are ignored.",
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(randomVolumeHalfExtents)));
                    EditorGUILayout.HelpBox(
                        "Each instance spawns at a random point inside this axis-aligned box (center = Scatter Center or world origin), then rigidbodies simulate for up to Max Simulate Time.",
                        MessageType.Info);
                }

                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(objectCount)));
            }
            else
            {
                EditorGUILayout.LabelField("Surface filter (roots)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterIncludeGameObjects)),
                    new GUIContent("Include Only"), true);
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(scatterExcludeGameObjects)),
                    new GUIContent("Exclude"), true);
                EditorGUILayout.HelpBox(
                    "Brush hits use the same Layer mask and root filters as Ground Raycast. Include: empty = all (after Layer). Exclude removes listed hierarchies.",
                    MessageType.None);
            }

            var showRaycastLayers = !(editorUiMode == ObjectScattererUiMode.Scatter &&
                                      spawnPlacementMode == SpawnPlacementMode.RandomVolumeThenDrop);
            if (showRaycastLayers)
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(raycastLayers)));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Randomization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(minScale)));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(maxScale)));

            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(applyRandomRotation)));
            if (applyRandomRotation)
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(rotationRandomMode)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(rotationMinDeg)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(rotationMaxDeg)));
            }

            var rotationAlignmentDisabled = spawnPlacementMode == SpawnPlacementMode.GroundedRaycast &&
                                            !surfaceFitMode && editorUiMode != ObjectScattererUiMode.Brush;
            using (new EditorGUI.DisabledScope(rotationAlignmentDisabled))
            {
                if (applyRandomRotation ||
                    (spawnPlacementMode == SpawnPlacementMode.GroundedRaycast && surfaceFitMode) ||
                    editorUiMode == ObjectScattererUiMode.Brush)
                {
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(rotationAlignment)));
                }
            }

            if (rotationAlignmentDisabled && applyRandomRotation)
            {
                EditorGUILayout.HelpBox(
                    "Rotation alignment needs a surface normal. Enable \"Snap to surfaces in region\" for Ground Raycast, or use Brush Paint.",
                    MessageType.None);
            }

            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(usePositionJitter)));
            if (usePositionJitter)
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(positionJitterMin)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(positionJitterMax)));
            }

            if (editorUiMode == ObjectScattererUiMode.Brush)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Brush (Scene tool)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(brushRadius)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(brushSpacing)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(brushStrokeMode)));
                EditorGUILayout.HelpBox(
                    "Select the Object Scatterer Scene tool, then paint on colliders. Stroke: drag with spacing. Click: one stamp per mouse down. The Scene toolbar mirrors these fields.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
            // Random Volume + Drop layout applies only to Batch Scatter; Brush always uses optional-drop controls.
            var randomVolumeBatchPhysicsUi = editorUiMode == ObjectScattererUiMode.Scatter &&
                                             spawnPlacementMode == SpawnPlacementMode.RandomVolumeThenDrop;
            if (randomVolumeBatchPhysicsUi)
            {
                EditorGUILayout.LabelField("Drop (Random Volume — Batch Scatter)", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Every spawn runs rigidbody simulation. Enable preview playback to see motion in the Scene view.",
                    MessageType.None);
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(maxDropSimulateSeconds)));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(physicsVisualPlayback)));
                using (new EditorGUI.DisabledScope(!physicsVisualPlayback))
                {
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(physicsStepsPerEditorFrame)));
                }
            }
            else
            {
                EditorGUILayout.LabelField("Optional drop (Ground Raycast / Brush)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(simulateDropOnSpawn)));
                using (new EditorGUI.DisabledScope(!simulateDropOnSpawn))
                {
                    if (editorUiMode == ObjectScattererUiMode.Brush)
                    {
                        EditorGUILayout.PropertyField(
                            _serializedObject.FindProperty(nameof(brushPhysicsSpawnHeight)),
                            new GUIContent(
                                "Spawn height along normal",
                                "Offset along the surface normal before drop simulation when painting with the brush."));
                    }

                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(maxDropSimulateSeconds)));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(physicsVisualPlayback)));
                    using (new EditorGUI.DisabledScope(!physicsVisualPlayback))
                    {
                        EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(physicsStepsPerEditorFrame)));
                    }
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(showRangeGizmos)));

            EditorGUILayout.Space(4f);
            _modeHelpExpanded = EditorGUILayout.Foldout(_modeHelpExpanded, "Mode help", true);
            if (_modeHelpExpanded)
            {
                if (editorUiMode == ObjectScattererUiMode.Scatter)
                {
                    EditorGUILayout.HelpBox(
                        "Ground Raycast: sphere or box around Scatter Center. With snap: random point + ray; hit must stay inside the region. Random Volume + Drop applies to Batch Scatter only (box spawn + physics). Brush Paint uses Optional drop in Physics, independent of this placement mode.",
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Brush samples a tangent disc and raycasts along -normal. Uses the same Layer mask and root filters as Ground Raycast.",
                        MessageType.None);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(12f);
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
            }

            if (editorUiMode == ObjectScattererUiMode.Scatter)
            {
                using (new EditorGUI.DisabledScope(ObjectScattererEditorTool.IsActiveTool()))
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var buttonHeight = Mathf.Max(42f, EditorGUIUtility.singleLineHeight * 2.2f);
                    if (GUILayout.Button("Scatter Objects", GUILayout.Height(buttonHeight), GUILayout.MinWidth(280f)))
                    {
                        ScatterBatch();
                    }

                    GUILayout.FlexibleSpace();
                }

                if (ObjectScattererEditorTool.IsActiveTool())
                {
                    EditorGUILayout.HelpBox(
                        "Batch scatter is disabled while the Object Scatterer Scene tool is active (use brush or switch tool).",
                        MessageType.Warning);
                }
                else if (spawnPlacementMode == SpawnPlacementMode.RandomVolumeThenDrop)
                {
                    EditorGUILayout.HelpBox(
                        "Random Volume mode: use Scatter Objects to spawn; brush is unavailable.",
                        MessageType.Info);
                }
            }
            else
            {
                if (ObjectScattererEditorTool.IsActiveTool())
                {
                    EditorGUILayout.HelpBox(
                        "Brush mode: paint in the Scene view with the Object Scatterer tool.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Activate the Object Scatterer Scene tool to paint. Batch scatter is under Scatter mode.",
                        MessageType.Info);
                }
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void ScatterBatch()
        {
            if (!ValidatePrefabListForAction(showDialog: true))
                return;

            if (objectCount <= 0)
            {
                EditorUtility.DisplayDialog("Object Scatterer", "Object Count must be greater than zero.", "OK");
                return;
            }

            if (!ValidateScaleAndJitter(showDialog: true))
                return;

            if (spawnPlacementMode == SpawnPlacementMode.GroundedRaycast)
            {
                if (scatterVolumeShape == ScatterVolumeShape.Sphere)
                {
                    if (scatterRadius <= 0f)
                    {
                        EditorUtility.DisplayDialog("Object Scatterer", "Scatter Radius must be greater than zero.", "OK");
                        return;
                    }
                }
                else if (scatterBoxHalfExtents.x <= 0f || scatterBoxHalfExtents.y <= 0f || scatterBoxHalfExtents.z <= 0f)
                {
                    EditorUtility.DisplayDialog(
                        "Object Scatterer",
                        "Scatter Box Half Extents must be greater than zero on each axis.",
                        "OK");
                    return;
                }
            }
            else
            {
                if (randomVolumeHalfExtents.x <= 0f || randomVolumeHalfExtents.y <= 0f || randomVolumeHalfExtents.z <= 0f)
                {
                    EditorUtility.DisplayDialog(
                        "Object Scatterer",
                        "Random Volume Half Extents must be greater than zero on each axis.",
                        "OK");
                    return;
                }
            }

            var center = scatterCenter != null ? scatterCenter.position : Vector3.zero;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Scatter Objects");
            var undoGroup = Undo.GetCurrentGroup();

            var parentTransform = GetOrCreateParentForBatch();
            if (parentTransform == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            var validPrefabs = BuildValidPrefabList();
            var random = new System.Random();

            if (spawnPlacementMode == SpawnPlacementMode.RandomVolumeThenDrop)
            {
                for (var i = 0; i < objectCount; i++)
                {
                    var prefab = validPrefabs[random.Next(0, validPrefabs.Count)];
                    var worldPos = SampleRandomPointInVolume(center, randomVolumeHalfExtents, random);
                    if (usePositionJitter)
                        worldPos += SamplePositionJitterOffset(random);

                    TrySpawnVolumeDropInstance(prefab, parentTransform, worldPos, random, registerUndo: true);
                }

                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            var placed = 0;
            var attempts = 0;
            var maxAttempts = Mathf.Max(objectCount * 40, objectCount + 10);

            while (placed < objectCount && attempts < maxAttempts)
            {
                attempts++;

                var prefab = validPrefabs[random.Next(0, validPrefabs.Count)];

                if (!TryGetGroundedSpawnPoint(random, center, out var worldPos, out var surfaceNormal, out var hasSurface))
                    continue;

                if (!TrySpawnOneInstance(prefab, parentTransform, worldPos, surfaceNormal, hasSurface, random,
                        registerUndo: true))
                    continue;

                placed++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (placed < objectCount)
            {
                var msg = surfaceFitMode
                    ? $"Placed {placed} of {objectCount} objects. Some ray samples missed a surface within the attempt limit."
                    : $"Placed {placed} of {objectCount} objects (unexpected shortfall).";
                EditorUtility.DisplayDialog("Object Scatterer", msg, "OK");
            }
        }

        private bool TryGetGroundedSpawnPoint(System.Random random, Vector3 center, out Vector3 worldPos,
            out Vector3 surfaceNormal, out bool hasSurface)
        {
            worldPos = default;
            surfaceNormal = Vector3.up;
            hasSurface = surfaceFitMode;

            if (!surfaceFitMode)
            {
                if (scatterVolumeShape == ScatterVolumeShape.Sphere)
                    worldPos = ObjectScattererPlacement.SampleUniformInSphere(center, scatterRadius, random);
                else
                    worldPos = ObjectScattererPlacement.SampleUniformInBox(center, scatterBoxHalfExtents, random);
                return true;
            }

            return TrySampleSurfaceHit(center, random, out worldPos, out surfaceNormal);
        }

        /// <summary>
        /// Random point in volume, random ray direction, closest valid hit; hit point must lie inside scatter volume.
        /// </summary>
        private bool TrySampleSurfaceHit(Vector3 center, System.Random random, out Vector3 hitPoint,
            out Vector3 hitNormal)
        {
            hitPoint = default;
            hitNormal = Vector3.up;

            const int maxAttempts = 96;
            var maxRayLen = scatterVolumeShape == ScatterVolumeShape.Sphere
                ? scatterRadius * 2f + 0.02f
                : scatterBoxHalfExtents.magnitude * 2f + 0.02f;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var p = scatterVolumeShape == ScatterVolumeShape.Sphere
                    ? ObjectScattererPlacement.SampleUniformInSphere(center, scatterRadius, random)
                    : ObjectScattererPlacement.SampleUniformInBox(center, scatterBoxHalfExtents, random);

                var dir = ObjectScattererPlacement.RandomUnitVector(random);
                if (dir.sqrMagnitude < 1e-8f)
                    dir = Vector3.forward;

                var ray = new Ray(p, dir);
                if (!ObjectScattererSceneRaycast.TryRaycast(ray, maxRayLen, raycastLayers, ScatterHitTransformPasses,
                        out var hit))
                    continue;

                if (!IsScatterHitPointInVolume(center, hit.point))
                    continue;

                hitPoint = hit.point;
                hitNormal = hit.normal;
                return true;
            }

            return false;
        }

        private bool IsScatterHitPointInVolume(Vector3 center, Vector3 worldPoint)
        {
            if (scatterVolumeShape == ScatterVolumeShape.Sphere)
                return ObjectScattererPlacement.IsPointInSphere(center, scatterRadius, worldPoint);
            return ObjectScattererPlacement.IsPointInAxisAlignedBox(center, scatterBoxHalfExtents, worldPoint);
        }

        /// <summary>
        /// Uniform random point inside an axis-aligned box: center +/- halfExtents per axis.
        /// </summary>
        private static Vector3 SampleRandomPointInVolume(Vector3 center, Vector3 halfExtents, System.Random random)
        {
            var rx = (float)(random.NextDouble() * 2.0 - 1.0) * halfExtents.x;
            var ry = (float)(random.NextDouble() * 2.0 - 1.0) * halfExtents.y;
            var rz = (float)(random.NextDouble() * 2.0 - 1.0) * halfExtents.z;
            return center + new Vector3(rx, ry, rz);
        }

        /// <summary>
        /// World-space jitter offset from min/max (used in volume mode without re-raycasting).
        /// </summary>
        private Vector3 SamplePositionJitterOffset(System.Random random)
        {
            var jx = Mathf.Lerp(positionJitterMin.x, positionJitterMax.x, (float)random.NextDouble());
            var jy = Mathf.Lerp(positionJitterMin.y, positionJitterMax.y, (float)random.NextDouble());
            var jz = Mathf.Lerp(positionJitterMin.z, positionJitterMax.z, (float)random.NextDouble());
            return new Vector3(jx, jy, jz);
        }

        /// <summary>
        /// Project assets: PrefabUtility.InstantiatePrefab. Scene/hierarchy objects: UnityEngine.Object.Instantiate duplicate.
        /// Keeps the instance root's inherited local scale and multiplies it by <paramref name="uniformScale"/> (random factor from min/max).
        /// Parents with worldPositionStays so world pose/scale are preserved relative to the parent.
        /// </summary>
        private static GameObject InstantiateScatterSourceWithUniformWorldScale(GameObject source, Transform parentTransform,
            Vector3 worldPosition, Quaternion worldRotation, float uniformScale)
        {
            if (source == null)
                return null;

            GameObject instance;
            if (EditorUtility.IsPersistent(source))
                instance = (GameObject)PrefabUtility.InstantiatePrefab(source, null);
            else
                instance = UnityEngine.Object.Instantiate(source);

            if (instance == null)
                return null;

            var t = instance.transform;
            var inheritedScale = t.localScale;
            t.SetPositionAndRotation(worldPosition, worldRotation);
            t.localScale = Vector3.Scale(inheritedScale, Vector3.one * uniformScale);
            if (parentTransform != null)
                t.SetParent(parentTransform, worldPositionStays: true);

            Physics.SyncTransforms();
            return instance;
        }

        /// <summary>
        /// Spawns at an arbitrary world position, then runs editor physics so instances fall and collide.
        /// </summary>
        private bool TrySpawnVolumeDropInstance(GameObject source, Transform parentTransform, Vector3 worldPosition,
            System.Random random, bool registerUndo)
        {
            var uniformScale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
            var instance = InstantiateScatterSourceWithUniformWorldScale(source, parentTransform, worldPosition,
                SampleRandomRotation(random), uniformScale);
            if (instance == null)
                return false;

            if (registerUndo)
                Undo.RegisterCreatedObjectUndo(instance, "Scatter Object");

            SimulateDrop(instance);
            return true;
        }

        private bool ValidatePrefabListForAction(bool showDialog)
        {
            if (scatterSources == null || scatterSources.Count == 0)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Object Scatterer", "Add at least one entry to Scatter Sources.", "OK");
                return false;
            }

            foreach (var p in scatterSources)
            {
                if (p == null)
                {
                    if (showDialog)
                        EditorUtility.DisplayDialog("Object Scatterer", "Scatter Sources contains null entries. Remove or assign all entries.", "OK");
                    return false;
                }
            }

            return true;
        }

        private static List<GameObject> BuildValidPrefabListFrom(IReadOnlyList<GameObject> source)
        {
            var list = new List<GameObject>();
            foreach (var p in source)
            {
                if (p != null)
                    list.Add(p);
            }

            return list;
        }

        private List<GameObject> BuildValidPrefabList()
        {
            return BuildValidPrefabListFrom(scatterSources);
        }

        private bool ValidateScaleAndJitter(bool showDialog)
        {
            if (minScale <= 0f || maxScale <= 0f)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Object Scatterer", "Min Scale and Max Scale must be greater than zero.", "OK");
                return false;
            }

            if (minScale > maxScale)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Object Scatterer", "Min Scale cannot be greater than Max Scale.", "OK");
                return false;
            }

            if (usePositionJitter)
            {
                if (positionJitterMin.x > positionJitterMax.x || positionJitterMin.y > positionJitterMax.y ||
                    positionJitterMin.z > positionJitterMax.z)
                {
                    if (showDialog)
                        EditorUtility.DisplayDialog("Object Scatterer", "Position Jitter Min must be <= Max on each axis.", "OK");
                    return false;
                }
            }

            if (applyRandomRotation)
            {
                if (rotationRandomMode == RotationRandomMode.YawOnly)
                {
                    if (rotationMinDeg.y > rotationMaxDeg.y)
                    {
                        if (showDialog)
                            EditorUtility.DisplayDialog("Object Scatterer", "Rotation Y Min Deg must be <= Y Max.", "OK");
                        return false;
                    }
                }
                else
                {
                    if (rotationMinDeg.x > rotationMaxDeg.x || rotationMinDeg.y > rotationMaxDeg.y ||
                        rotationMinDeg.z > rotationMaxDeg.z)
                    {
                        if (showDialog)
                            EditorUtility.DisplayDialog("Object Scatterer", "Rotation Min Deg must be <= Max on each axis.", "OK");
                        return false;
                    }
                }
            }

            return true;
        }

        private Transform GetOrCreateParentForBatch()
        {
            if (parentContainer != null)
                return parentContainer;

            var parentGo = new GameObject("Scattered Objects");
            Undo.RegisterCreatedObjectUndo(parentGo, "Create Scatter Parent");
            var t = parentGo.transform;
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        private Transform GetOrCreateParentForBrushStroke()
        {
            if (parentContainer != null)
                return parentContainer;

            if (_brushStrokeParent != null)
                return _brushStrokeParent;

            var parentGo = new GameObject("Scattered Objects");
            Undo.RegisterCreatedObjectUndo(parentGo, "Create Scatter Parent");
            _brushStrokeParent = parentGo.transform;
            _brushStrokeParent.position = Vector3.zero;
            _brushStrokeParent.rotation = Quaternion.identity;
            _brushStrokeParent.localScale = Vector3.one;
            return _brushStrokeParent;
        }

        /// <summary>
        /// Applies jitter: world offset when not surface-attached; tangent-plane offset + short ray along -normal when attached.
        /// </summary>
        private bool ResolveFinalSpawnPosition(Vector3 basePos, Vector3 normal, bool hasSurface, System.Random random,
            out Vector3 outPos, out Vector3 outNormal)
        {
            outPos = basePos;
            outNormal = normal;

            if (!usePositionJitter)
                return true;

            var j = SamplePositionJitterOffset(random);
            if (!hasSurface)
            {
                outPos = basePos + j;
                return true;
            }

            var offsetInPlane = Vector3.ProjectOnPlane(j, normal);
            const float lift = 0.25f;
            var origin = basePos + offsetInPlane + normal * lift;
            var ray = new Ray(origin, -normal);
            if (ObjectScattererSceneRaycast.TryRaycast(ray, lift + 3f, raycastLayers, ScatterHitTransformPasses,
                    out var hit))
            {
                outPos = hit.point;
                outNormal = hit.normal;
                return true;
            }

            return false;
        }

        private Quaternion SampleRandomRotation(System.Random random)
        {
            if (!applyRandomRotation)
                return Quaternion.identity;

            if (rotationRandomMode == RotationRandomMode.YawOnly)
            {
                var yDeg = Mathf.Lerp(rotationMinDeg.y, rotationMaxDeg.y, (float)random.NextDouble());
                return Quaternion.Euler(0f, yDeg, 0f);
            }

            var x = Mathf.Lerp(rotationMinDeg.x, rotationMaxDeg.x, (float)random.NextDouble());
            var y = Mathf.Lerp(rotationMinDeg.y, rotationMaxDeg.y, (float)random.NextDouble());
            var z = Mathf.Lerp(rotationMinDeg.z, rotationMaxDeg.z, (float)random.NextDouble());
            return Quaternion.Euler(x, y, z);
        }

        private bool TrySpawnOneInstance(GameObject prefab, Transform parentTransform, RaycastHit hit,
            System.Random random, bool registerUndo, bool spawnFromBrush = false)
        {
            return TrySpawnOneInstance(prefab, parentTransform, hit.point, hit.normal, true, random, registerUndo,
                spawnFromBrush);
        }

        private bool TrySpawnOneInstance(GameObject prefab, Transform parentTransform, Vector3 worldPosition,
            Vector3 surfaceNormal, bool hasSurface, System.Random random, bool registerUndo,
            bool spawnFromBrush = false)
        {
            if (!ResolveFinalSpawnPosition(worldPosition, surfaceNormal, hasSurface, random, out var finalPos,
                    out var finalNormal))
                return false;

            var placePos = finalPos;
            var lift = Mathf.Max(0f, brushPhysicsSpawnHeight);
            if (spawnFromBrush && simulateDropOnSpawn && lift > 0f && hasSurface)
                placePos += finalNormal.normalized * lift;

            var uniformScale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
            var rotation = ComputeFinalRotation(finalNormal, hasSurface, random);
            var instance = InstantiateScatterSourceWithUniformWorldScale(prefab, parentTransform, placePos, rotation,
                uniformScale);
            if (instance == null)
                return false;

            if (registerUndo)
                Undo.RegisterCreatedObjectUndo(instance, "Scatter Object");

            if (simulateDropOnSpawn)
                SimulateDrop(instance);

            return true;
        }

        private Quaternion ComputeFinalRotation(Vector3 surfaceNormal, bool hasSurface, System.Random random)
        {
            if (!applyRandomRotation)
            {
                if (!hasSurface || rotationAlignment == RotationAlignment.World)
                    return Quaternion.identity;
                return Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            }

            var rotRand = SampleRandomRotation(random);
            if (!hasSurface)
                return rotRand;

            if (rotationAlignment == RotationAlignment.World)
                return rotRand;

            return Quaternion.FromToRotation(Vector3.up, surfaceNormal) * rotRand;
        }

        /// <summary>
        /// Editor physics: instant solve, or multi-frame playback when physicsVisualPlayback is true.
        /// All Rigidbodies under <paramref name="go"/> (including children) participate; if none exist, a temporary Rigidbody is added on the root only.
        /// If the hierarchy has no colliders, temporary MeshCollider/BoxCollider are built from MeshRenderer/MeshFilter and SkinnedMeshRenderer (including children).
        /// </summary>
        private void SimulateDrop(GameObject go)
        {
            var tempColliders = new List<ObjectScattererTempPhysicsFromMeshes.TempColliderInfo>(8);

            // Rigidbody on the root must exist before MeshCollider children are added so compound colliders
            // cook with the correct world transform (parent scale + local scale compensation).
            var bodies = CollectRigidbodiesForDrop(go);
            if (!ObjectScattererTempPhysicsFromMeshes.HasAnyCollider(go))
            {
                ObjectScattererTempPhysicsFromMeshes.AddTemporaryCollidersFromRenderMeshes(go, tempColliders);
                ObjectScattererTempPhysicsFromMeshes.RefreshMeshCollidersForCurrentTransforms(tempColliders);
                ObjectScattererTempPhysicsFromMeshes.CompensateTemporaryCollidersForRigidbodyRootScale(tempColliders,
                    go.transform);
            }

            foreach (var b in bodies)
            {
                if (b.Rb == null)
                    continue;
                b.Rb.isKinematic = false;
                b.Rb.useGravity = true;
            }

            Physics.SyncTransforms();

            if (physicsVisualPlayback)
            {
                ObjectScattererPhysicsPlayback.Enqueue(go, bodies, tempColliders, maxDropSimulateSeconds,
                    physicsStepsPerEditorFrame);
                return;
            }

            var savedCollisionModes = new CollisionDetectionMode[bodies.Count];
            ObjectScattererDropPhysicsSettings.BeginSolverBoostForSession();
            ObjectScattererDropPhysicsSettings.BeginPerBodyContinuousAgainstStatic(bodies, savedCollisionModes);

            var wasSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            var allowed = new List<Rigidbody>(bodies.Count);
            foreach (var b in bodies)
            {
                if (b.Rb != null)
                    allowed.Add(b.Rb);
            }

            try
            {
                ObjectScattererPhysicsIsolation.Begin(allowed);
                try
                {
                    var dt = ObjectScattererDropPhysicsSettings.GetDropSimulationFixedDeltaTime();
                    var maxSteps = Mathf.Max(1, Mathf.CeilToInt(maxDropSimulateSeconds / dt));
                    for (var i = 0; i < maxSteps; i++)
                    {
                        Physics.Simulate(dt);
                        if (AllRigidbodiesSleeping(bodies))
                            break;
                    }
                }
                finally
                {
                    ObjectScattererPhysicsIsolation.End();
                }
            }
            finally
            {
                ObjectScattererDropPhysicsSettings.RestorePerBodyCollisionModes(bodies, savedCollisionModes);
                ObjectScattererDropPhysicsSettings.EndSolverBoostForSession();
                Physics.simulationMode = wasSimMode;
            }

            foreach (var b in bodies)
            {
                if (b.Rb == null)
                    continue;
                Undo.RecordObject(b.Rb.transform, "Scatter settle");
                if (b.AddedTemp)
                    ObjectScattererTempPhysicsFromMeshes.DestroyComponentImmediateSafe(b.Rb);
                else
                {
                    b.Rb.isKinematic = true;
                    Undo.RecordObject(b.Rb, "Scatter settle");
                }
            }

            ObjectScattererTempPhysicsFromMeshes.DestroyTemporaryColliders(tempColliders);
        }

        private static List<ObjectScattererPhysicsPlayback.RigidbodyDropInfo> CollectRigidbodiesForDrop(GameObject root)
        {
            var existing = root.GetComponentsInChildren<Rigidbody>(true);
            var list = new List<ObjectScattererPhysicsPlayback.RigidbodyDropInfo>(Mathf.Max(1, existing.Length));
            if (existing.Length == 0)
            {
                var rb = root.AddComponent<Rigidbody>();
                list.Add(new ObjectScattererPhysicsPlayback.RigidbodyDropInfo { Rb = rb, AddedTemp = true });
                return list;
            }

            foreach (var rb in existing)
                list.Add(new ObjectScattererPhysicsPlayback.RigidbodyDropInfo { Rb = rb, AddedTemp = false });

            return list;
        }

        private static bool AllRigidbodiesSleeping(IReadOnlyList<ObjectScattererPhysicsPlayback.RigidbodyDropInfo> bodies)
        {
            for (var i = 0; i < bodies.Count; i++)
            {
                var rb = bodies[i].Rb;
                if (rb == null)
                    continue;
                if (!rb.IsSleeping())
                    return false;
            }

            return true;
        }
    }
}
