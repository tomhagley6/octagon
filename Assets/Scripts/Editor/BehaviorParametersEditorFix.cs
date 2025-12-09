using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.MLAgents.Policies;

namespace Unity.MLAgents.Editor
{
    /// <summary>
    /// Patched version of BehaviorParametersEditor that prevents sensor validation
    /// from running during Play mode. This fixes a race condition in the built-in 
    /// editor that causes NullReferenceExceptions with custom sensors.
    /// 
    /// The built-in editor calls InitializeSensors() on every Inspector redraw,
    /// which corrupts the agent's sensor list during gameplay. This wrapper delegates
    /// to the original editor but intercepts the problematic validation call.
    /// </summary>
    [CustomEditor(typeof(BehaviorParameters))]
    [CanEditMultipleObjects]
    public class BehaviorParametersEditorFix : UnityEditor.Editor
    {
        private UnityEditor.Editor builtInEditor;
        private Type builtInEditorType;
        private MethodInfo displayFailedModelChecksMethod;
        private bool hasReflectionError = false;

        void OnEnable()
        {
            // Find the original BehaviorParametersEditor from ML-Agents package
            builtInEditorType = Type.GetType("Unity.MLAgents.Editor.BehaviorParametersEditor, Unity.ML-Agents.Editor");
            
            if (builtInEditorType != null)
            {
                // Create an instance of the original editor
                builtInEditor = CreateEditor(target, builtInEditorType);
                
                // Get the DisplayFailedModelChecks method so we can intercept it
                displayFailedModelChecksMethod = builtInEditorType.GetMethod(
                    "DisplayFailedModelChecks", 
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                
                if (displayFailedModelChecksMethod == null)
                {
                    Debug.LogWarning("[BehaviorParametersEditorFix] Could not find DisplayFailedModelChecks method. Using fallback.");
                    hasReflectionError = true;
                }
            }
            else
            {
                Debug.LogWarning("[BehaviorParametersEditorFix] Could not find built-in BehaviorParametersEditor. Using fallback.");
                hasReflectionError = true;
            }
        }

        void OnDisable()
        {
            if (builtInEditor != null)
            {
                DestroyImmediate(builtInEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            // Fallback if reflection failed: use simple read-only view
            if (hasReflectionError || builtInEditor == null)
            {
                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "Inspector simplified during Play mode to prevent race conditions with custom sensors.",
                        MessageType.Info);
                    EditorGUI.BeginDisabledGroup(true);
                }
                
                DrawDefaultInspector();
                
                if (Application.isPlaying)
                {
                    EditorGUI.EndDisabledGroup();
                }
                return;
            }

            // During Play mode, temporarily replace the problematic method with a no-op
            if (Application.isPlaying)
            {
                // We can't actually replace the method, so we'll prevent the built-in
                // editor from running and show a simplified view instead
                EditorGUILayout.HelpBox(
                    "Model validation disabled during Play mode to prevent interference with running agents.",
                    MessageType.Info);
                
                // Show the properties but disabled
                EditorGUI.BeginDisabledGroup(true);
                DrawDefaultInspector();
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                // In Edit mode, use the full built-in editor with all its validation
                builtInEditor.OnInspectorGUI();
            }
        }
    }
}
