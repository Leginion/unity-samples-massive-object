using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GPUInstancedRenderer : MonoBehaviour
{
    [Header("=== 必须设置 ===")]
    public Mesh instanceMesh;           // Capsule 的 Mesh（从 MeshFilter 拖或手动赋）
    public Material instanceMaterial;   // 使用你修复好的 Indirect Unlit Shader 的 Material

    [Header("=== 运行时数据 ===")]
    public int instanceCount = 100000;  // 目标数量，先从小开始测试
    private Matrix4x4[] matrices;       // CPU 侧临时数组（每帧更新）

    private ComputeBuffer matrixBuffer; // GPU Buffer：每个实例的 Matrix4x4
    private ComputeBuffer argsBuffer;   // Indirect Args Buffer

    private uint[] args = new uint[5];  // indexCountPerInstance, instanceCount, startIndex, baseVertex, startInstance

    private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10000f); // 大包围盒，防止 cull

    void Awake()
    {
        if (!instanceMesh || !instanceMaterial)
        {
            Debug.LogError("Missing mesh or material!");
            enabled = false;
            return;
        }

        // 初始化临时数组
        matrices = new Matrix4x4[instanceCount];

        // 初始化 args Buffer
        args[0] = (uint)instanceMesh.GetIndexCount(0);     // index count per instance
        args[1] = (uint)instanceCount;                      // instance count (稍后更新)
        args[2] = (uint)instanceMesh.GetBaseVertex(0);     // base vertex
        args[3] = 0;                                        // start index
        args[4] = 0;                                        // start instance

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // 初始化 matrix Buffer
        matrixBuffer = new ComputeBuffer(instanceCount, 64); // Matrix4x4 = 64 bytes
    }

    /// <summary>
    /// 每帧由 EnemyManager 调用，传入所有实例的矩阵
    /// </summary>
    public void UpdateInstanceMatrices(Matrix4x4[] newMatrices, int count)
    {
        if (count <= 0 || count > instanceCount) return;

        // 更新 CPU 数组（可选，如果你想在 CPU 侧缓存）
        // System.Array.Copy(newMatrices, matrices, count);

        // 直接上传到 GPU
        matrixBuffer.SetData(newMatrices, 0, 0, count);

        // 更新 instance count
        args[1] = (uint)count;
        argsBuffer.SetData(args);
    }

    void Update()
    {
        if (!instanceMaterial || !instanceMesh || matrixBuffer == null) return;

        // 绑定 Buffer 到 Shader
        instanceMaterial.SetBuffer("instanceMatrices", matrixBuffer);

        // 执行 Indirect 绘制
        Graphics.DrawMeshInstancedIndirect(
            instanceMesh,
            0,                              // submesh index
            instanceMaterial,
            bounds,                         // 包围盒
            argsBuffer,
            0,                              // args offset
            null,                           // property block (可选)
            ShadowCastingMode.Off,          // 先关闭阴影，测试快
            false,                          // receive shadows
            gameObject.layer                // layer
        );
    }

    void OnDestroy()
    {
        matrixBuffer?.Release();
        argsBuffer?.Release();
    }

    // 可选：调试用，显示当前实例数量
    void OnGUI()
    {
        GUILayout.Label($"Instanced Objects: {args[1]}");
    }
}