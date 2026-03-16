using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArmAI))]
public class ArmAIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Arms", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftArm"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightArm"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftFireSpot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightFireSpot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("IAS"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bullet", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Bullet"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("restDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("armMoveSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fireInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engageOnlyInActiveRoom"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room Local Anchors", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftAnchor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightAnchor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sweepMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sweepMax"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Phase", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Portion"), true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        MonoScript script = MonoScript.FromMonoBehaviour((ArmAI)target);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }
}
