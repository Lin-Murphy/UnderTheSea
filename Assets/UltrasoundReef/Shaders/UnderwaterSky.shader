Shader "UltrasoundReef/SunlitWaterSky" {
 Properties {_Brightness("Water brightness",Range(0,2))=.85
 _Shallow("Shallow water (toward the surface)",Color)=(.42,.84,.90,1)
 _Deep("Deep water (looking down)",Color)=(.015,.10,.23,1)}
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Brightness;half4 _Shallow,_Deep;
 CBUFFER_END
 float _ReefNightBlend;
 struct A{float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 dir:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.dir=i.p.xyz;return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float3 d=normalize(i.dir);float s=saturate(dot(d,normalize(float3(-.28,.63,.72))));
 half3 night=lerp(half3(1,1,1),half3(.07,.13,.23),_ReefNightBlend);
 // 2026-11-05: shallow-to-deep gradient. The horizon is exactly the fog colour, so distant reef fades
 // seamlessly into open water; looking up brightens toward the shallow surface, looking down sinks to deep blue.
 half3 horizon=unity_FogColor.rgb;
 half3 up=lerp(horizon,_Shallow.rgb*_Brightness*night,smoothstep(.02,.85,d.y));
 half3 color=d.y>=0?up:lerp(horizon,_Deep.rgb*night,smoothstep(0,.55,-d.y));
 color+=half3(.749,.937,.941)*pow(s,18)*.17*night;
 color+=half3(1,.93,.76)*pow(s,160)*.3*night;
 color+=half3(2.5,2.4,2.1)*smoothstep(.999,.9996,s)*night;
 return half4(color,1);}
 ENDHLSL
 }
 }
}
