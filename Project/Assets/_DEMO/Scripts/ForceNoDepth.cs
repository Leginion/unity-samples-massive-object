using UnityEngine;

public class ForceNoDepth : MonoBehaviour
{
    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.depthTextureMode = DepthTextureMode.None;
            Debug.Log("Forced depthTextureMode = None");
        }
    }
}