using System;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Physics raycast first, then TerrainCollider, then triangle tests against MeshRenderer and SkinnedMeshRenderer
    /// (no MeshCollider required; static meshes need Read/Write enabled on the mesh asset).
    /// </summary>
    internal static class ObjectScattererSceneRaycast
    {
        private const float MeshRaycastMaxDistance = 5000f;

        public static bool TryRaycast(Ray ray, float maxDistance, LayerMask layerMask, out RaycastHit bestHit)
        {
            return TryRaycast(ray, maxDistance, layerMask, null, out bestHit);
        }

        /// <param name="transformFilter">If non-null, only hits whose transform passes this test are considered (closest valid hit).</param>
        public static bool TryRaycast(Ray ray, float maxDistance, LayerMask layerMask,
            Func<Transform, bool> transformFilter, out RaycastHit bestHit)
        {
            bestHit = default;
            var bestDist = float.MaxValue;
            var found = false;

            var physicsHits = Physics.RaycastAll(ray, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            Array.Sort(physicsHits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in physicsHits)
            {
                if (h.collider == null)
                    continue;
                if (transformFilter != null && !transformFilter(h.collider.transform))
                    continue;
                bestHit = h;
                bestDist = h.distance;
                found = true;
                break;
            }

            foreach (var terrain in Terrain.activeTerrains)
            {
                var tc = terrain.GetComponent<TerrainCollider>();
                if (tc == null || !tc.enabled)
                    continue;
                if (!tc.Raycast(ray, out var th, maxDistance) || th.distance > maxDistance)
                    continue;
                if (transformFilter != null && th.collider != null && !transformFilter(th.collider.transform))
                    continue;
                if (th.distance < bestDist)
                {
                    bestDist = th.distance;
                    bestHit = th;
                    found = true;
                }
            }

            var meshMax = Mathf.Min(maxDistance, MeshRaycastMaxDistance);
            if (ObjectScattererMeshRaycast.TryRaycastMeshes(ray, meshMax, layerMask, transformFilter, out var mh) &&
                mh.distance < bestDist)
            {
                bestHit = mh;
                found = true;
            }

            return found;
        }
    }
}
