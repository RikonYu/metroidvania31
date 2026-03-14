using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaserBullet))]
public class LaserBulletEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        DrawPropertiesExcluding(serializedObject, "m_Script", "Duration");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        MonoScript script = MonoScript.FromMonoBehaviour((LaserBullet)target);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }
}
