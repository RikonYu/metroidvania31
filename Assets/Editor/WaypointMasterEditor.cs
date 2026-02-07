using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaypointMaster))]
public class WaypointMasterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector 内容（如 pathColor 和 loop）
        DrawDefaultInspector();

        WaypointMaster script = (WaypointMaster)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("路径点管理", EditorStyles.boldLabel);

        // --- 按钮：添加路径点 ---
        if (GUILayout.Button("添加路径点"))
        {
            GameObject newPoint = new GameObject("Waypoint_" + script.transform.childCount);
            newPoint.transform.SetParent(script.transform);

            // 默认位置：放在最后一个点旁边，如果没有点则放在 Master 位置
            if (script.transform.childCount > 1)
            {
                Transform lastPoint = script.transform.GetChild(script.transform.childCount - 2);
                newPoint.transform.position = lastPoint.position + Vector3.right;
            }
            else
            {
                newPoint.transform.localPosition = Vector3.zero;
            }

            newPoint.AddComponent<Waypoint>();
            Undo.RegisterCreatedObjectUndo(newPoint, "Add Waypoint");
        }

        // --- 按钮：删除最后一个点 ---
        if (GUILayout.Button("删除末尾点"))
        {
            int childCount = script.transform.childCount;
            if (childCount > 0)
            {
                Undo.DestroyObjectImmediate(script.transform.GetChild(childCount - 1).gameObject);
            }
        }

        EditorGUILayout.Space();

        // --- 按钮：一键吸附地面 ---
        if (GUILayout.Button("一键贴地"))
        {
            SnapAllToGround(script);
        }
    }

    private void SnapAllToGround(WaypointMaster master)
    {
        Undo.RecordObjects(master.GetComponentsInChildren<Transform>(), "Snap Waypoints");

        foreach (Transform child in master.transform)
        {
            // 从点上方 5 米处向下发射射线
            Ray ray = new Ray(child.position + Vector3.up * 0.5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                child.position = hit.point;
            }
            else
            {
                // 如果是 2D 游戏，可以使用 Physics2D.Raycast
                Debug.LogWarning($"{child.name} 下方未检测到地面，请手动调整。");
            }
        }
    }
}