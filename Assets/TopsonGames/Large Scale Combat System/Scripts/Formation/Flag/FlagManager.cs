using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace TopsonGames
{
    public class FlagManager : MonoBehaviour
    {
        public static FlagManager Instance;

        private List<FlagController> activeFlags = new List<FlagController>(50);
        private Camera mainCamera;

        void Awake()
        {
            if (Instance != null)
                Destroy(this);
            else 
                Instance = this;
        }

        void Start()
        {
            mainCamera = Camera.main;
        }

        public void RegisterFlag(FlagController flag)
        {
            if (!activeFlags.Contains(flag))
                activeFlags.Add(flag);
        }

        public void UnregisterFlag(FlagController flag)
        {
            if(activeFlags.Contains(flag))
                activeFlags.Remove(flag);
        }

        void LateUpdate()
        {
            if (activeFlags.Count == 0 || mainCamera == null) return;

            int flagCount = activeFlags.Count;
            var flagPositions = new NativeArray<Vector3>(flagCount, Allocator.TempJob);
            var outputRotations = new NativeArray<Quaternion>(flagCount, Allocator.TempJob);

            for (int i = 0; i < flagCount; i++)
            {
                flagPositions[i] = activeFlags[i].transform.position;
            }
            var job = new BillboardJob
            {
                FlagPositions = flagPositions,
                CameraPosition = mainCamera.transform.position,
                OutputRotations = outputRotations
            };
            JobHandle handle = job.Schedule(flagCount, 32);
            handle.Complete();

            for (int i = 0; i < flagCount; i++)
            {
                if (activeFlags[i] != null) 
                {
                    activeFlags[i].transform.rotation = outputRotations[i];
                }
            }
            flagPositions.Dispose();
            outputRotations.Dispose();
        }
    }
}