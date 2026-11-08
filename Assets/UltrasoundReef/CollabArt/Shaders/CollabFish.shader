// Collaborator fish colors (one material per submesh) with the PastelFish tail sway and night blend.
Shader "UltrasoundReef/CollabFish" {
 Properties{_BaseColor("Color",Color)=(1,1,1,1) _Gloss("Soft highlight",Range(0,1))=.25}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass{Tags{"LightMode"="UniversalForward"} Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;half _Gloss;
 CBUFFER_END
 float _ReefNightBlend;
 float _ReefMotionTime,_ReefMotionActive;
 struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float f:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 float t=lerp(_Time.y,_ReefMotionTime,_ReefMotionActive);float phase=unity_ObjectToWorld._m03*1.7+unity_ObjectToWorld._m23;
 float tail=saturate((-i.p.z+.04)/.4);i.p.x+=sin(t*3.3+phase+i.p.z*8)*tail*tail*.055;
 o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 Light l=GetMainLight();half3 n=normalize(i.n)*(front?1:-1);half d=saturate(dot(n,l.direction)*.5+.5);
 half3 e=normalize(GetWorldSpaceViewDir(i.w));half spec=pow(saturate(dot(n,normalize(l.direction+e))),24)*_Gloss;
 half3 c=_BaseColor.rgb*(.72+.28*d)+spec;
 return half4(MixFog(c*lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend),i.f),1);}
 ENDHLSL
 }
 }
}
