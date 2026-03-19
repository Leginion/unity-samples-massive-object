using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class HiZGenerator : MonoBehaviour
{
    public Shader depthCopyShader;
    public Shader maxDownsampleShader;   // ← 新增拖入上面新建的 MaxDownsample

    [SerializeField] private RenderTexture hiZTexture;
    private CommandBuffer cmd;
    private Camera cam;
    private Material depthCopyMat;
    private Material maxMat;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;

        depthCopyMat = new Material(depthCopyShader);
        maxMat = new Material(maxDownsampleShader);

        int w = cam.pixelWidth;
        int h = cam.pixelHeight;

        hiZTexture = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        hiZTexture.useMipMap = true;
        hiZTexture.autoGenerateMips = false;
        hiZTexture.filterMode = FilterMode.Point;
        hiZTexture.wrapMode = TextureWrapMode.Clamp;
        hiZTexture.Create();

        cmd = new CommandBuffer { name = "Hi-Z Pyramid Generation" };
        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cmd);
    }

    private void LateUpdate()
    {
        if (cmd == null) return;
        cmd.Clear();

        int mipCount = hiZTexture.mipmapCount;

        // 第 0 层：拷贝线性深度
        cmd.Blit(null, hiZTexture, depthCopyMat);

        // 逐层 max downsample（Blit 方案）
        for (int mip = 1; mip < mipCount; mip++)
        {
            cmd.SetRenderTarget(hiZTexture, mip);           // 切换到当前 mip
            cmd.Blit(hiZTexture, hiZTexture, maxMat);       // 从上一层 Blit
        }
    }

    private void OnGUI()
    {
        if (hiZTexture != null)
            GUI.DrawTexture(new Rect(10, 10, 512, 512), hiZTexture, ScaleMode.ScaleToFit);
    }

    private void OnDisable()
    {
        if (cmd != null) cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, cmd);
        if (hiZTexture != null) hiZTexture.Release();
        if (depthCopyMat != null) Destroy(depthCopyMat);
        if (maxMat != null) Destroy(maxMat);
    }
}