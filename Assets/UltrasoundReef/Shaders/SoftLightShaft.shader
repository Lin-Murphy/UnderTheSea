Shader "UltrasoundReef/SoftLightShaft" {
 Properties { _BaseColor("Ray tint and opacity",Color)=(.75,.94,.94,.045) }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Blend SrcAlpha One ZWrite Off Cull Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 CBUFFER_END
 float _ReefNightBlend;
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 w:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 p=i.p.xyz;p.x+=sin(_Time.y*.08+p.y*.15)*.08*i.uv.y;o.w=TransformObjectToWorld(p);o.p=TransformWorldToHClip(o.w);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 half across=pow(saturate(1-abs(i.uv.x*2-1)),2);
 half ends=smoothstep(0,.22,i.uv.y)*(1-smoothstep(.75,1,i.uv.y));
 half proximity=smoothstep(.5,2,distance(_WorldSpaceCameraPos,i.w));
 return half4(_BaseColor.rgb,_BaseColor.a*across*ends*proximity*(1-_ReefNightBlend));}
 ENDHLSL
 }
 }
}
