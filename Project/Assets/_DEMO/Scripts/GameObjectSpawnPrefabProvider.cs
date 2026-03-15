using System.Collections.Generic;
using UnityEngine;

public class GameObjectSpawnPrefabProvider : MonoBehaviour, IGameObjectSpawnPrefabProvider
{
    [SerializeField] private List<GameObject> list = new();

    public void AddPrefab(GameObject prefab)
    {
        list.Add(prefab);
    }

    public void RemovePrefab(GameObject prefab)
    {
        list.Remove(prefab);
    }
    
    public GameObject GetSpawnPrefab()
    {
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }
}
