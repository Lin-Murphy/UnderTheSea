// Textured coral for the collaborator's baked atlases; same banded light, rim and night blend as CoralSunlight.
Shader "UltrasoundReef/CollabCoral" {
 Properties {
 _BaseMap("Baked color atlas",2D)="white"{}
 _BaseColor("Tint",Color)=(1,1,1,1)
 _Glow("Warm rim",Range(0,1))=.12
 _GlowIntensity("Breathing glow",Range(0,2))=.02
 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST;half4 _BaseColor;float _Glow,_GlowIntensity;
 CBUFFER_END
 float _ReefNightBlend;
 float _ReefBreathGlow;   // 2026-11-05: 场景4 吸气时由 BreathGlow 设置 0..1,让枝叶尖端发微光
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;float f:TEXCOORD3;float h:TEXCOORD4;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.f=ComputeFogFactor(o.p.z);o.h=i.p.y;return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 half3 n=normalize(i.n)*(front?1:-1),e=normalize(GetWorldSpaceViewDir(i.w));Light l=GetMainLight();
 half lam=dot(n,l.direction)*.5+.5;
 half bands=.52+smoothstep(.2,.45,lam)*.22+smoothstep(.58,.85,lam)*.26;
 half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
 half rim=pow(1-saturate(dot(n,e)),3);
 half3 color=c*bands+half3(1,.72,.48)*rim*_Glow+c*_GlowIntensity;
 // Baked coral/anemone meshes are unit height with the base at y=0, so object y marks branch tips.
 half tip=smoothstep(.7,1.,i.h);
 half shimmer=.8+.2*sin(_Time.y*2.2+i.w.x*3.1+i.w.z*2.3);
 half3 tipGlow=(half3(.55,.88,1)*tip*.42*shimmer+c*.06)*_ReefBreathGlow;   // 微光:只在尖端,轻微闪烁
 return half4(MixFog(color*lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend)+tipGlow,i.f),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
