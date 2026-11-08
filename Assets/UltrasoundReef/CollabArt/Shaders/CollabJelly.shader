// Collaborator jellyfish (single bell + tentacle mesh). Keeps the MoonJellyfish contract driven by
// JellyfishBreath: _Breath (inhale swell + glow), _Visibility (arrival fade) and _JellyTime.
Shader "UltrasoundReef/CollabJelly" {
 Properties {_BaseColor("Color",Color)=(.54,.78,.86,1) _Opacity("Opacity",Range(0,1))=.46 _Glow("Breath glow",Range(0,3))=.6
 _BellRadius("Bell radius (object)",Float)=.5 _TentacleLength("Tentacle length (object)",Float)=1.8
 _Breath("Breath",Range(0,1))=0 _Visibility("Arrival visibility",Range(0,1))=1 _JellyTime("Animation time",Float)=0}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass{
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;float _Opacity,_Glow,_BellRadius,_TentacleLength,_Breath,_Visibility,_JellyTime;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 float b=saturate(_Breath),t=_JellyTime;float3 p=i.p.xyz;
 float below=saturate(-p.y/max(.01,_TentacleLength));float bell=1-saturate(below*5);
 float rim=saturate(length(p.xz)/max(.01,_BellRadius));
 p.xz*=1+(b*.18+sin(t*.95)*(.008+b*.014)*rim*rim)*bell;p.y+=sin(t*.95)*.012*rim*bell;
 p.x+=sin(t*.8-below*3.4)*below*below*.085;p.z+=cos(t*.7-below*3)*below*below*.07;
 o.w=TransformObjectToWorld(p);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float3 n=normalize(i.n)*(front?1:-1);float rim=pow(1-abs(dot(n,normalize(GetWorldSpaceViewDir(i.w)))),2.5);
 half b=saturate(_Breath);
 // 2026-11-05: breathing glow is exaggerated - the whole body lights up, the rim flares and fog is cut through.
 half g=b*b*(3-2*b);
 // 2026-11-05: breathing glow peak halved (user request).
 half3 c=_BaseColor.rgb*(.75+g*_Glow*1.1)+half3(.8,.93,1)*rim*(.55+g*.8)+half3(.85,.95,1)*g*.375;
 half alpha=saturate(_Opacity*(.35+rim*.9)+g*.225);
 return half4(lerp(MixFog(c,i.fog),c,g*.35),alpha*_Visibility);}
 ENDHLSL
 }
 }
}
