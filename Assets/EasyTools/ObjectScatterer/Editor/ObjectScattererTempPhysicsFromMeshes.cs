using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// When a hierarchy has no colliders, adds temporary MeshCollider (or BoxCollider fallback) from render meshes.
    /// Skinned meshes bake to an owned <see cref="Mesh"/> that must be destroyed with the collider.
    /// Box colliders use mesh-local / renderer-local bounds (not world AABB) so size matches the mesh under rotation and scale.
    /// </summary>
    internal static class ObjectScattererTempPhysicsFromMeshes
    {
        public struct TempColliderInfo
        {
            public Collider Collider;
            public Mesh OwnedMesh;
        }

        /// <summary>
        /// Returns true if any collider exists under <paramref name="root"/> (including inactive).
        /// </summary>
        public static bool HasAnyCollider(GameObject root)
        {
            return root.GetComponentsInChildren<Collider>(true).Length > 0;
        }

        /// <summary>
        /// Returns true if any rigidbody exists under <paramref name="root"/> (including inactive).
        /// </summary>
        public static bool HasAnyRigidbody(GameObject root)
        {
            return root.GetComponentsInChildren<Rigidbody>(true).Length > 0;
        }

        /// <summary>
        /// Adds temporary colliders from MeshRenderer/MeshFilter and SkinnedMeshRenderer under <paramref name="root"/>.
        /// </summary>
        public static void AddTemporaryCollidersFromRenderMeshes(GameObject root, List<TempColliderInfo> outTemp)
        {
            if (root == null || outTemp == null)
                return;

            var meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                if (mr == null)
                    continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;

                var mesh = mf.sharedMesh;
                if (mesh.isReadable && mesh.triangles.Length > 0)
                {
                    var mc = mr.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                    mc.convex = true;
                    outTemp.Add(new TempColliderInfo { Collider = mc, OwnedMesh = null });
                }
                else
                    AddBoxColliderFromMeshLocalBounds(mesh, mr.gameObject, outTemp);
            }

            var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinned)
            {
                if (smr == null || smr.sharedMesh == null)
                    continue;

                var baked = new Mesh();
                smr.BakeMesh(baked);
                if (baked.triangles.Length == 0)
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                    AddBoxColliderFromSkinnedLocalBounds(smr, outTemp);
                    continue;
                }

                var mc = smr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = baked;
                mc.convex = true;
                outTemp.Add(new TempColliderInfo { Collider = mc, OwnedMesh = baked });
            }
        }

        /// <summary>
        /// Mesh.bounds is the axis-aligned bounds of the mesh in local space (matches MeshCollider footprint for an AABB).
        /// </summary>
        private static void AddBoxColliderFromMeshLocalBounds(Mesh mesh, GameObject go, List<TempColliderInfo> outTemp)
        {
            if (mesh == null || go == null)
                return;

            var lb = mesh.bounds;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = lb.center;
            bc.size = lb.size;
            outTemp.Add(new TempColliderInfo { Collider = bc, OwnedMesh = null });
        }

        /// <summary>
        /// SkinnedMeshRenderer.localBounds is in the renderer's local space.
        /// </summary>
        private static void AddBoxColliderFromSkinnedLocalBounds(SkinnedMeshRenderer smr, List<TempColliderInfo> outTemp)
        {
            if (smr == null)
                return;

            var lb = smr.localBounds;
            var bc = smr.gameObject.AddComponent<BoxCollider>();
            bc.center = lb.center;
            bc.size = lb.size;
            outTemp.Add(new TempColliderInfo { Collider = bc, OwnedMesh = null });
        }

        /// <summary>
        /// Convex MeshCollider hull is cooked when the mesh is assigned; after parenting / scale changes,
        /// reassign the mesh and sync transforms so compound colliders match the visual scale (e.g. parent lossy scale 2x with child local scale compensation).
        /// </summary>
        public static void RefreshMeshCollidersForCurrentTransforms(List<TempColliderInfo> temp)
        {
            if (temp == null)
                return;
            for (var i = 0; i < temp.Count; i++)
            {
                if (temp[i].Collider is MeshCollider mc && mc.sharedMesh != null)
                {
                    var m = mc.sharedMesh;
                    mc.sharedMesh = null;
                    mc.sharedMesh = m;
                }
            }

            Physics.SyncTransforms();
        }

        /// <summary>
        /// Compound colliders (often on <b>children</b> of the <see cref="Rigidbody"/>) can cook at the wrong size in
        /// editor physics when overall scale ≠ 1. Correction divides mesh vertices / box extents in world space per axis.
        /// Scale source order: <b>rigidbody</b> transform (documented compound reference) → <paramref name="spawnRoot"/>
        /// (instance root when child world scale ~1 due to local compensation) → <b>collider</b> transform (leaf).
        /// </summary>
        public static void CompensateTemporaryCollidersForRigidbodyRootScale(List<TempColliderInfo> temp,
            Transform spawnRoot)
        {
            if (temp == null || spawnRoot == null)
                return;

            for (var i = 0; i < temp.Count; i++)
            {
                var info = temp[i];
                if (info.Collider == null)
                    continue;

                var rb = info.Collider.GetComponentInParent<Rigidbody>();
                var colTf = info.Collider.transform;
                var divide = Vector3.one;
                if (rb != null)
                    divide = GetCompoundColliderWorldDivideFactors(rb.transform);
                if (IsApproximatelyVector3One(divide))
                    divide = GetCompoundColliderWorldDivideFactors(spawnRoot);
                if (IsApproximatelyVector3One(divide))
                    divide = GetCompoundColliderWorldDivideFactors(colTf);
                if (IsApproximatelyVector3One(divide))
                    continue;

                if (info.Collider is MeshCollider mc && mc.sharedMesh != null)
                {
                    var oldOwned = info.OwnedMesh;
                    var newMesh = DuplicateMeshWithWorldAxisDivide(mc.sharedMesh, mc.transform, divide);
                    if (newMesh == null)
                        continue;

                    if (oldOwned != null)
                        UnityEngine.Object.DestroyImmediate(oldOwned);

                    var m = newMesh;
                    mc.sharedMesh = null;
                    mc.sharedMesh = m;
                    info.OwnedMesh = newMesh;
                    info.Collider = mc;
                    temp[i] = info;
                }
                else if (info.Collider is BoxCollider bc)
                {
                    var inv = new Vector3(InvSafe(divide.x), InvSafe(divide.y), InvSafe(divide.z));
                    bc.size = Vector3.Scale(bc.size, inv);
                    bc.center = Vector3.Scale(bc.center, inv);
                    temp[i] = info;
                }
            }

            Physics.SyncTransforms();
        }

        private static float InvSafe(float v)
        {
            return 1f / Mathf.Max(1e-8f, Mathf.Abs(v));
        }

        private static bool IsApproximatelyVector3One(Vector3 v)
        {
            const float e = 1e-4f;
            return Mathf.Abs(v.x - 1f) < e && Mathf.Abs(v.y - 1f) < e && Mathf.Abs(v.z - 1f) < e;
        }

        /// <summary>
        /// Per-axis divisors from <see cref="Transform.lossyScale"/>; axes already ~1 use 1 (no change).
        /// </summary>
        private static Vector3 GetCompoundColliderWorldDivideFactors(Transform t)
        {
            if (t == null)
                return Vector3.one;
            const float e = 1e-4f;
            var s = t.lossyScale;
            return new Vector3(
                Mathf.Abs(s.x - 1f) < e ? 1f : s.x,
                Mathf.Abs(s.y - 1f) < e ? 1f : s.y,
                Mathf.Abs(s.z - 1f) < e ? 1f : s.z);
        }

        /// <summary>
        /// Undo compound overshoot: scale each vertex offset from <paramref name="meshTf"/> world position (mesh pivot)
        /// in world axes by <c>1/divideWorld</c>. Scaling around world origin (0,0,0) would shift the hull away from the
        /// renderer when the object is not at the origin — that caused visible position offset while size looked OK.
        /// </summary>
        private static Mesh DuplicateMeshWithWorldAxisDivide(Mesh source, Transform meshTf, Vector3 divideWorld)
        {
            if (source == null || meshTf == null)
                return null;

            var verts = source.vertices;
            if (verts == null || verts.Length == 0)
                return null;

            var inv = new Vector3(InvSafe(divideWorld.x), InvSafe(divideWorld.y), InvSafe(divideWorld.z));
            var pivotW = meshTf.position;
            var newVerts = new Vector3[verts.Length];
            for (var i = 0; i < verts.Length; i++)
            {
                var vw = meshTf.TransformPoint(verts[i]);
                vw = pivotW + Vector3.Scale(vw - pivotW, inv);
                newVerts[i] = meshTf.InverseTransformPoint(vw);
            }

            var copy = new Mesh
            {
                name = source.name + "_ScatterPhysComp",
                indexFormat = source.indexFormat
            };
            copy.vertices = newVerts;
            copy.triangles = source.triangles;
            copy.RecalculateBounds();
            copy.RecalculateNormals();
            return copy;
        }

        public static void DestroyTemporaryColliders(List<TempColliderInfo> temp)
        {
            if (temp == null)
                return;
            for (var i = 0; i < temp.Count; i++)
            {
                var c = temp[i].Collider;
                if (c != null)
                    DestroyColliderImmediateSafe(c);
                var m = temp[i].OwnedMesh;
                if (m != null)
                    UnityEngine.Object.DestroyImmediate(m);
            }

            temp.Clear();
        }

        /// <summary>
        /// <see cref="DestroyImmediate"/> on a <see cref="Collider"/> while the Inspector targets that component
        /// can throw <see cref="SerializedObjectNotCreatableException"/> in MeshColliderEditor. Clear selection first.
        /// </summary>
        public static void DestroyColliderImmediateSafe(Collider c)
        {
            if (c == null)
                return;
            PrepareDestroyComponent(c);
            UnityEngine.Object.DestroyImmediate(c);
        }

        /// <summary>
        /// Same selection fix as <see cref="DestroyColliderImmediateSafe"/> for other components (e.g. temporary <see cref="Rigidbody"/>).
        /// </summary>
        public static void DestroyComponentImmediateSafe(Component comp)
        {
            if (comp == null)
                return;
            PrepareDestroyComponent(comp);
            UnityEngine.Object.DestroyImmediate(comp);
        }

        private static void PrepareDestroyComponent(Component comp)
        {
            if (comp == null)
                return;
            var objs = Selection.objects;
            var list = new List<Object>(objs.Length);
            var removed = false;
            for (var i = 0; i < objs.Length; i++)
            {
                if (objs[i] != comp)
                    list.Add(objs[i]);
                else
                    removed = true;
            }

            if (removed)
                Selection.objects = list.ToArray();
            if (Selection.activeObject == comp && comp.gameObject != null)
                Selection.activeGameObject = comp.gameObject;
        }
    }
}
