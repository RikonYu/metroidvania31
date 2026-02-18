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

    [Header("Target")]
    public Transform player;

    [Header("Settings")]
    public float cellSizeWorldUnits = 2f;
    public float inactiveAlpha = 0.5f;
    public float activeAlpha = 1.0f;

    [Header("Grid Configuration")]
    public Vector2Int tileUnitRatio = new Vector2Int(16, 9);
    
    [Header("UI Settings")]
    public Transform gridParent;
    public GameObject uiGridPrefab;
    public float uiCellSize = 50f;

    [HideInInspector]
    public List<MinimapNode> generatedMapData = new List<MinimapNode>();

    private Dictionary<Vector2Int, Image> gridImages = new Dictionary<Vector2Int, Image>();
    
    private float mapMinX;
    private float mapMaxY;
    private float blockWidthWorld;
    private float blockHeightWorld;
    
    private Vector2Int lastPlayerGridPos = new Vector2Int(-9999, -9999);

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
        
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (GameController.instance != null)
        {
            GenerateMap();
        }
    }

    private void Update()
    {
        UpdatePlayerPosition();
    }

    private void UpdatePlayerPosition()
    {
        if (player == null || gridImages.Count == 0) return;

        int px = Mathf.RoundToInt((player.position.x - mapMinX) / blockWidthWorld);
        int py = Mathf.RoundToInt((mapMaxY - player.position.y) / blockHeightWorld);
        
        Vector2Int currentPlayerGridPos = new Vector2Int(px, py);

        if (currentPlayerGridPos == lastPlayerGridPos) return;

        if (gridImages.TryGetValue(lastPlayerGridPos, out Image lastImg))
        {
            SetImageAlpha(lastImg, inactiveAlpha);
        }

        if (gridImages.TryGetValue(currentPlayerGridPos, out Image currentImg))
        {
            SetImageAlpha(currentImg, activeAlpha);
        }

        lastPlayerGridPos = currentPlayerGridPos;
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
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

        mapMinX = float.MaxValue;
        mapMaxY = float.MinValue;

        foreach (var room in allRooms)
        {
            if (room == null) continue;
            Vector3 pos = room.transform.position;
            if (pos.x < mapMinX) mapMinX = pos.x;
            if (pos.y > mapMaxY) mapMaxY = pos.y;
        }

        blockWidthWorld = tileUnitRatio.x * cellSizeWorldUnits;
        blockHeightWorld = tileUnitRatio.y * cellSizeWorldUnits;

        foreach (var room in allRooms)
        {
            if (room == null) continue;

            Vector3 roomWorldPos = room.transform.position;
            
            float rawWidth = room.roomBounds.size.x / blockWidthWorld;
            float rawHeight = room.roomBounds.size.y / blockHeightWorld;

            int sizeX = Mathf.Max(1, Mathf.RoundToInt(rawWidth));
            int sizeY = Mathf.Max(1, Mathf.RoundToInt(rawHeight));
            Vector2Int roomGridSize = new Vector2Int(sizeX, sizeY);

            int gridX = Mathf.RoundToInt((roomWorldPos.x - mapMinX) / blockWidthWorld);
            int gridY = Mathf.RoundToInt((mapMaxY - roomWorldPos.y) / blockHeightWorld);

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

        gridImages.Clear();
        lastPlayerGridPos = new Vector2Int(-9999, -9999);

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
                    Image img = uiCell.GetComponent<Image>();

                    Vector2Int cellKey = new Vector2Int(node.gridPosition.x + x, node.gridPosition.y + y);

                    if (img != null)
                    {
                        SetImageAlpha(img, inactiveAlpha);
                        if (!gridImages.ContainsKey(cellKey))
                        {
                            gridImages.Add(cellKey, img);
                        }
                    }

                    if (rect != null)
                    {
                        rect.pivot = new Vector2(0, 1);
                        rect.anchorMin = new Vector2(0, 1);
                        rect.anchorMax = new Vector2(0, 1);
                        
                        rect.sizeDelta = new Vector2(uiCellSize, uiCellSize);
                        
                        float posX = cellKey.x * uiCellSize;
                        float posY = -cellKey.y * uiCellSize;
                        
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