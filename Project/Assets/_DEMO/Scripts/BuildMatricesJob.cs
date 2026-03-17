using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile(CompileSynchronously = true)]  // CompileSynchronously 第一次 Schedule 时就编译，避免卡顿
public struct BuildMatricesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> Positions;
    [ReadOnly] public NativeArray<Quaternion> Rotations;
    [ReadOnly] public NativeArray<Vector3> Scales;  // 如果 scales 可为 null，需要处理

    public NativeArray<Matrix4x4> Matrices;  // 输出

    public void Execute(int index)
    {
        Vector3 pos = Positions[index];
        Quaternion rot = Rotations[index];
        Vector3 scale = Scales.IsCreated ? Scales[index] : Vector3.one;  // 处理 null scales

        Matrices[index] = Matrix4x4.TRS(pos, rot, scale);
    }
}