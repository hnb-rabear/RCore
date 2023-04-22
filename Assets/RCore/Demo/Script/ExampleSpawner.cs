using System.Collections;
using UnityEngine;
using RCore.Components;
using Random = UnityEngine.Random;

namespace RCore.Demo
{
    public class ExampleSpawner : MonoBehaviour
    {
        public Transform[] spawnPositions;
        public HorizontalAlignment alignment;

        private void OnEnable()
        {
            StartCoroutine(IEUpdate1());
            StartCoroutine(IEUpdate2());
        }

        private IEnumerator IEUpdate1()
        {
            var interval = new WaitForSeconds(0.1f);
            while (true)
            {
                Transform prefab = null;
                int random = Random.Range(0, 4);
                switch (random)
                {
                    case 0: prefab = ExamplePoolsManager.Instance.redCubePrefab; break;
                    case 1: prefab = ExamplePoolsManager.Instance.blueCubePrefab; break;
                    case 2: prefab = ExamplePoolsManager.Instance.greenCubePrefab; break;
                    case 3: prefab = ExamplePoolsManager.Instance.spherePrefab; break;
                }

                yield return interval;
                var position = spawnPositions[Random.Range(0, spawnPositions.Length)];
                ExamplePoolsManager.Instance.Spawn(prefab, position.position);
            }
        }

        private IEnumerator IEUpdate2()
        {
            var interval = new WaitForSeconds(1);
            while (true)
            {
                yield return interval;
                alignment.cellDistance = Random.Range(1f, 3f);
                alignment.AlignByTweener(null);
            }
        }
    }
}