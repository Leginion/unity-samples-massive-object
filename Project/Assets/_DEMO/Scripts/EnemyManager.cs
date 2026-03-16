using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    #region Singleton
    private static EnemyManager _Instance;
    public static EnemyManager Instance { get { return _Instance; } }
    #endregion
    
    #region GPU
    
    public Vector3[] positions;
    public Quaternion[] rotations;
    public Vector3[] scales;
    public float[] raiseSpeeds;

    public GPUInstancedRenderer gpuRenderer;
    
    private Matrix4x4[] tempMatrices;

    public void InitMatrices()
    {
        int count = positions.Length;
        tempMatrices = new Matrix4x4[count];
    }

    public void UpdateMatrices(float deltaTime)
    {
        int count = positions.Length;
        
        // update positions, rotations, scales
        for (int i = 0; i < count; i++)
        {
            Vector3 position = positions[i];
            float raiseSpeed = raiseSpeeds[i];
            position.y += raiseSpeed * deltaTime;
            if (position.y >= 200f)
            {
                position.y = 0f;
            }
            positions[i] = position;
        }
        
        // structure Matrix4x4 array
        for (int i = 0; i < count; i++)
        {
            tempMatrices[i] = Matrix4x4.TRS(
                positions[i],
                rotations[i],
                scales != null ? scales[i] : Vector3.one
            );
        }
        
        // send gpu
        gpuRenderer.UpdateInstanceMatrices(tempMatrices, count);
    }

    #endregion
    
    
    
    
    
    
    
    private static readonly List<Transform> _transforms = new();

    public static void Add(Transform t)
    {
        _transforms.Add(t);
    }

    public static void Remove(Transform t)
    {
        _transforms.Remove(t);
    }

    public static void NotifyUpdate(float deltaTime)
    {
        Vector3 deltaMove = Vector3.forward * deltaTime;
        foreach (Transform t in _transforms)
        {
            SimpleEnemyMovementNoMono.Move(t, deltaMove);
        }
    }

    private void Awake()
    {
        _Instance = this;
    }

    private void Update()
    {
        NotifyUpdate(Time.deltaTime);
    }
}
