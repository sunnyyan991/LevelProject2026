//////////////////////////////////////////////////////
// MK Glow Editor Helper UI Content	           		//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2020 All rights reserved.            //
//////////////////////////////////////////////////////
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

//Disable XRManagement warning for selective workflow
//XRpackage has to be imported...
#pragma warning disable CS0618

namespace MK.Glow.Editor
{
    public static partial class EditorHelper
    {
        public static class EditorUIContent
        {
            public static class Tooltips
            {
                public static readonly GUIContent workmode = new GUIContent("Workmode", "Choose between a Post Processing Volume or Global workmode. \nIf Global is used, then the Post Processing Framework can be skipped.");
                
                //Main
                public static readonly GUIContent renderPriority = new GUIContent("Render Priority", "Define if the pipeline should focus on quality or performance.");
                public static readonly GUIContent debugView = new GUIContent("Debug View", "Displaying of different render steps. \n \n" +
                                                                               "None: Debug view is disabled. \n\n" +
                                                                               "Raw Bloom: Shows extracted bloom map. \n\n" +
                                                                               "Raw Lens Flare: Shows extracted lens flare map. \n\n" +
                                                                               "Raw Glare: Shows extracted glare map. \n\n" +
                                                                               "Bloom: Shows created bloom without lens surface. \n\n" +
                                                                               "Lens Flare: Shows created lens flare without lens surface. \n\n" +
                                                                               "Glare: Shows created glare without lens surface. \n\n" +
                                                                               "Composite: Shows combined bloom, lensflare, glare and lens surface, just without source image. \n");
                public static readonly GUIContent quality = new GUIContent("Quality", "General rendered quality of the glow. Higher setting results in better looking and less aliasing.");
                public static readonly GUIContent workflow = new GUIContent("Workflow", "Basic definition of the workflow. \n\n" +
                                                                              "Luminance: Glow map is defined by the pixels brightness and a threshold value. Just use the emission of the shaders and raise it up. Performs significantly faster than selective workflow.\n\n" +
                                                                               "Selective: Glow map is created by using separate shaders (MK/Glow/Selective).\n\n");
                public static readonly GUIContent selectiveRenderLayerMask = new GUIContent("Render Layer", "In most cases 'Everything' should be chosen to avoid Z issues.");
                public static readonly GUIContent anamorphicRatio = new GUIContent("Anamorphic", "Anamorphic scaling. \n\n" +
                                                                                     "> 0: scaling horizontally\n" +
                                                                                     "< 0: scaling vertically\n" +
                                                                                     "0: no scaling");

                //Bloom
                public static readonly GUIContent bloomThreshold = new GUIContent("Threshold", "Threshold in gamma space for extraction of bright areas. \n\n Min: Minimum brightness until the bloom starts. \n Max: Maximum brightness for cutting off colors.");
                public static readonly GUIContent bloomScattering = new GUIContent("Scattering", "Scattering of the bloom. A higher value increases the scattered area.");
                public static readonly GUIContent bloomIntensity = new GUIContent("Intensity", "Intensity of the bloom in gamma space.");

                //Lens Surface
                public static readonly GUIContent allowLensSurface = new GUIContent("", "");
                public static readonly GUIContent lensSurfaceDirtTexture = new GUIContent("Dirt", "Dirt overlay which will be applied to the glow (RGB). Best results if texture is tileable.");
                public static readonly GUIContent lensSurfaceDirtIntensity = new GUIContent("Intensity", "Intensity of the dirt effect. Value is in gamma space.");
                public static readonly GUIContent lensSurfaceDiffractionTexture = new GUIContent("Diffraction", "Diffraction overlay which will be applied to the glow (RGB). Texture is rotating based on view.");
                public static readonly GUIContent lensSurfaceDiffractionIntensity = new GUIContent("Intensity", "Intensity of the diffraction effect in gamma space.");
            }

            public static readonly string mainTitle = "Main";
            public static readonly string bloomTitle = "Bloom";
            public static readonly string lensSurfaceTitle = "Lens Surface";
            public static readonly string dirtTitle = "Dirt:";
            public static readonly string diffractionTitle = "Diffraction:";

            public static void LensFlareFeatureNotSupportedWarning()
            {
                EditorGUILayout.HelpBox("Lens flare feature is not supported on your active graphics api / render setup.", MessageType.Warning);
            }

            public static void GlareFeatureNotSupportedWarning()
            {
                EditorGUILayout.HelpBox("Glare feature is not supported on your active graphics api / render setup.", MessageType.Warning);
            }

            public static void OptimalSetupWarning(UnityEngine.Camera camera, bool warningAllowed)
            {
                if(warningAllowed)
                {
                    string msg = "";
                    if(!camera.allowHDR && PlayerSettings.colorSpace != ColorSpace.Linear)
                    {
                        msg = "linear color space and hdr";
                    }
                    else if(PlayerSettings.colorSpace != ColorSpace.Linear)
                    {
                        msg = "linear color space";
                    }
                    else if(!camera.allowHDR)
                    {
                        msg = "hdr";
                    }
                    if(!camera.allowHDR || PlayerSettings.colorSpace != ColorSpace.Linear)
                        EditorGUILayout.HelpBox("For best looking results its recommend to use " + msg, MessageType.Warning);
                }
            }

            public static void XRUnityVersionWarning()
            {
                #if UNITY_2018_3_OR_NEWER
                #else
                if(PlayerSettings.virtualRealitySupported)
                {
                    EditorGUILayout.HelpBox("Your are currently targeting XR. For best XR support its recommend to update to unity 2018.3 or higher.", MessageType.Warning);
                }
                #endif
            }

            public static void SelectiveWorkflowVRWarning(MK.Glow.Workflow workflow)
            {
                #if UNITY_6000_OR_NEWER
                if(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null && workflow == Workflow.Selective)
                #else
                if(UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null && workflow == Workflow.Selective)
                #endif
                {
                    EditorGUILayout.HelpBox("Selective workflow isn't supported if a scriptable rendering pipeline is active. Please use Luminance workflow instead.", MessageType.Warning);
                }
                if(PlayerSettings.virtualRealitySupported && workflow == Workflow.Selective)
                {
                    EditorGUILayout.HelpBox("Selective workflow isn't supported in XR. Please use Threshold workflow instead.", MessageType.Warning);
                }
            }

            public static void IsNotSupportedWarning()
            {
                if(!Compatibility.IsSupported)
                    EditorGUILayout.HelpBox("Plugin is not supported and will be disabled. At least any HDR RenderTexture format should be supported by your hardware.", MessageType.Warning);
            }

            public static void SelectiveWorkflowDeprecated()
            {
                EditorGUILayout.HelpBox("Selective Workflow will be deprecated in a future update, due to engine compatibility issues. Its highly recommend to use Threshold Workflow.", MessageType.Warning);
            }
        }
    }
}
#endif