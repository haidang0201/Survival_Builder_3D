using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace TopsonGames
{
    public class AnimatorLinkLODManager : MonoBehaviour
    {
        public static AnimatorLinkLODManager instance;

        [Tooltip("The distance at which the animators are switched.")]
        public float cullDistance = 100f;

        private Camera mainCamera;
        private bool isSetup = false;

        private List<AnimatorLink> activeLinks = new List<AnimatorLink>();
        private HashSet<AnimatorLink> pendingAdds = new HashSet<AnimatorLink>();
        private HashSet<AnimatorLink> pendingRemoves = new HashSet<AnimatorLink>();

        private HashSet<AnimatorLink> forceUpdateLinks = new HashSet<AnimatorLink>();

        private TransformAccessArray transformAccessArray;
        private NativeArray<bool> results;
        private JobHandle jobHandle;
        private bool isJobScheduled = false;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }

        IEnumerator Start()
        {
            mainCamera = Camera.main;
            yield return new WaitForEndOfFrame();
            isSetup = true;
        }

        public void Register(AnimatorLink link)
        {
            pendingRemoves.Remove(link);
            pendingAdds.Add(link);
        }

        public void Unregister(AnimatorLink link)
        {
            pendingAdds.Remove(link);
            pendingRemoves.Add(link);
        }

        private void ReallocateArrays(int capacity)
        {
            if (transformAccessArray.isCreated) transformAccessArray.Dispose();
            if (results.IsCreated) results.Dispose();

            if (capacity <= 0) return;

            Transform[] transforms = new Transform[capacity];
            for (int i = 0; i < capacity; i++) transforms[i] = activeLinks[i].transform;

            transformAccessArray = new TransformAccessArray(transforms);
            results = new NativeArray<bool>(capacity, Allocator.Persistent);
        }

        void Update()
        {
            if (!isSetup || mainCamera == null) return;

            if (isJobScheduled) jobHandle.Complete();

            bool linksChanged = false;

            if (pendingRemoves.Count > 0)
            {
                foreach (var link in pendingRemoves) activeLinks.Remove(link);
                pendingRemoves.Clear();
                linksChanged = true;
            }

            if (pendingAdds.Count > 0)
            {
                foreach (var link in pendingAdds)
                {
                    if (!activeLinks.Contains(link))
                    {
                        activeLinks.Add(link);
                        forceUpdateLinks.Add(link); 
                    }
                }
                pendingAdds.Clear();
                linksChanged = true;
            }

            if (linksChanged || (activeLinks.Count > 0 && !transformAccessArray.isCreated))
            {
                ReallocateArrays(activeLinks.Count);
            }

            if (activeLinks.Count == 0 || !transformAccessArray.isCreated)
            {
                isJobScheduled = false;
                return;
            }

            var job = new AnimatorDistanceJob
            {
                CameraPosition = mainCamera.transform.position,
                CullDistanceSqr = cullDistance * cullDistance,
                Results = results
            };

            jobHandle = job.Schedule(transformAccessArray);
            isJobScheduled = true;
        }

        void LateUpdate()
        {
            if (!isJobScheduled) return;

            jobHandle.Complete();
            isJobScheduled = false;

            for (int i = 0; i < activeLinks.Count; i++)
            {
                var link = activeLinks[i];
                if (link == null) continue;

                bool shouldBeAnimator = results[i];
                bool needsForceUpdate = forceUpdateLinks.Contains(link);

                if (needsForceUpdate || link.IsAnimator != shouldBeAnimator)
                {
                    if (shouldBeAnimator)
                    {
                        link.EnableAnimator();
                    }
                    else
                    {
                        link.DisableAnimator();
                    }
                }
            }
            forceUpdateLinks.Clear();
        }

        private void OnDestroy()
        {
            if (isJobScheduled) jobHandle.Complete();
            if (transformAccessArray.isCreated) transformAccessArray.Dispose();
            if (results.IsCreated) results.Dispose();
        }
    }
}