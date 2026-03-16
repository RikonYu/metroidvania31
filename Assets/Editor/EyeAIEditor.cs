using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EyeAI))]
public class EyeAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Eye Control", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hurtTrackSpeedMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hurtShakeAmplitude"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("openDoorsOnDeath"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        MonoScript script = MonoScript.FromMonoBehaviour((EyeAI)target);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }
}
