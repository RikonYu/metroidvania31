using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Room))]
public class RoomSnapper : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

        // 显示当前的吸附规则提示
        Minimap map = FindObjectOfType<Minimap>();
        if (map != null)
        {
            float stepX = 16 * map.cellSizeWorldUnits;
            float stepY = 9 * map.cellSizeWorldUnits;
            EditorGUILayout.HelpBox($"当前吸附步长:\nX: {stepX} (16 * {map.cellSizeWorldUnits})\nY: {stepY} (9 * {map.cellSizeWorldUnits})", MessageType.Info);
        }

        if (GUILayout.Button("Snap to (16x, 9y) Grid"))
        {
            SnapAllRooms();
        }
    }

    private void SnapAllRooms()
    {
        Minimap map = FindObjectOfType<Minimap>(true);

        if (map == null)
        {
            Debug.LogError("场景中未找到 Minimap 组件！");
            return;
        }

        // 核心公式：步长 = (16, 9) * Unit
        float unit = map.cellSizeWorldUnits;
        float stepX = 16f * unit;
        float stepY = 9f * unit;

        // 防止除以0的保护
        if (stepX <= 0.001f) stepX = 1f;
        if (stepY <= 0.001f) stepY = 1f;

        Room[] allRooms = FindObjectsOfType<Room>(true);
        int moveCount = 0;

        foreach (var room in allRooms)
        {
            Transform t = room.transform;
            Vector3 oldPos = t.position;

            // 计算吸附后的坐标
            float snappedX = Mathf.Round(oldPos.x / stepX) * stepX;
            float snappedY = Mathf.Round(oldPos.y / stepY) * stepY;
            
            // 只有当位置真的发生变化时才记录和修改
            if (Mathf.Abs(snappedX - oldPos.x) > 0.01f || Mathf.Abs(snappedY - oldPos.y) > 0.01f)
            {
                Undo.RecordObject(t, "Snap Room Position");
                t.position = new Vector3(snappedX, snappedY, oldPos.z);
                EditorUtility.SetDirty(t);
                
                Debug.Log($"已修正 Room '{room.name}': {oldPos} -> {t.position} (步长: {stepX}x{stepY})");
                moveCount++;
            }
        }

        if (moveCount > 0)
        {
            Debug.Log($"完成！共调整了 {moveCount} 个房间的位置。");
        }
        else
        {
            Debug.Log("所有房间已经对齐，无需调整。");
        }
    }
}