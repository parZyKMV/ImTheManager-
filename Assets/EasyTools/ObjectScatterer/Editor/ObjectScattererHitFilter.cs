using System.Collections.Generic;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Include / exclude GameObject roots (default: include list empty = allow all; exclude always subtracts).
    /// </summary>
    internal static class ObjectScattererHitFilter
    {
        public static bool TransformPasses(
            Transform hitTransform,
            IReadOnlyList<GameObject> includeRoots,
            IReadOnlyList<GameObject> excludeRoots)
        {
            if (hitTransform == null)
                return false;

            if (excludeRoots != null)
            {
                for (var i = 0; i < excludeRoots.Count; i++)
                {
                    var ex = excludeRoots[i];
                    if (ex != null && IsTransformUnderRoot(hitTransform, ex.transform))
                        return false;
                }
            }

            if (includeRoots == null || includeRoots.Count == 0)
                return true;

            for (var i = 0; i < includeRoots.Count; i++)
            {
                var inc = includeRoots[i];
                if (inc != null && IsTransformUnderRoot(hitTransform, inc.transform))
                    return true;
            }

            return false;
        }

        private static bool IsTransformUnderRoot(Transform t, Transform root)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                if (cur == root)
                    return true;
            }

            return false;
        }
    }
}
