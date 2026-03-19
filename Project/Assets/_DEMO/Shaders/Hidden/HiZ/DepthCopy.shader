Shader "Hidden/HiZ/DepthCopy"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _CameraDepthTexture;
            float frag (v2f_img i) : SV_Target
            {
                float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                return Linear01Depth(d);
            }
            ENDCG
        }
    }
}