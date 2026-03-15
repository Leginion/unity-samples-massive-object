using System.Collections;
using UnityEngine;

public class GameObjectSpawner : MonoBehaviour, IGameObjectSpawner
{
    [SerializeField] private GameObjectSpawnPrefabProvider _spawnPrefabProvider;
    [SerializeField] private int _wantSpawnCount = 10000;
    [SerializeField] private int _batchSpawnUnit = 50;
    [SerializeField] private float _batchSpawnInterval = 0.2f;
    [SerializeField] private int _alreadySpawnCount = 0;

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

            if (i % _batchSpawnUnit == 0)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
    
    public GameObject SpawnSingle()
    {
        GameObject prefab = _spawnPrefabProvider.GetSpawnPrefab();
        GameObject instance = Instantiate(prefab);
        _alreadySpawnCount++;
        return instance;
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
