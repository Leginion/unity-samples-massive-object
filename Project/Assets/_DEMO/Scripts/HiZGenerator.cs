using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class HiZGenerator : MonoBehaviour
{
    public ComputeShader hiZCompute;          // 拖入上面的 Compute Shader
    private RenderTexture hiZTexture;
    private CommandBuffer cmd;
    private Camera cam;
    private int kernel;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;   // 关键：开启 Depth Texture

        int w = cam.pixelWidth;
        int h = cam.pixelHeight;

        // 创建带 mipmap 的 Hi-Z RenderTexture
        hiZTexture = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        hiZTexture.useMipMap = true;
        hiZTexture.autoGenerateMips = false;
        hiZTexture.enableRandomWrite = true;
        hiZTexture.filterMode = FilterMode.Point;
        hiZTexture.wrapMode = TextureWrapMode.Clamp;
        hiZTexture.Create();

        kernel = hiZCompute.FindKernel("DownsampleMax");

        // CommandBuffer（Built-in 专用）
        cmd = new CommandBuffer { name = "Hi-Z Pyramid Generation" };
        cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, cmd);
    }

    private void OnDisable()
    {
        if (cmd != null) cam.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cmd);
        if (hiZTexture != null) hiZTexture.Release();
    }

    private void OnPreRender()
    {
        cmd.Clear();

        int mipCount = hiZTexture.mipmapCount;
        int width = hiZTexture.width;
        int height = hiZTexture.height;

        // 第 0 层：把相机 Depth Texture 拷贝进去
        cmd.Blit(null, hiZTexture, new Vector2(1,1), new Vector2(0,0));  // 直接 Blit _CameraDepthTexture（Built-in 自动绑定）

        // 逐层 downsample（max depth）
        for (int mip = 1; mip < mipCount; mip++)
        {
            width >>= 1;
            height >>= 1;

            hiZCompute.SetTexture(kernel, "_SrcDepth", hiZTexture, mip - 1);
            hiZCompute.SetTexture(kernel, "_DstDepth", hiZTexture, mip);

            int groupsX = Mathf.CeilToInt(width / 8f);
            int groupsY = Mathf.CeilToInt(height / 8f);
            cmd.DispatchCompute(hiZCompute, kernel, groupsX, groupsY, 1);
        }
    }

    // 调试用：把 Hi-Z 金字塔显示在屏幕左下角（可选）
    private void OnGUI()
    {
        if (hiZTexture != null)
            GUI.DrawTexture(new Rect(10, 10, 256, 256), hiZTexture, ScaleMode.ScaleToFit);
    }
}