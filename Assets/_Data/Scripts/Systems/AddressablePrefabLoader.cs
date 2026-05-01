// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.ResourceManagement.AsyncOperations;

// public class AddressablePrefabLoader : Singleton<AddressablePrefabLoader>
// {
//     private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
//     private Dictionary<string, AsyncOperationHandle<GameObject>> handleCache = new Dictionary<string, AsyncOperationHandle<GameObject>>();

//     protected override void Awake()
//     {
//         MakeSingleton(false);

//         if (AddressablePrefabLoader.Ins != this)
//         {
//             enabled = false;
//             return;
//         }
//     }

//     public void LoadPrefab(string addressKey, Action<GameObject> onLoaded)
//     {
//         if (string.IsNullOrEmpty(addressKey))
//         {
//             Debug.LogError("Address key bị rỗng.");
//             onLoaded?.Invoke(null);
//             return;
//         }

//         if (prefabCache.TryGetValue(addressKey, out GameObject cachedPrefab))
//         {
//             onLoaded?.Invoke(cachedPrefab);
//             return;
//         }

//         Addressables.LoadAssetAsync<GameObject>(addressKey).Completed += handle =>
//         {
//             if (handle.Status == AsyncOperationStatus.Succeeded)
//             {
//                 GameObject prefab = handle.Result;

//                 prefabCache[addressKey] = prefab;
//                 handleCache[addressKey] = handle;

//                 onLoaded?.Invoke(prefab);
//             }
//             else
//             {
//                 Debug.LogError("Không load được Addressable Prefab: " + addressKey);
//                 onLoaded?.Invoke(null);
//             }
//         };
//     }

//     public void SpawnPrefab(string addressKey, Vector3 position, Quaternion rotation)
//     {
//         LoadPrefab(addressKey, prefab =>
//         {
//             if (prefab == null) return;

//             Instantiate(prefab, position, rotation);
//         });
//     }

//     public void SpawnPrefab(string addressKey, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> onSpawned = null)
//     {
//         LoadPrefab(addressKey, prefab =>
//         {
//             if (prefab == null)
//             {
//                 onSpawned?.Invoke(null);
//                 return;
//             }

//             GameObject obj = Instantiate(prefab, position, rotation, parent);
//             onSpawned?.Invoke(obj);
//         });
//     }

//     public void ReleaseAll()
//     {
//         foreach (var handle in handleCache.Values)
//         {
//             if (handle.IsValid())
//             {
//                 Addressables.Release(handle);
//             }
//         }

//         prefabCache.Clear();
//         handleCache.Clear();
//     }
// }