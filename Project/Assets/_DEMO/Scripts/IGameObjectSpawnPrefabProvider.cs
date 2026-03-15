using UnityEngine;

public interface IGameObjectSpawnPrefabProvider
{
    GameObject GetSpawnPrefab();
    void AddPrefab(GameObject prefab);
    void RemovePrefab(GameObject prefab);
}