Shader "UltrasoundReef/CoralSunlight" {
 Properties {
 _BaseColor("Root coral",Color)=(.965,.71,.56,1)
 _TipColor("Cream tips",Color)=(.984,.89,.66,1)
 _Height("Gradient height",Float)=2
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
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_TipColor;float _Height,_Glow,_GlowIntensity;
 CBUFFER_END
 float _ReefNightBlend;
 struct A {float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float h:TEXCOORD2;float f:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.h=saturate(i.p.y/max(.1,_Height));o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 half3 n=normalize(i.n),e=normalize(GetWorldSpaceViewDir(i.w));Light l=GetMainLight();
 half lam=dot(n,l.direction)*.5+.5;
 half bands=.52+smoothstep(.2,.45,lam)*.22+smoothstep(.58,.85,lam)*.26;
 half3 c=lerp(_BaseColor.rgb,_TipColor.rgb,smoothstep(.55,1,i.h));
 half rim=pow(1-saturate(dot(n,e)),3);
 half3 color=c*bands+half3(1,.72,.48)*rim*_Glow+c*_GlowIntensity;
 return half4(MixFog(color*lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend),i.f),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
