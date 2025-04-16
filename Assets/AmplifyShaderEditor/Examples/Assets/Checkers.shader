// Made with Amplify Shader Editor v1.9.6.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Checkers"
{
	Properties
	{
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows   
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _TextureSample0;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 color8 = IsGammaSpace() ? float4(1,1,1,0) : float4(1,1,1,0);
			float4 color7 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float2 temp_cast_0 = (5.0).xx;
			float2 break12 = i.uv_texcoord;
			float4 appendResult17 = (float4(( break12.x + ( _Time.y * 0.25 ) ) , ( break12.y + ( _Time.y * 0.25 ) ) , 0.0 , 0.0));
			float2 FinalUV13_g1 = ( temp_cast_0 * ( 0.5 + appendResult17.xy ) );
			float2 temp_cast_2 = (0.5).xx;
			float2 temp_cast_3 = (1.0).xx;
			float4 appendResult16_g1 = (float4(ddx( FinalUV13_g1 ) , ddy( FinalUV13_g1 )));
			float4 UVDerivatives17_g1 = appendResult16_g1;
			float4 break28_g1 = UVDerivatives17_g1;
			float2 appendResult19_g1 = (float2(break28_g1.x , break28_g1.z));
			float2 appendResult20_g1 = (float2(break28_g1.x , break28_g1.z));
			float dotResult24_g1 = dot( appendResult19_g1 , appendResult20_g1 );
			float2 appendResult21_g1 = (float2(break28_g1.y , break28_g1.w));
			float2 appendResult22_g1 = (float2(break28_g1.y , break28_g1.w));
			float dotResult23_g1 = dot( appendResult21_g1 , appendResult22_g1 );
			float2 appendResult25_g1 = (float2(dotResult24_g1 , dotResult23_g1));
			float2 derivativesLength29_g1 = sqrt( appendResult25_g1 );
			float2 temp_cast_4 = (-1.0).xx;
			float2 temp_cast_5 = (1.0).xx;
			float2 clampResult57_g1 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g1 + 0.25 ) ) - temp_cast_2 ) ) * 4.0 ) - temp_cast_3 ) * ( 0.35 / derivativesLength29_g1 ) ) , temp_cast_4 , temp_cast_5 );
			float2 break71_g1 = clampResult57_g1;
			float2 break55_g1 = derivativesLength29_g1;
			float4 lerpResult73_g1 = lerp( color8 , color7 , saturate( ( 0.5 + ( 0.5 * break71_g1.x * break71_g1.y * sqrt( saturate( ( 1.1 - max( break55_g1.x , break55_g1.y ) ) ) ) ) ) ));
			o.Albedo = ( tex2D( _TextureSample0, ( 10.0 * i.uv_texcoord ) ) * lerpResult73_g1 ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19603
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-1536,-912;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;9;-1008,-1232;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-1248,-736;Inherit;False;Constant;_Float13;Float 13;0;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;19;-1232,-864;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-1152,-1152;Inherit;False;Constant;_Float12;Float 12;0;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;12;-1312,-1056;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-752,-1200;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-1008,-816;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;-576,-1136;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;21;-732.0063,-946.5435;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;192,-1312;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;28;464,-1328;Inherit;False;Constant;_Float15;Float 15;1;0;Create;True;0;0;0;False;0;False;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;7;-1072,-240;Inherit;False;Constant;_Color2;Color 2;0;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;4;-720,-192;Inherit;True;Constant;_Float11;Float 11;0;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;17;-352,-1248;Inherit;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;8;-1312,-560;Inherit;False;Constant;_Color3;Color 3;0;0;Create;True;0;0;0;False;0;False;1,1,1,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;464,-1232;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;2;-304,-960;Inherit;True;Checkerboard;-1;;1;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;24;272,-1088;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;b6ccb75e36dcf6b4396c44d0acb59672;b6ccb75e36dcf6b4396c44d0acb59672;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-192,-640;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-64,-1184;Inherit;False;Constant;_Float14;Float 14;1;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;128,-832;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Checkers;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;2;;;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;12;0;1;0
WireConnection;10;0;9;0
WireConnection;10;1;11;0
WireConnection;20;0;19;0
WireConnection;20;1;18;0
WireConnection;14;0;12;0
WireConnection;14;1;10;0
WireConnection;21;0;12;1
WireConnection;21;1;20;0
WireConnection;17;0;14;0
WireConnection;17;1;21;0
WireConnection;27;0;28;0
WireConnection;27;1;26;0
WireConnection;2;1;17;0
WireConnection;2;2;8;0
WireConnection;2;3;7;0
WireConnection;2;4;4;0
WireConnection;24;1;27;0
WireConnection;23;0;24;0
WireConnection;23;1;2;0
WireConnection;0;0;23;0
ASEEND*/
//CHKSM=5EAC0F95048E393047FC1AC5AF4F1F247D866C2B