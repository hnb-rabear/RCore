//#define ADDRESSABLES

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RCore.Common;

namespace RCore.Demo
{
    public class ExampleAddressables : MonoBehaviour
    {
        public string label;
        public List<AssetReference> prefabReferences;
        public List<GameObject> objects;
        public List<GameObject> prefabs;

        private PoolsContainer<Transform> poolContainer;

#if ADDRESSABLES
        private IEnumerator Start()
        {
            yield return StartCoroutine(IELoadPrefabReferences());
            yield return StartCoroutine(IEInstantitePrefabReferences());
            yield return StartCoroutine(IEPoolPrefabReferences());
            yield return StartCoroutine(IELoadAndInstantiteLabel());
        }

        private IEnumerator IELoadPrefabReferences()
        {
            var task = AddressableUtil.WaitLoadAssetsAsync<GameObject>(prefabReferences);
            yield return new WaitUntil(() => task.IsCompleted);
            prefabs = task.Result.GetObjects();
        }

        ///Example of instantite gameobject from references
        private IEnumerator IEInstantitePrefabReferences()
        {
            var task = AddressableUtil.WaitInstantiateAsync(prefabReferences);
            yield return new WaitUntil(() => task.IsCompleted);
            objects = task.Result.GetObjects();
            for (int i = 0; i < objects.Count; i++)
                objects[i].transform.position = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));
        }

        ///Example of loading prefabs and pushing them into pools
        private IEnumerator IEPoolPrefabReferences()
        {
            poolContainer = new PoolsContainer<Transform>(transform);

            var task = AddressableUtil.WaitLoadPrefabsAsync<Transform>(prefabReferences);
            yield return new WaitUntil(() => task.IsCompleted);
            var prefabs = task.Result.GetPrefabs();
            float time = 3;
            while (time > 0)
            {
                var prefab = prefabs[Random.Range(0, prefabs.Count)];
                var position = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));
                poolContainer.Spawn(prefab, position);
                yield return new WaitForSeconds(0.25f);
                time -= 0.25f;
            }
        }

        private IEnumerator IELoadAndInstantiteLabel()
        {
            if (string.IsNullOrEmpty(label))
                yield break;

            var task = AddressableUtil.WaitLoadResouceLocationAsync(label);
            yield return new WaitUntil(() => task.IsCompleted);
            var localtions = task.Result;
            foreach (var location in localtions)
            {
                var task2 = AddressableUtil.WaitInstantiateAsync(location.ToString());
                yield return new WaitUntil(() => task.IsCompleted);
                var obj = task2.Result;
            }
        }
#endif
    }
}