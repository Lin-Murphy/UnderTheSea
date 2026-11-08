Shader "UltrasoundReef/RootedKelp" {
 Properties { _BaseColor("Grey green",Color)=(.56,.65,.49,1) _TipColor("Leaf tip",Color)=(.7,.77,.59,1) _Interactive("Interactive",Float)=0 _Reveal("Gel reveal",Range(0,1))=0 _MotionTime("Motion time (-1 = automatic)",Float)=-1 _Bend("Reveal bend",Float)=-2.2 _ClearCenter("Coral clearance center",Vector)=(0,0,0,0) _ClearSize("Coral clearance size",Vector)=(0,0,0,0) }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}Cull Off
 Pass {Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_TipColor;float _Interactive,_Reveal,_MotionTime,_Bend;float4 _ClearCenter,_ClearSize;
 CBUFFER_END
 float _ReefNightBlend;
 float _ReefMotionTime,_ReefMotionActive;
 struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float h:TEXCOORD1;float f:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 float h=saturate(i.p.y/2.4);float t=_MotionTime>=0?_MotionTime:lerp(_Time.y,_ReefMotionTime,_ReefMotionActive);
 float3 world=TransformObjectToWorld(i.p.xyz);float phase=unity_ObjectToWorld._m03*.7+unity_ObjectToWorld._m23*.3;
 float reveal=saturate(_Reveal);
 float bend=_Interactive>.5?lerp(.65,_Bend,reveal):0;
 float wave=sin(t*.65+h*3.5+phase)*(_Interactive>.5?.1*reveal:.11);
 world+=float3((bend+wave)*h*h,-.65*h*h*reveal*_Interactive,(-.14*_Interactive+cos(t*.45+h*4+phase)*.035)*h*h);
 float3 delta=abs(world-_ClearCenter.xyz);
 if(_Interactive>.5 && h>.08 && all(delta<_ClearSize.xyz*.5))world.z=_ClearCenter.z-_ClearSize.z*.5-.035;
 o.p=TransformWorldToHClip(world);o.n=TransformObjectToWorldNormal(i.n);o.h=h;o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 half3 n=normalize(i.n)*(front?1:-1);Light l=GetMainLight();half diffuse=saturate(dot(n,l.direction)*.5+.5);
 half3 c=lerp(_BaseColor.rgb,_TipColor.rgb,i.h*.65)*(.65+.35*diffuse);
 return half4(MixFog(c*lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend),i.f),1);}
 ENDHLSL
 }
 }
}
