// The sea surface seen from below: a gently heaving ceiling with drifting light ripples and a brighter
// "window" straight overhead. Fog fades it into the water colour at a distance.
Shader "UnderTheSea/SeaSurface" {
 Properties {
 _DeepColor("Surface tint",Color)=(.30,.70,.80,1)
 _LightColor("Sunlit ripples",Color)=(.86,.98,1,1)
 _Caustics("Ripple texture",2D)="black"{}
 _Ripple("Ripple strength",Range(0,2))=.9
 _Scale("Ripple scale",Float)=.09
 _Speed("Ripple drift",Float)=.035
 _Swell("Swell height",Float)=.14
 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+50"} Cull Off
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_Caustics);SAMPLER(sampler_Caustics);
 CBUFFER_START(UnityPerMaterial)
 half4 _DeepColor,_LightColor;float4 _Caustics_ST;float _Ripple,_Scale,_Speed,_Swell;
 CBUFFER_END
 float _ReefNightBlend;
 struct A{float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float f:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  float3 w=TransformObjectToWorld(i.p.xyz);float t=_Time.y;
  w.y+=(sin(w.x*.35+t*.6)*.6+sin(w.z*.27-t*.45)*.5+sin((w.x+w.z)*.6+t*.9)*.25)*_Swell;
  o.w=w;o.p=TransformWorldToHClip(w);o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float t=_Time.y;float2 uv=i.w.xz*_Scale;
  half r1=SAMPLE_TEXTURE2D(_Caustics,sampler_Caustics,uv+float2(t,t*.6)*_Speed).r;
  half r2=SAMPLE_TEXTURE2D(_Caustics,sampler_Caustics,uv*1.37-float2(t*.7,-t)*_Speed+float2(.31,.17)).r;
  half r3=SAMPLE_TEXTURE2D(_Caustics,sampler_Caustics,uv*.45+float2(-t,t*.3)*_Speed*.6).r;
  half ripple=saturate(r1*.55+r2*.45)*(.6+.4*r3);
  float3 v=normalize(i.w-GetCameraPositionWS());
  half window=smoothstep(.55,.97,v.y);          // brighter straight overhead (Snell's window)
  half3 c=lerp(_DeepColor.rgb,_LightColor.rgb,window*.55)+_LightColor.rgb*ripple*_Ripple*(.3+.7*window);
  c*=lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend);
  return half4(MixFog(c,i.f),1);}
 ENDHLSL
 }
 }
}
