//////////////////////////////////////////////////////
// MK Glow Shader SM20								//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2020 All rights reserved.           
 //
//////////////////////////////////////////////////////
Shader "Hidden/MK/Glow/MKGlowSM20"
{
	HLSLINCLUDE
		#ifndef MK_RENDER_PIPELINE_BUILT_IN
			#define MK_RENDER_PIPELINE_BUILT_IN
		#endif
	ENDHLSL
	SubShader
	{
		Tags {"LightMode" = "Always" "RenderType"="Opaque" "PerformanceChecks"="False"}
		Cull Off ZWrite Off ZTest Always

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Copy - 0
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vertSimple
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include_with_pragmas "../Inc/Copy.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Presample - 1
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vertSimple
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile __ _MK_PPSV2
			#define _MK_BLOOM

			#include_with_pragmas "../Inc/Presample.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Downsample - 2
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vertSimple
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			
			#pragma multi_compile __ _MK_PPSV2
			#define _MK_BLOOM

			#include_with_pragmas "../Inc/Downsample.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Upsample - 3
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile __ _MK_PPSV2
			#define _MK_BLOOM

			#include_with_pragmas "../Inc/Upsample.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Composite - 4
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile __ _MK_PPSV2
			#pragma multi_compile __ _MK_LEGACY_BLIT

			#pragma multi_compile __ _MK_LENS_SURFACE

			#include_with_pragmas "../Inc/Composite.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Debug - 5
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile __ _MK_PPSV2
			#pragma multi_compile __ _MK_LEGACY_BLIT
			
			#pragma multi_compile __ _MK_DEBUG_RAW_BLOOM _MK_DEBUG_COMPOSITE
			#pragma multi_compile __ _MK_LENS_SURFACE

			#include_with_pragmas "../Inc/Debug.hlsl"
			ENDHLSL
		}
	}
	//Shadermodel 2 should be the lowest possible hardware level
	FallBack Off
}
