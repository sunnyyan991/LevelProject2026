//////////////////////////////////////////////////////
// MK Glow Editor Postprocessing Stack	      		//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2019 All rights reserved.            //
//////////////////////////////////////////////////////
#if UNITY_EDITOR
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;
using MK.Glow;
using UnityEditor;
using UnityEngine;
using MK.Glow.Editor;

namespace MK.Glow.LWRP.Editor
{
	using Tooltips = EditorHelper.EditorUIContent.Tooltips;

	[PostProcessEditor(typeof(MK.Glow.LWRP.MKGlowLite))]
	internal class MKGlowEditor : PostProcessEffectEditor<MK.Glow.LWRP.MKGlowLite>
	{
		//Behaviors
		private SerializedProperty _showEditorMainBehavior;
		private SerializedProperty _showEditorBloomBehavior;
		private SerializedProperty _showEditorLensSurfaceBehavior;
		private SerializedProperty _isInitialized;

		//Main
		private SerializedParameterOverride _debugView;
		private SerializedParameterOverride _workflow;
		private SerializedParameterOverride _selectiveRenderLayerMask;
		private SerializedParameterOverride _anamorphicRatio;

		//Bloom
		private SerializedParameterOverride _bloomThreshold;
		private SerializedParameterOverride _bloomScattering;
		private SerializedParameterOverride _bloomIntensity;

		//Lens Surface
		private SerializedParameterOverride _allowLensSurface;
		private SerializedParameterOverride _lensSurfaceDirtTexture;
		private SerializedParameterOverride _lensSurfaceDirtIntensity;
		private SerializedParameterOverride _lensSurfaceDiffractionTexture;
		private SerializedParameterOverride _lensSurfaceDiffractionIntensity;

		public override void OnEnable()
		{
			//Editor
			_showEditorMainBehavior = FindProperty(x => x.showEditorMainBehavior);
			_showEditorBloomBehavior = FindProperty(x => x.showEditorBloomBehavior);
			_showEditorLensSurfaceBehavior = FindProperty(x => x.showEditorLensSurfaceBehavior);
			_isInitialized = FindProperty(x => x.isInitialized);

			//Main
			_debugView = FindParameterOverride(x => x.debugView);
			_workflow = FindParameterOverride(x => x.workflow);
			_selectiveRenderLayerMask = FindParameterOverride(x => x.selectiveRenderLayerMask);
			_anamorphicRatio = FindParameterOverride(x => x.anamorphicRatio);

			//Bloom
			_bloomThreshold = FindParameterOverride(x => x.bloomThreshold);
			_bloomScattering = FindParameterOverride(x => x.bloomScattering);
			_bloomIntensity = FindParameterOverride(x => x.bloomIntensity);

			_allowLensSurface = FindParameterOverride(x => x.allowLensSurface);
			_lensSurfaceDirtTexture = FindParameterOverride(x => x.lensSurfaceDirtTexture);
			_lensSurfaceDirtIntensity = FindParameterOverride(x => x.lensSurfaceDirtIntensity);
			_lensSurfaceDiffractionTexture = FindParameterOverride(x => x.lensSurfaceDiffractionTexture);
			_lensSurfaceDiffractionIntensity = FindParameterOverride(x => x.lensSurfaceDiffractionIntensity);
		}

		public override void OnInspectorGUI()
		{
			if(_isInitialized.boolValue == false)
			{
				_bloomIntensity.value.floatValue = 1f;
				_bloomIntensity.overrideState.boolValue = true;

				_lensSurfaceDirtIntensity.value.floatValue = 2.5f;
				_lensSurfaceDirtIntensity.overrideState.boolValue = true;
				_lensSurfaceDiffractionIntensity.value.floatValue = 2f;
				_lensSurfaceDiffractionIntensity.overrideState.boolValue = true;

				_isInitialized.boolValue = true;
			}

			EditorHelper.EditorUIContent.IsNotSupportedWarning();
			EditorHelper.EditorUIContent.XRUnityVersionWarning();
			if(_workflow.value.enumValueIndex == 1)
            {
				EditorHelper.EditorUIContent.SelectiveWorkflowDeprecated();
			}

			if(EditorHelper.HandleBehavior(_showEditorMainBehavior.serializedObject.targetObject, EditorHelper.EditorUIContent.mainTitle, "", _showEditorMainBehavior, null))
			{
				PropertyField(_debugView, Tooltips.debugView);
				PropertyField(_workflow, Tooltips.workflow);
				EditorHelper.EditorUIContent.SelectiveWorkflowVRWarning((Workflow)_workflow.value.enumValueIndex);
                if(_workflow.value.enumValueIndex == 1)
                {
                    PropertyField(_selectiveRenderLayerMask, Tooltips.selectiveRenderLayerMask);
                }
				PropertyField(_anamorphicRatio, Tooltips.anamorphicRatio);
				EditorHelper.VerticalSpace();
			}
			
			if(EditorHelper.HandleBehavior(_showEditorBloomBehavior.serializedObject.targetObject, EditorHelper.EditorUIContent.bloomTitle, "", _showEditorBloomBehavior, null))
			{
				if(_workflow.value.enumValueIndex == 0)
					PropertyField(_bloomThreshold, Tooltips.bloomThreshold);
				PropertyField(_bloomScattering, Tooltips.bloomScattering);
				PropertyField(_bloomIntensity, Tooltips.bloomIntensity);
				_bloomIntensity.value.floatValue = Mathf.Max(0, _bloomIntensity.value.floatValue);

				EditorHelper.VerticalSpace();
			}

			if(EditorHelper.HandleBehavior(_showEditorLensSurfaceBehavior.serializedObject.targetObject, EditorHelper.EditorUIContent.lensSurfaceTitle, "", _showEditorLensSurfaceBehavior, _allowLensSurface.value))
			{
				using (new EditorGUI.DisabledScope(!_allowLensSurface.value.boolValue))
                {
					EditorHelper.DrawHeader(EditorHelper.EditorUIContent.dirtTitle);
					PropertyField(_lensSurfaceDirtTexture, Tooltips.lensSurfaceDirtTexture);
					PropertyField(_lensSurfaceDirtIntensity, Tooltips.lensSurfaceDirtIntensity);
					_lensSurfaceDirtIntensity.value.floatValue = Mathf.Max(0, _lensSurfaceDirtIntensity.value.floatValue);
					EditorGUILayout.Space();
					EditorHelper.DrawHeader(EditorHelper.EditorUIContent.diffractionTitle);
					PropertyField(_lensSurfaceDiffractionTexture, Tooltips.lensSurfaceDiffractionTexture);
					PropertyField(_lensSurfaceDiffractionIntensity, Tooltips.lensSurfaceDiffractionIntensity);
					_lensSurfaceDiffractionIntensity.value.floatValue = Mathf.Max(0, _lensSurfaceDiffractionIntensity.value.floatValue);
				}
				EditorHelper.VerticalSpace();
			}

			EditorHelper.DrawSplitter();
		}
    }
}
#endif