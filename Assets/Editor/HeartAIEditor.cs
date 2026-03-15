using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeartAI))]
public class HeartAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LaunchPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("InsectPrefab"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State 1", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("state1LaunchInterval"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State 2", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("state2BurstInterval"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Damage Reaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hurtBurstCooldown"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vessel Reveal", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("revealStepInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("revealDuration"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        MonoScript script = MonoScript.FromMonoBehaviour((HeartAI)target);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }
}
