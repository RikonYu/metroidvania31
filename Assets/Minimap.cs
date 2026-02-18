using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[System.Serializable]
public class MinimapNode
{
    public Room roomReference;
    public Vector2Int gridPosition;
    public Vector2Int gridSize;
}

public class Minimap : MonoBehaviour
{ 
    public static Minimap instance;

    public float cellSizeWorldUnits = 2f;

    [Header("Grid Configuration")]
    public Vector2Int tileUnitRatio = new Vector2Int(16, 9);
    
    [Header("UI Settings")]
    public Transform gridParent;
    public GameObject uiGridPrefab;
    public float uiCellSize = 50f;

    [HideInInspector]
    public List<MinimapNode> generatedMapData = new List<MinimapNode>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (gridParent == null)
        {
            gridParent = transform.Find("GridParent");
        }
        
        if (GameController.instance != null)
        {
            GenerateMap();
        }
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (GameController.instance == null || GameController.instance.Rooms == null)
        {
            Debug.LogWarning("GameController instance or Rooms list is missing.");
            return;
        }

        Room[] allRooms = GameController.instance.Rooms.ToArray();
        
        if (allRooms.Length == 0) return;

        generatedMapData.Clear();

        float minWorldX = float.MaxValue;
        float maxWorldY = float.MinValue;

        foreach (var room in allRooms)
        {
            if (room == null) continue;
            Vector3 pos = room.transform.position;
            if (pos.x < minWorldX) minWorldX = pos.x;
            if (pos.y > maxWorldY) maxWorldY = pos.y;
        }

        float blockWidthWorld = tileUnitRatio.x * cellSizeWorldUnits;
        float blockHeightWorld = tileUnitRatio.y * cellSizeWorldUnits;

        foreach (var room in allRooms)
        {
            if (room == null) continue;

            Vector3 roomWorldPos = room.transform.position;
            
            float rawWidth = room.roomBounds.size.x / blockWidthWorld;
            float rawHeight = room.roomBounds.size.y / blockHeightWorld;

            int sizeX = Mathf.Max(1, Mathf.RoundToInt(rawWidth));
            int sizeY = Mathf.Max(1, Mathf.RoundToInt(rawHeight));
            Vector2Int roomGridSize = new Vector2Int(sizeX, sizeY);

            int gridX = Mathf.RoundToInt((roomWorldPos.x - minWorldX) / blockWidthWorld);
            int gridY = Mathf.RoundToInt((maxWorldY - roomWorldPos.y) / blockHeightWorld);

            MinimapNode node = new MinimapNode
            {
                roomReference = room,
                gridPosition = new Vector2Int(gridX, gridY),
                gridSize = roomGridSize
            };

            generatedMapData.Add(node);
        }

        DrawMapUI();
    }

    public void DrawMapUI()
    {
        if (gridParent == null || uiGridPrefab == null) return;

        var children = new List<GameObject>();
        foreach (Transform child in gridParent) children.Add(child.gameObject);
        
        foreach (var child in children)
        {
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        foreach (var node in generatedMapData)
        {
            for (int x = 0; x < node.gridSize.x; x++)
            {
                for (int y = 0; y < node.gridSize.y; y++)
                {
                    GameObject uiCell = Instantiate(uiGridPrefab, gridParent);
                    RectTransform rect = uiCell.GetComponent<RectTransform>();
                    
                    if (rect != null)
                    {
                        rect.pivot = new Vector2(0, 1);
                        rect.anchorMin = new Vector2(0, 1);
                        rect.anchorMax = new Vector2(0, 1);
                        
                        rect.sizeDelta = new Vector2(uiCellSize, uiCellSize);
                        
                        float posX = (node.gridPosition.x + x) * uiCellSize;
                        float posY = -(node.gridPosition.y + y) * uiCellSize;
                        
                        rect.anchoredPosition = new Vector2(posX, posY);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (generatedMapData == null) return;

        float blockWidth = tileUnitRatio.x * cellSizeWorldUnits;
        float blockHeight = tileUnitRatio.y * cellSizeWorldUnits;

        Vector3 drawSize = new Vector3(blockWidth * 0.95f, blockHeight * 0.95f, 1f);

        foreach (var node in generatedMapData)
        {
            if (node.roomReference == null) continue;

            Gizmos.color = Color.cyan; 
            
            for (int x = 0; x < node.gridSize.x; x++)
            {
                for (int y = 0; y < node.gridSize.y; y++)
                {
                    Vector3 basePos = node.roomReference.transform.position;
                    
                    float offX = x * blockWidth;
                    float offY = -y * blockHeight; 

                    Vector3 cellCenter = basePos + new Vector3(offX + blockWidth/2, offY - blockHeight/2, 0);
                    
                    Gizmos.DrawWireCube(cellCenter, drawSize);
                }
            }
        }
    }
#endif
}