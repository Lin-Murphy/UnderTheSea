Shader "UltrasoundReef/PastelFish" {
 Properties{_BaseColor("Body tint",Color)=(1,1,1,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 CBUFFER_END
 float _ReefNightBlend;
 float _ReefMotionTime,_ReefMotionActive;
 struct A{float4 p:POSITION;float3 n:NORMAL;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;half4 c:COLOR;float3 n:TEXCOORD0;float f:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 float t=lerp(_Time.y,_ReefMotionTime,_ReefMotionActive);float phase=unity_ObjectToWorld._m03*1.7+unity_ObjectToWorld._m23;
 float tail=saturate((-i.p.z+.04)/.4);i.p.x+=sin(t*3.3+phase+i.p.z*8)*tail*tail*.055;
 o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.c=i.c*_BaseColor;o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);Light l=GetMainLight();half d=saturate(dot(normalize(i.n),l.direction)*.5+.5);return half4(MixFog(i.c.rgb*(.75+.25*d)*lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend),i.f),1);}
 ENDHLSL
 }
 }
}
