using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// World-space volume sampling and helpers.
    /// </summary>
    internal static class ObjectScattererPlacement
    {
        public static Vector3 SampleUniformInSphere(Vector3 center, float radius, System.Random r)
        {
            var u = (float)r.NextDouble();
            var v = (float)r.NextDouble();
            var w = (float)r.NextDouble();
            var theta = u * 2f * Mathf.PI;
            var phi = Mathf.Acos(2f * v - 1f);
            var rad = radius * Mathf.Pow(w, 1f / 3f);
            var sinp = Mathf.Sin(phi);
            return center + rad * new Vector3(sinp * Mathf.Cos(theta), sinp * Mathf.Sin(theta), Mathf.Cos(phi));
        }

        public static Vector3 SampleUniformInBox(Vector3 center, Vector3 halfExtents, System.Random rnd)
        {
            return center + new Vector3(
                Mathf.Lerp(-halfExtents.x, halfExtents.x, (float)rnd.NextDouble()),
                Mathf.Lerp(-halfExtents.y, halfExtents.y, (float)rnd.NextDouble()),
                Mathf.Lerp(-halfExtents.z, halfExtents.z, (float)rnd.NextDouble()));
        }

        public static bool IsPointInSphere(Vector3 center, float radius, Vector3 p)
        {
            return (p - center).sqrMagnitude <= radius * radius;
        }

        public static bool IsPointInAxisAlignedBox(Vector3 center, Vector3 halfExtents, Vector3 p)
        {
            var l = p - center;
            return Mathf.Abs(l.x) <= halfExtents.x && Mathf.Abs(l.y) <= halfExtents.y && Mathf.Abs(l.z) <= halfExtents.z;
        }

        public static Vector3 RandomUnitVector(System.Random rnd)
        {
            var u = (float)rnd.NextDouble() * 2f * Mathf.PI;
            var v = (float)rnd.NextDouble() * 2f - 1f;
            var w = Mathf.Sqrt(Mathf.Max(0f, 1f - v * v));
            return new Vector3(w * Mathf.Cos(u), w * Mathf.Sin(u), v);
        }

        public static Vector3 SampleUniformOnSphereSurface(Vector3 center, float radius, System.Random rnd)
        {
            var u = (float)rnd.NextDouble() * 2f * Mathf.PI;
            var v = (float)rnd.NextDouble() * 2f - 1f;
            var w = Mathf.Sqrt(1f - v * v);
            return center + radius * new Vector3(w * Mathf.Cos(u), w * Mathf.Sin(u), v);
        }

        public static Vector3 SampleUniformOnBoxSurface(Vector3 center, Vector3 halfExtents, System.Random rnd)
        {
            var face = rnd.Next(0, 6);
            float x, y, z;
            switch (face)
            {
                case 0:
                    x = halfExtents.x;
                    y = Mathf.Lerp(-halfExtents.y, halfExtents.y, (float)rnd.NextDouble());
                    z = Mathf.Lerp(-halfExtents.z, halfExtents.z, (float)rnd.NextDouble());
                    break;
                case 1:
                    x = -halfExtents.x;
                    y = Mathf.Lerp(-halfExtents.y, halfExtents.y, (float)rnd.NextDouble());
                    z = Mathf.Lerp(-halfExtents.z, halfExtents.z, (float)rnd.NextDouble());
                    break;
                case 2:
                    x = Mathf.Lerp(-halfExtents.x, halfExtents.x, (float)rnd.NextDouble());
                    y = halfExtents.y;
                    z = Mathf.Lerp(-halfExtents.z, halfExtents.z, (float)rnd.NextDouble());
                    break;
                case 3:
                    x = Mathf.Lerp(-halfExtents.x, halfExtents.x, (float)rnd.NextDouble());
                    y = -halfExtents.y;
                    z = Mathf.Lerp(-halfExtents.z, halfExtents.z, (float)rnd.NextDouble());
                    break;
                case 4:
                    x = Mathf.Lerp(-halfExtents.x, halfExtents.x, (float)rnd.NextDouble());
                    y = Mathf.Lerp(-halfExtents.y, halfExtents.y, (float)rnd.NextDouble());
                    z = halfExtents.z;
                    break;
                default:
                    x = Mathf.Lerp(-halfExtents.x, halfExtents.x, (float)rnd.NextDouble());
                    y = Mathf.Lerp(-halfExtents.y, halfExtents.y, (float)rnd.NextDouble());
                    z = -halfExtents.z;
                    break;
            }

            return center + new Vector3(x, y, z);
        }

        /// <summary>Direction from center toward a random point on the sphere shell.</summary>
        public static Vector3 InnerRayDirectionSphere(Vector3 center, float radius, System.Random rnd)
        {
            var p = SampleUniformOnSphereSurface(center, radius, rnd);
            return (p - center).normalized;
        }

        /// <summary>Direction from center toward a random point on the box surface.</summary>
        public static Vector3 InnerRayDirectionBox(Vector3 center, Vector3 halfExtents, System.Random rnd)
        {
            var p = SampleUniformOnBoxSurface(center, halfExtents, rnd);
            return (p - center).normalized;
        }
    }
}
