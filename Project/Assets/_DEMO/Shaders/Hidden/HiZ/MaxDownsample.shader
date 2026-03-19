Shader "Hidden/HiZ/MaxDownsample"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float frag (v2f_img i) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * 0.5;
                float d0 = tex2D(_MainTex, i.uv + float2(-offset.x, -offset.y)).r;
                float d1 = tex2D(_MainTex, i.uv + float2( offset.x, -offset.y)).r;
                float d2 = tex2D(_MainTex, i.uv + float2(-offset.x,  offset.y)).r;
                float d3 = tex2D(_MainTex, i.uv + float2( offset.x,  offset.y)).r;
                return max(max(d0, d1), max(d2, d3));
            }
            ENDCG
        }
    }
}