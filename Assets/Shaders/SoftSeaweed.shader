Shader "UnderTheSea/SoftSeaweed" {
 Properties { _BaseColor("Root color",Color)=(.42,.58,.38,1) _TipColor("Tip color",Color)=(.72,.82,.55,1) _Backlight("Backlight",Range(0,1))=.35 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"} Cull Off
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_TipColor; half _Backlight;
 CBUFFER_END
 float _ReefNightBlend;
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;half4 c:COLOR;float f:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.c=i.c;o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  half3 n=normalize(i.n)*(front?1:-1);Light l=GetMainLight();
  half wrap=saturate(dot(n,l.direction)*.5+.5);
  half back=saturate(dot(-n,l.direction))*_Backlight;          // thin leaves glow a little when lit from behind
  half vein=1-.18*pow(abs(i.uv.x*2-1),3);                      // darker edges, brighter midrib
  half3 c=lerp(_BaseColor.rgb,_TipColor.rgb,smoothstep(0,1,i.uv.y))*i.c.rgb*vein;
  c*= .62+.38*wrap+back;
  c*=lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend);
  return half4(MixFog(c,i.f),1);}
 ENDHLSL
 }
 }
}
