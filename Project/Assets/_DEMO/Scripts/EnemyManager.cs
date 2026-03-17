using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private bool enableBurst = false;
    
    #region Singleton
    private static EnemyManager _Instance;
    public static EnemyManager Instance { get { return _Instance; } }
    #endregion
    
    #region Burst
    private NativeArray<Vector3> nativePositions;
    private NativeArray<Quaternion> nativeRotations;
    private NativeArray<Vector3> nativeScales;
    private NativeArray<Matrix4x4> nativeMatrices;
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

        if (enableBurst)
        {
            nativePositions = new NativeArray<Vector3>(count, Allocator.Persistent);
            nativeRotations = new NativeArray<Quaternion>(count, Allocator.Persistent);
            nativeScales   = scales != null ? new NativeArray<Vector3>(count, Allocator.Persistent) : default;
            nativeMatrices = new NativeArray<Matrix4x4>(count, Allocator.Persistent);
            
            // 初始拷贝数据（只一次）
            nativePositions.CopyFrom(positions);
            nativeRotations.CopyFrom(rotations);
            if (scales != null) nativeScales.CopyFrom(scales);
            
            // 保留，用于后续 SetData（如果 gpuRenderer 需要 managed 数组）
            tempMatrices = new Matrix4x4[count];
        }
        else
        {
            tempMatrices = new Matrix4x4[count];
        }
    }

    private void UpdateMatrices_Normal(float deltaTime)
    {
        int count = positions.Length;
        
        // update positions, rotations, scales
        Profiler.BeginSample("UpdatePositions");
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
        Profiler.EndSample();
        
        // structure Matrix4x4 array
        Profiler.BeginSample("BuildMatrices");
        for (int i = 0; i < count; i++)
        {
            Profiler.BeginSample("BuildMatrices - TRS Build");
            Matrix4x4 mat = Matrix4x4.TRS(
                positions[i],
                rotations[i],
                scales != null ? scales[i] : Vector3.one
            );
            Profiler.EndSample();
            
            Profiler.BeginSample("BuildMatrices - TRS Write");
            tempMatrices[i] = mat;
            Profiler.EndSample();
        }
        Profiler.EndSample();
        
        // send gpu
        Profiler.BeginSample("UpdateGPU");
        gpuRenderer.UpdateInstanceMatrices(tempMatrices, count);
        Profiler.EndSample();
    }

    private void UpdateMatrices_Burst(float deltaTime)
    {
        int count = positions.Length;

        Profiler.BeginSample("UpdatePositions");
        // 位置更新仍用普通循环（这个轻，可稍后 Job 化）
        for (int i = 0; i < count; i++)
        {
            Vector3 position = positions[i];
            position.y += raiseSpeeds[i] * deltaTime;
            if (position.y >= 200f)
            {
                position.y = 0f;
            }
            positions[i] = position;
        }
        Profiler.EndSample();

        Profiler.BeginSample("BuildMatrices");

        Profiler.BeginSample("CopyPositionsToNative");
        nativePositions.CopyFrom(positions);  // 拷贝更新后的位置（~几 ms）
        Profiler.EndSample();

        Profiler.BeginSample("BuildJob");
        var job = new BuildMatricesJob
        {
            Positions  = nativePositions,
            Rotations  = nativeRotations,
            Scales     = nativeScales,
            Matrices   = nativeMatrices
        };

        // Schedule 并行执行（batchSize 64 是经验值，可调 32~128 看性能）
        JobHandle handle = job.Schedule(count, 64);
        handle.Complete();  // 同步等待完成（简单起步，后面可异步）
        Profiler.EndSample();

        Profiler.BeginSample("CopyToTempMatrices");
        nativeMatrices.CopyTo(tempMatrices);  // 拷贝回 managed 数组供 gpuRenderer 用
        Profiler.EndSample();

        Profiler.EndSample();

        Profiler.BeginSample("UpdateGPU");
        gpuRenderer.UpdateInstanceMatrices(tempMatrices, count);
        Profiler.EndSample();
    }

    public void UpdateMatrices(float deltaTime)
    {
        if (enableBurst)
        {
            UpdateMatrices_Burst(deltaTime);
        }
        else
        {
            UpdateMatrices_Normal(deltaTime);
        }
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

    private void OnDisable()
    {
        if (enableBurst)
        {
            if (nativePositions.IsCreated) nativePositions.Dispose();
            if (nativeRotations.IsCreated) nativeRotations.Dispose();
            if (nativeScales.IsCreated) nativeScales.Dispose();
            if (nativeMatrices.IsCreated) nativeMatrices.Dispose();
        }
    }
}
