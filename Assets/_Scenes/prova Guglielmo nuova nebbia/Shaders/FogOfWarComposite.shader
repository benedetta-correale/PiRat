Shader "Hidden/FogOfWarComposite"
{
    Properties
    {
        _MainTex("SceneTex", 2D) = "white" {}
        _MaskTex("MaskTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay+1" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                // mask = [0,1], 0 = fog, 1 = scene
                float mask = tex2D(_MaskTex, i.uv).r;
                fixed4 sceneCol = tex2D(_MainTex, i.uv);
                // fuori maschera nero; dentro maschera scena
                return lerp(fixed4(0,0,0,1), sceneCol, mask);
            }
            ENDCG
        }
    }
}
