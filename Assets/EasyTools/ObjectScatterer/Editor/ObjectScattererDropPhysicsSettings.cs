using System.Collections.Generic;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Tightens editor drop simulation to reduce tunneling through thin static colliders (sweep CCD + smaller dt + solver iterations).
    /// </summary>
    internal static class ObjectScattererDropPhysicsSettings
    {
        private const int MinSolverIterations = 16;
        private const int MinSolverVelocityIterations = 4;
        /// <summary>Upper bound for Physics.Simulate step length (seconds). Smaller = less discrete tunneling.</summary>
        private const float MaxSimulateDeltaTime = 0.008333333f; // ~120 Hz

        private static int _solverBoostRefCount;
        private static int _savedSolverIterations;
        private static int _savedSolverVelocityIterations;

        private static void PushSolverBoost()
        {
            if (_solverBoostRefCount == 0)
            {
                _savedSolverIterations = Physics.defaultSolverIterations;
                _savedSolverVelocityIterations = Physics.defaultSolverVelocityIterations;
                Physics.defaultSolverIterations = Mathf.Max(_savedSolverIterations, MinSolverIterations);
                Physics.defaultSolverVelocityIterations =
                    Mathf.Max(_savedSolverVelocityIterations, MinSolverVelocityIterations);
            }

            _solverBoostRefCount++;
        }

        private static void PopSolverBoost()
        {
            if (_solverBoostRefCount <= 0)
                return;
            _solverBoostRefCount--;
            if (_solverBoostRefCount != 0)
                return;
            Physics.defaultSolverIterations = _savedSolverIterations;
            Physics.defaultSolverVelocityIterations = _savedSolverVelocityIterations;
        }

        /// <summary>
        /// Single fixed timestep for all drop <see cref="Physics.Simulate"/> calls (smaller than project default when possible).
        /// </summary>
        public static float GetDropSimulationFixedDeltaTime()
        {
            var raw = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            return Mathf.Min(raw, MaxSimulateDeltaTime);
        }

        public static void BeginSolverBoostForSession()
        {
            PushSolverBoost();
        }

        public static void EndSolverBoostForSession()
        {
            PopSolverBoost();
        }

        /// <summary>
        /// Returns true if any rigidbody was tuned (caller saves/restores modes in parallel arrays).
        /// </summary>
        public static bool BeginPerBodyContinuousAgainstStatic(IReadOnlyList<ObjectScattererPhysicsPlayback.RigidbodyDropInfo> bodies,
            CollisionDetectionMode[] outSavedModes)
        {
            if (bodies == null || outSavedModes == null)
                return false;
            var any = false;
            for (var i = 0; i < bodies.Count; i++)
            {
                var rb = bodies[i].Rb;
                if (rb == null)
                    continue;
                outSavedModes[i] = rb.collisionDetectionMode;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                any = true;
            }

            return any;
        }

        public static void RestorePerBodyCollisionModes(IReadOnlyList<ObjectScattererPhysicsPlayback.RigidbodyDropInfo> bodies,
            CollisionDetectionMode[] savedModes)
        {
            if (bodies == null || savedModes == null)
                return;
            for (var i = 0; i < bodies.Count && i < savedModes.Length; i++)
            {
                var rb = bodies[i].Rb;
                if (rb == null)
                    continue;
                rb.collisionDetectionMode = savedModes[i];
            }
        }
    }
}
