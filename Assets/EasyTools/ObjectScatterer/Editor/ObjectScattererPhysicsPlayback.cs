using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyTools.ObjectScatterer
{
    /// <summary>
    /// Spreads Physics.Simulate across editor frames so drops are visible (Edit mode), unlike one-frame instant solve.
    /// Supports multiple rigidbodies under one spawned root (children participate).
    /// </summary>
    internal static class ObjectScattererPhysicsPlayback
    {
        public struct RigidbodyDropInfo
        {
            public Rigidbody Rb;
            public bool AddedTemp;
        }

        private sealed class Job
        {
            public GameObject Go;
            public RigidbodyDropInfo[] Bodies;
            public ObjectScattererTempPhysicsFromMeshes.TempColliderInfo[] TempColliders;
            public CollisionDetectionMode[] SavedCollisionModes;
            public int StepsRemaining;
            public float Dt;
            public int StepsPerFrame;
        }

        private static readonly List<Job> Jobs = new List<Job>();
        private static readonly List<Rigidbody> AllowedForIsolation = new List<Rigidbody>(32);
        private static bool _updateHooked;
        private static SimulationMode _savedSimulationMode = SimulationMode.FixedUpdate;

        public static void Enqueue(GameObject go, IReadOnlyList<RigidbodyDropInfo> bodies,
            IReadOnlyList<ObjectScattererTempPhysicsFromMeshes.TempColliderInfo> tempColliders,
            float maxSimulateSeconds, int stepsPerEditorFrame)
        {
            if (bodies == null || bodies.Count == 0)
                return;

            var dt = ObjectScattererDropPhysicsSettings.GetDropSimulationFixedDeltaTime();
            var maxSteps = Mathf.Max(1, Mathf.CeilToInt(maxSimulateSeconds / dt));
            var arr = new RigidbodyDropInfo[bodies.Count];
            for (var i = 0; i < bodies.Count; i++)
                arr[i] = bodies[i];

            ObjectScattererTempPhysicsFromMeshes.TempColliderInfo[] tempArr = null;
            if (tempColliders != null && tempColliders.Count > 0)
            {
                tempArr = new ObjectScattererTempPhysicsFromMeshes.TempColliderInfo[tempColliders.Count];
                for (var i = 0; i < tempColliders.Count; i++)
                    tempArr[i] = tempColliders[i];
            }

            var savedModes = new CollisionDetectionMode[bodies.Count];
            if (Jobs.Count == 0)
            {
                _savedSimulationMode = Physics.simulationMode;
                Physics.simulationMode = SimulationMode.Script;
                ObjectScattererDropPhysicsSettings.BeginSolverBoostForSession();
            }

            ObjectScattererDropPhysicsSettings.BeginPerBodyContinuousAgainstStatic(bodies, savedModes);

            var job = new Job
            {
                Go = go,
                Bodies = arr,
                TempColliders = tempArr,
                SavedCollisionModes = savedModes,
                StepsRemaining = maxSteps,
                Dt = dt,
                StepsPerFrame = Mathf.Max(1, stepsPerEditorFrame)
            };

            Jobs.Add(job);
            EnsureUpdateHook();
        }

        private static void EnsureUpdateHook()
        {
            if (_updateHooked)
                return;
            EditorApplication.update += Tick;
            _updateHooked = true;
        }

        /// <summary>
        /// Steps/Frame must apply immediately while playback runs; the job snapshot would otherwise stay stale.
        /// </summary>
        private static int ResolveLivePhysicsStepsPerEditorFrame(int jobSnapshot)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<ObjectScattererWindow>())
            {
                if (w != null)
                    return w.GetPhysicsStepsPerEditorFrame();
            }

            return Mathf.Max(1, jobSnapshot);
        }

        private static void Tick()
        {
            if (Jobs.Count == 0)
            {
                StopUpdateHook();
                return;
            }

            var job = Jobs[0];
            if (job.Go == null || job.Bodies == null || job.Bodies.Length == 0)
            {
                if (job.SavedCollisionModes != null && job.Bodies != null)
                    ObjectScattererDropPhysicsSettings.RestorePerBodyCollisionModes(job.Bodies, job.SavedCollisionModes);
                Jobs.RemoveAt(0);
                RestoreAutoSimulationIfIdle();
                return;
            }

            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            var liveStepsPerFrame = ResolveLivePhysicsStepsPerEditorFrame(job.StepsPerFrame);
            var stepsThisFrame = Mathf.Min(liveStepsPerFrame, job.StepsRemaining);

            AllowedForIsolation.Clear();
            for (var j = 0; j < Jobs.Count; j++)
            {
                var bodies = Jobs[j].Bodies;
                if (bodies == null)
                    continue;
                for (var k = 0; k < bodies.Length; k++)
                {
                    var r = bodies[k].Rb;
                    if (r != null)
                        AllowedForIsolation.Add(r);
                }
            }

            ObjectScattererPhysicsIsolation.Begin(AllowedForIsolation);
            try
            {
                for (var i = 0; i < stepsThisFrame; i++)
                {
                    Physics.Simulate(job.Dt);
                    job.StepsRemaining--;
                    if (AllBodiesSleeping(job))
                    {
                        job.StepsRemaining = 0;
                        break;
                    }

                    if (job.StepsRemaining <= 0)
                        break;
                }
            }
            finally
            {
                ObjectScattererPhysicsIsolation.End();
            }

            SceneView.RepaintAll();

            if (job.StepsRemaining <= 0 || AllBodiesSleeping(job))
            {
                FinalizeJob(job);
                Jobs.RemoveAt(0);
                RestoreAutoSimulationIfIdle();
            }
        }

        private static bool AllBodiesSleeping(Job job)
        {
            var bodies = job.Bodies;
            if (bodies == null)
                return true;
            for (var i = 0; i < bodies.Length; i++)
            {
                var rb = bodies[i].Rb;
                if (rb == null)
                    continue;
                if (!rb.IsSleeping())
                    return false;
            }

            return true;
        }

        private static void FinalizeJob(Job job)
        {
            if (job.SavedCollisionModes != null && job.Bodies != null)
                ObjectScattererDropPhysicsSettings.RestorePerBodyCollisionModes(job.Bodies, job.SavedCollisionModes);

            if (job.Go == null || job.Bodies == null)
                return;

            if (job.TempColliders != null)
            {
                for (var i = 0; i < job.TempColliders.Length; i++)
                {
                    var tc = job.TempColliders[i];
                    if (tc.Collider != null)
                        ObjectScattererTempPhysicsFromMeshes.DestroyColliderImmediateSafe(tc.Collider);
                    if (tc.OwnedMesh != null)
                        UnityEngine.Object.DestroyImmediate(tc.OwnedMesh);
                }
            }

            foreach (var b in job.Bodies)
            {
                if (b.Rb == null)
                    continue;
                Undo.RecordObject(b.Rb.transform, "Scatter settle");
                if (b.AddedTemp)
                {
                    ObjectScattererTempPhysicsFromMeshes.DestroyComponentImmediateSafe(b.Rb);
                }
                else
                {
                    b.Rb.isKinematic = true;
                    Undo.RecordObject(b.Rb, "Scatter settle");
                }
            }
        }

        private static void RestoreAutoSimulationIfIdle()
        {
            if (Jobs.Count > 0)
                return;
            Physics.simulationMode = _savedSimulationMode;
            ObjectScattererDropPhysicsSettings.EndSolverBoostForSession();
        }

        private static void StopUpdateHook()
        {
            if (!_updateHooked)
                return;
            EditorApplication.update -= Tick;
            _updateHooked = false;
            Physics.simulationMode = _savedSimulationMode;
        }
    }
}
