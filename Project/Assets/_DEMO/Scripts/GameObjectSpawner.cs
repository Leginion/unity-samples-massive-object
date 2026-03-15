using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GameObjectSpawner : MonoBehaviour, IGameObjectSpawner
{
    [SerializeField] private GameObjectSpawnPrefabProvider _spawnPrefabProvider;
    [SerializeField] private int _wantSpawnCount = 10000;

    private void Start()
    {
        StartCoroutine(RequestBatchSpawn());
    }

    private IEnumerator RequestBatchSpawn()
    {
        int count = _wantSpawnCount;
        for (int i = 0; i < count; i++)
        {
            SpawnSingle();

            if (i == 1000)
            {
                yield return new WaitForNextFrameUnit();
            }
        }
    }
    
    public GameObject SpawnSingle()
    {
        GameObject prefab = _spawnPrefabProvider.GetSpawnPrefab();
        return Instantiate(prefab);
    }

    public GameObject[] SpawnMultiple(int spawnCount)
    {
        GameObject[] newObjects = new GameObject[spawnCount];
        
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = _spawnPrefabProvider.GetSpawnPrefab();
            newObjects[i] = Instantiate(prefab);
        }

        return newObjects;
    }
}
