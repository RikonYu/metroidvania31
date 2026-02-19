using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        
        Minimap map = FindObjectOfType<Minimap>();
        if (map != null)
        {
            float stepX = 16 * map.cellSizeWorldUnits;
            float stepY = 9 * map.cellSizeWorldUnits;
            EditorGUILayout.HelpBox($"Snap Step:\nX: {stepX}\nY: {stepY}", MessageType.Info);
        }

        if (GUILayout.Button("Snap to (16x, 9y) Grid"))
        {
            SnapAllRooms();
        }
    }

    private void SnapAllRooms()
    {
        Minimap map = FindObjectOfType<Minimap>(true);
        if (map == null) return;

        float unit = map.cellSizeWorldUnits;
        float stepX = 16f * unit;
        float stepY = 9f * unit;

        Room[] allRooms = FindObjectsOfType<Room>(true);

        foreach (var room in allRooms)
        {
            Transform t = room.transform;
            Tilemap[] tilemaps = room.GetComponentsInChildren<Tilemap>(true);
            
            if (tilemaps.Length > 0)
            {
                float minWorldX = float.MaxValue;
                float minWorldY = float.MaxValue;

                foreach (var tm in tilemaps)
                {
                    tm.CompressBounds();
                    if (tm.cellBounds.size.x == 0 || tm.cellBounds.size.y == 0) continue;

                    Vector3 bl = tm.transform.TransformPoint(tm.localBounds.min);
                    if (bl.x < minWorldX) minWorldX = bl.x;
                    if (bl.y < minWorldY) minWorldY = bl.y;
                }

                if (minWorldX != float.MaxValue)
                {
                    Vector3 pivotOffset = new Vector3(minWorldX, minWorldY, t.position.z) - t.position;

                    if (Mathf.Abs(pivotOffset.x) > 0.001f || Mathf.Abs(pivotOffset.y) > 0.001f)
                    {
                        foreach (Transform child in t)
                        {
                            Undo.RecordObject(child, "Adjust Child Pivot");
                            child.position -= pivotOffset;
                        }
                        Undo.RecordObject(t, "Adjust Room Pivot");
                        t.position += pivotOffset;
                    }
                }
            }

            Vector3 currentPos = t.position;
            float snappedX = Mathf.Round(currentPos.x / stepX) * stepX;
            float snappedY = Mathf.Round(currentPos.y / stepY) * stepY;

            if (Mathf.Abs(snappedX - currentPos.x) > 0.01f || Mathf.Abs(snappedY - currentPos.y) > 0.01f)
            {
                Undo.RecordObject(t, "Snap Room Position");
                t.position = new Vector3(snappedX, snappedY, currentPos.z);
                EditorUtility.SetDirty(t);
            }
        }
    }
}