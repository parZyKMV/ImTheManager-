using System;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Ray vs MeshRenderer / SkinnedMeshRenderer triangles when no collider is present (editor fallback).
    /// Includes inactive GameObjects and disabled renderers so nested / child meshes still participate.
    /// </summary>
    internal static class ObjectScattererMeshRaycast
    {
        private static Mesh _bakedMeshScratch;

        public static bool TryRaycastMeshes(Ray worldRay, float maxDistance, LayerMask layerMask, out RaycastHit hit)
        {
            return TryRaycastMeshes(worldRay, maxDistance, layerMask, null, out hit);
        }

        public static bool TryRaycastMeshes(Ray worldRay, float maxDistance, LayerMask layerMask,
            Func<Transform, bool> transformFilter, out RaycastHit hit)
        {
            hit = default;
            var bestT = float.MaxValue;
            var found = false;

            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
            foreach (var mr in renderers)
            {
                if (mr == null)
                    continue;
                if (transformFilter != null && !transformFilter(mr.transform))
                    continue;
                var layer = 1 << mr.gameObject.layer;
                if ((layerMask.value & layer) == 0)
                    continue;

                if (!mr.bounds.IntersectRay(worldRay, out var enter))
                    continue;
                if (enter > maxDistance)
                    continue;

                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;

                var mesh = mf.sharedMesh;
                if (!mesh.isReadable)
                    continue;

                var matrix = mr.transform.localToWorldMatrix;
                if (RaycastTrianglesAgainstMesh(mesh, matrix, worldRay, maxDistance, ref bestT, ref hit))
                    found = true;
            }

            var skinned = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include);
            foreach (var smr in skinned)
            {
                if (smr == null || smr.sharedMesh == null)
                    continue;
                if (transformFilter != null && !transformFilter(smr.transform))
                    continue;
                var layer = 1 << smr.gameObject.layer;
                if ((layerMask.value & layer) == 0)
                    continue;

                if (!smr.bounds.IntersectRay(worldRay, out var enter))
                    continue;
                if (enter > maxDistance)
                    continue;

                EnsureBakedMeshScratch();
                smr.BakeMesh(_bakedMeshScratch);
                var matrix = smr.transform.localToWorldMatrix;
                if (RaycastTrianglesAgainstMesh(_bakedMeshScratch, matrix, worldRay, maxDistance, ref bestT, ref hit))
                    found = true;
            }

            return found;
        }

        private static void EnsureBakedMeshScratch()
        {
            if (_bakedMeshScratch == null)
                _bakedMeshScratch = new Mesh();
        }

        private static bool RaycastTrianglesAgainstMesh(Mesh mesh, Matrix4x4 localToWorld, Ray worldRay,
            float maxDistance, ref float bestT, ref RaycastHit hit)
        {
            var tris = mesh.triangles;
            if (tris == null || tris.Length == 0)
                return false;

            var verts = mesh.vertices;
            if (verts == null || verts.Length == 0)
                return false;

            var found = false;
            for (var i = 0; i < tris.Length; i += 3)
            {
                var w0 = localToWorld.MultiplyPoint3x4(verts[tris[i]]);
                var w1 = localToWorld.MultiplyPoint3x4(verts[tris[i + 1]]);
                var w2 = localToWorld.MultiplyPoint3x4(verts[tris[i + 2]]);

                if (!RayTriangleWorld(worldRay, w0, w1, w2, out var t))
                    continue;
                if (t < 1e-4f || t > maxDistance)
                    continue;

                if (t < bestT)
                {
                    bestT = t;
                    hit.point = worldRay.GetPoint(t);
                    var e0 = w1 - w0;
                    var e1 = w2 - w0;
                    var n = Vector3.Cross(e0, e1);
                    if (n.sqrMagnitude < 1e-12f)
                        continue;
                    n.Normalize();
                    if (Vector3.Dot(n, -worldRay.direction) < 0f)
                        n = -n;
                    hit.normal = n;
                    hit.distance = t;
                    found = true;
                }
            }

            return found;
        }

        private static bool RayTriangleWorld(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
        {
            t = 0f;
            const float eps = 1e-6f;
            var e1 = v1 - v0;
            var e2 = v2 - v0;
            var dir = ray.direction;
            var orig = ray.origin;
            var p = Vector3.Cross(dir, e2);
            var det = Vector3.Dot(e1, p);
            if (det > -eps && det < eps)
                return false;
            var invDet = 1f / det;
            var tvec = orig - v0;
            var u = Vector3.Dot(tvec, p) * invDet;
            if (u < 0f || u > 1f)
                return false;
            var q = Vector3.Cross(tvec, e1);
            var v = Vector3.Dot(dir, q) * invDet;
            if (v < 0f || u + v > 1f)
                return false;
            t = Vector3.Dot(e2, q) * invDet;
            return t > eps;
        }
    }
}
