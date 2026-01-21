using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore.Example
{
	public class ExampleSpawner : MonoBehaviour
	{
		public Transform[] spawnPositions;
		public HorizontalAlignment alignment;
		public Transform[] prefabs;
		public Transform[] prefabsBuiltin;

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
				yield return interval;
				if (Random.value > 0.5f)
				{
					Transform prefab;
					if (Random.value > 0.5f)
						prefab = prefabs[Random.Range(0, prefabs.Length)];
					else
						prefab = prefabsBuiltin[Random.Range(0, prefabsBuiltin.Length)];
					var position = spawnPositions[Random.Range(0, spawnPositions.Length)];
					ExamplePoolsManager.Instance.SpawnByPoolContainer(prefab, position.position);
				}
				else
				{
					var position = spawnPositions[Random.Range(0, spawnPositions.Length)];
					ExamplePoolsManager.Instance.SpawnByPool(position.position);
				}
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