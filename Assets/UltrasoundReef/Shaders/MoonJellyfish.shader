Shader "UltrasoundReef/MoonJellyfish" {
 Properties {_BaseColor("Translucent tint",Color)=(.866,.953,.969,1) _Part("0 bell / 1 arms / 2 tentacles / 3 marking",Float)=0 _Breath("Breath",Range(0,1))=0 _Visibility("Arrival visibility",Range(0,1))=1 _JellyTime("Animation time",Float)=0}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass{
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;float _Part,_Breath,_Visibility,_JellyTime;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;float fog:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 float b=saturate(_Breath),t=_JellyTime;float3 p=i.p.xyz;
 if(_Part<.5){float rim=saturate(length(p.xz)/.58);p.xz*=1+b*.12+sin(t*.95)*(.008+b*.014)*rim*rim;p.y+=sin(t*.95)*.012*rim;}
 else if(_Part<2.5){float h=saturate(-p.y/1.8);p.x+=sin(t*.8-h*3.4+i.uv.x)*h*h*.085;p.z+=cos(t*.7-h*3+i.uv.x)*h*h*.07;}
 else {p.xz*=1+b*.07;}
 o.w=TransformObjectToWorld(p);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float3 n=normalize(i.n)*(front?1:-1);float rim=pow(1-abs(dot(n,normalize(GetWorldSpaceViewDir(i.w)))),2.5);
 half3 c=_BaseColor.rgb;half alpha;
 if(_Part<.5){c=c*(.65+_Breath*.15)+half3(.8,.93,1)*rim*(.85+_Breath*.2);alpha=.03+rim*.32;}
 else if(_Part<1.5){c=lerp(half3(.95,.72,.87),c,saturate(i.uv.y*.9));c+=rim*.35;alpha=.12+rim*.18;}
 else if(_Part<2.5){c+=rim*.2;alpha=.13;}
 else {c=c*(.8+_Breath*1.1)+half3(.18,.12,.18)*rim;alpha=.58;}
 return half4(MixFog(c,i.fog),alpha*_Visibility);}
 ENDHLSL
 }
 }
}
