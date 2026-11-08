// 2026-11-06 场景4 "Hold" 字幕:TextMesh 的字体贴图(alpha)× 顶点颜色,始终显示在最前面(不被海草/珊瑚挡住)。
Shader "UnderTheSea/HoldText" {
 Properties { _MainTex("Font Texture",2D)="white"{} _Color("Color",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST; half4 _Color;
 CBUFFER_END
 struct A{float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.c=i.c*_Color;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
  return half4(i.c.rgb,i.c.a*a);}
 ENDHLSL
 }
 }
}
