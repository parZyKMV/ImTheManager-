using System.Collections.Generic;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// During editor Physics.Simulate / Physics2D.Simulate, only specific bodies should integrate; others are temporarily frozen.
    /// </summary>
    internal static class ObjectScattererPhysicsIsolation
    {
        private static readonly List<Rigidbody> Frozen = new List<Rigidbody>(256);
        private static readonly List<bool> WasKinematic = new List<bool>(256);
        private static readonly List<Rigidbody2D> Frozen2D = new List<Rigidbody2D>(256);
        private static readonly List<RigidbodyType2D> WasBodyType2D = new List<RigidbodyType2D>(256);

        private static readonly List<Rigidbody> SingleAllowedScratch = new List<Rigidbody>(1);
        private static readonly List<Rigidbody2D> SingleAllowed2DScratch = new List<Rigidbody2D>(1);

        public static void BeginSingle(Rigidbody allowedDynamic)
        {
            SingleAllowedScratch.Clear();
            if (allowedDynamic != null)
                SingleAllowedScratch.Add(allowedDynamic);
            Begin(SingleAllowedScratch, null);
        }

        public static void BeginSingle(Rigidbody allowed3D, Rigidbody2D allowed2D)
        {
            SingleAllowedScratch.Clear();
            if (allowed3D != null)
                SingleAllowedScratch.Add(allowed3D);
            SingleAllowed2DScratch.Clear();
            if (allowed2D != null)
                SingleAllowed2DScratch.Add(allowed2D);
            Begin(SingleAllowedScratch, SingleAllowed2DScratch);
        }

        public static void Begin(IReadOnlyList<Rigidbody> allowedDynamic,
            IReadOnlyList<Rigidbody2D> allowed2D = null)
        {
            var allow = new HashSet<Rigidbody>();
            if (allowedDynamic != null)
            {
                for (var i = 0; i < allowedDynamic.Count; i++)
                {
                    var r = allowedDynamic[i];
                    if (r != null)
                        allow.Add(r);
                }
            }

            Frozen.Clear();
            WasKinematic.Clear();
            Frozen2D.Clear();
            WasBodyType2D.Clear();

            var all = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude);
            foreach (var rb in all)
            {
                if (rb == null || allow.Contains(rb))
                    continue;
                if (rb.isKinematic)
                    continue;

                WasKinematic.Add(rb.isKinematic);
                Frozen.Add(rb);
                rb.isKinematic = true;
            }

            var allow2d = new HashSet<Rigidbody2D>();
            if (allowed2D != null)
            {
                for (var i = 0; i < allowed2D.Count; i++)
                {
                    var r = allowed2D[i];
                    if (r != null)
                        allow2d.Add(r);
                }
            }

            var all2d = UnityEngine.Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Exclude);
            foreach (var rb2d in all2d)
            {
                if (rb2d == null || allow2d.Contains(rb2d))
                    continue;
                if (rb2d.bodyType != RigidbodyType2D.Dynamic)
                    continue;

                WasBodyType2D.Add(rb2d.bodyType);
                Frozen2D.Add(rb2d);
                rb2d.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        public static void End()
        {
            for (var i = 0; i < Frozen.Count; i++)
            {
                var rb = Frozen[i];
                if (rb != null)
                    rb.isKinematic = WasKinematic[i];
            }

            for (var i = 0; i < Frozen2D.Count; i++)
            {
                var rb2d = Frozen2D[i];
                if (rb2d != null)
                    rb2d.bodyType = WasBodyType2D[i];
            }

            Frozen.Clear();
            WasKinematic.Clear();
            Frozen2D.Clear();
            WasBodyType2D.Clear();
        }
    }
}
