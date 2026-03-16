using System.Collections;
using UnityEngine;

public class GPUTestBootstraper : MonoBehaviour
{
    [SerializeField] private float _spawnInterval = 0.2f;
    [SerializeField] private int _spawnCount = 5000;
    [SerializeField] private int _perBatch = 1000;
    [SerializeField] private int _aliveCount = 0;

    private bool started;
    
    private void Start()
    {
        started = false;
        
        EnemyManager.Instance.positions = new Vector3[_spawnCount];
        EnemyManager.Instance.rotations = new Quaternion[_spawnCount];
        EnemyManager.Instance.scales = new Vector3[_spawnCount];
        EnemyManager.Instance.raiseSpeeds = new float[_spawnCount];
        
        StartCoroutine(SpawnTask());
    }

    private void Update()
    {
        if (!started) return;
        
        EnemyManager.Instance.UpdateMatrices(Time.deltaTime);
    }

    private IEnumerator SpawnTask()
    {
        while (_aliveCount < _spawnCount)
        {
            _aliveCount++;
            SpawnSingle();

            if (_aliveCount % _perBatch == 0)
            {
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        GPUStart();
    }

    private void GPUStart()
    {
        started = true;
        EnemyManager.Instance.InitMatrices();
    }

    private void SpawnSingle()
    {
        Vector3 p1;
        Quaternion p2;
        Vector3 p3;
        
        int i = _aliveCount - 1;

        p1.x = Random.Range(-100f, 100f);
        p1.y = Random.Range(1f, 500f);
        p1.z = Random.Range(-100f, 100f);
        
        p2 = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        
        p3.x = Random.Range(0.2f, 0.8f);
        p3.y = Random.Range(0.2f, 0.8f);
        p3.z = Random.Range(0.2f, 0.8f);

        EnemyManager.Instance.positions[i] = p1;
        EnemyManager.Instance.rotations[i] = p2;
        EnemyManager.Instance.scales[i] = p3;
        EnemyManager.Instance.raiseSpeeds[i] = Random.Range(1f, 5f);
    }
}
