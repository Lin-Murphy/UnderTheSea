// Soft additive dots for the scene-4 breathing sparkles (no texture needed).
Shader "UnderTheSea/BreathSparkle" {
 Properties { _Intensity("Intensity",Range(0,4))=1.6 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Blend One One ZWrite Off Cull Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half _Intensity;
 CBUFFER_END
 struct A{float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.c=i.c;o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float2 d=i.uv*2-1;half r=saturate(1-dot(d,d));half core=r*r*r;
  return half4(i.c.rgb*core*i.c.a*_Intensity,0);}
 ENDHLSL
 }
 }
}
