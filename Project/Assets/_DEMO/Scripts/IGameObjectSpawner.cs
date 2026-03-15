using UnityEngine;

public interface IGameObjectSpawner
{
    GameObject SpawnSingle();
    GameObject[] SpawnMultiple(int spawnCount);
}
