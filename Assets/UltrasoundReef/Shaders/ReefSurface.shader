Shader "UltrasoundReef/SoftReefSurface" {
 Properties { _BaseColor("Base",Color)=(.25,.56,.60,1) _Caustics("Tiling caustics",2D)="black"{} _CausticStrength("Caustic strength",Range(0,.5))=.12 _TopLight("Lighter top faces",Range(0,.5))=.12 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_Caustics);SAMPLER(sampler_Caustics);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;float4 _Caustics_ST;float _CausticStrength,_TopLight;
 CBUFFER_END
 float _ReefNightBlend;
 struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float f:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.f=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 half3 n=normalize(i.n);Light l=GetMainLight();half lam=saturate(dot(n,l.direction)*.5+.5);
 half top=smoothstep(.05,.9,n.y);
 half3 base=_BaseColor.rgb*(.7+lam*.3)+half3(.55,.8,.76)*top*_TopLight;
 float2 uv=i.w.xz*.24;float2 drift=float2(_Time.y*.008,_Time.y*.005);
 half ca=SAMPLE_TEXTURE2D(_Caustics,sampler_Caustics,uv+drift).r;
 half cb=SAMPLE_TEXTURE2D(_Caustics,sampler_Caustics,uv*1.1-drift*.65+float2(.31,.17)).r;
 base+=half3(1,.93,.69)*(ca*.6+cb*.4)*_CausticStrength*top;
 return half4(MixFog(base*lerp(half3(1,1,1),half3(.16,.28,.43),_ReefNightBlend),i.f),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
