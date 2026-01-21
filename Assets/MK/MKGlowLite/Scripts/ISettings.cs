//////////////////////////////////////////////////////
// MK Glow ISettings 	    	    	       		//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2021 All rights reserved.            //
//////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Glow
{
    internal interface ISettings
    {
        //Main
        bool GetAllowGeometryShaders();
        bool GetAllowComputeShaders ();
        MK.Glow.DebugView GetDebugView();
        MK.Glow.Workflow GetWorkflow();
        LayerMask GetSelectiveRenderLayerMask();
        float GetAnamorphicRatio();

        //Bloom
		MK.Glow.MinMaxRange GetBloomThreshold();
		float GetBloomScattering();
		float GetBloomIntensity();

        //LensSurface
		bool GetAllowLensSurface();
		Texture2D GetLensSurfaceDirtTexture();
		float GetLensSurfaceDirtIntensity();
		Texture2D GetLensSurfaceDiffractionTexture();
		float GetLensSurfaceDiffractionIntensity();
    }
}