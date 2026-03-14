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
    [Range(0f, 1f)] public float exploredRoomAlpha = 0.33f;
    [Range(0f, 1f)] public float currentRoomAlpha = 0.67f;
    [Range(0f, 1f)] public float playerCellAlpha = 1.0f;

    [Header("Grid Configuration")]
    public Vector2Int tileUnitRatio = new Vector2Int(16, 9);
    
    [Header("UI Settings")]
    public Transform gridParent;
    public GameObject uiGridPrefab;
    public float uiCellSize = 50f;

    [HideInInspector]
    public List<MinimapNode> generatedMapData = new List<MinimapNode>();

    private Dictionary<Vector2Int, Image> gridImages = new Dictionary<Vector2Int, Image>();
    private Dictionary<Room, List<Image>> roomImages = new Dictionary<Room, List<Image>>();
    
    private float mapMinX;
    private float mapMaxY;
    private float blockWidthWorld;
    private float blockHeightWorld;
    
    private Vector2Int lastPlayerGridPos = new Vector2Int(-9999, -9999);
    private Room lastPlayerRoom;
    private bool wasMinimapVisible;

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

        player = GameController.instance.mc.transform;

        if (GameController.instance != null)
        {
            GenerateMap();
        }
    }

    private void Update()
    {
        bool isMinimapVisible = IsMinimapVisible();
        if (isMinimapVisible && !wasMinimapVisible)
        {
            RefreshMinimapDisplay();
            lastPlayerGridPos = new Vector2Int(-9999, -9999);
            lastPlayerRoom = null;
        }

        wasMinimapVisible = isMinimapVisible;
        UpdatePlayerPosition();
    }

    private void UpdatePlayerPosition()
    {
        if (player == null || gridImages.Count == 0 || !IsMinimapVisible()) return;

        if (!TryGetPlayerGridPosition(out Vector2Int currentPlayerGridPos, out Room currentRoom))
        {
            return;
        }

        if (currentPlayerGridPos == lastPlayerGridPos && currentRoom == lastPlayerRoom) return;

        RefreshMinimapDisplay(currentRoom, currentPlayerGridPos);

        lastPlayerGridPos = currentPlayerGridPos;
        lastPlayerRoom = currentRoom;
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
        Room[] allRooms = GameController.instance.Rooms.ToArray();
        
        if (allRooms.Length == 0) return;

        generatedMapData.Clear();

        mapMinX = float.MaxValue;
        mapMaxY = float.MinValue;

        foreach (var room in allRooms)
        {
            if (room == null) continue;
            Bounds bounds = room.roomBounds;
            if (bounds.min.x < mapMinX) mapMinX = bounds.min.x;
            if (bounds.max.y > mapMaxY) mapMaxY = bounds.max.y;
        }

        blockWidthWorld = tileUnitRatio.x * cellSizeWorldUnits;
        blockHeightWorld = tileUnitRatio.y * cellSizeWorldUnits;

        foreach (var room in allRooms)
        {
            if (room == null) continue;

            float rawWidth = room.roomBounds.size.x / blockWidthWorld;
            float rawHeight = room.roomBounds.size.y / blockHeightWorld;

            int sizeX = Mathf.Max(1, Mathf.CeilToInt(rawWidth));
            int sizeY = Mathf.Max(1, Mathf.CeilToInt(rawHeight));
            Vector2Int roomGridSize = new Vector2Int(sizeX, sizeY);

            int gridX = Mathf.FloorToInt((room.roomBounds.min.x - mapMinX) / blockWidthWorld);
            int gridY = Mathf.FloorToInt((mapMaxY - room.roomBounds.max.y) / blockHeightWorld);

            //Debug.Log($"{roomWorldPos.x},{roomWorldPos.y}->{gridX},{gridY}, {roomGridSize}");

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
        roomImages.Clear();
        lastPlayerGridPos = new Vector2Int(-9999, -9999);
        lastPlayerRoom = null;

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
                        SetImageAlpha(img, exploredRoomAlpha);
                        if (!gridImages.ContainsKey(cellKey))
                        {
                            gridImages.Add(cellKey, img);
                        }

                        if (node.roomReference != null)
                        {
                            if (!roomImages.TryGetValue(node.roomReference, out List<Image> images))
                            {
                                images = new List<Image>();
                                roomImages.Add(node.roomReference, images);
                            }
                            images.Add(img);
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

        RefreshMinimapDisplay();
    }

    private void RefreshMinimapDisplay()
    {
        if (TryGetPlayerGridPosition(out Vector2Int playerGridPos, out Room playerRoom))
        {
            RefreshMinimapDisplay(playerRoom, playerGridPos);
            return;
        }

        RefreshMinimapDisplay(null, new Vector2Int(-9999, -9999));
    }

    private void RefreshMinimapDisplay(Room highlightedRoom, Vector2Int highlightedCell)
    {
        foreach (var pair in roomImages)
        {
            Room room = pair.Key;
            bool isVisible = room != null && room.Visited;
            float alpha = room == highlightedRoom ? currentRoomAlpha : exploredRoomAlpha;

            foreach (Image img in pair.Value)
            {
                if (img == null) continue;
                img.gameObject.SetActive(isVisible);
                if (isVisible)
                {
                    SetImageAlpha(img, alpha);
                }
            }
        }

        if (highlightedRoom != null &&
            gridImages.TryGetValue(highlightedCell, out Image currentImg) &&
            currentImg != null &&
            currentImg.gameObject.activeSelf)
        {
            SetImageAlpha(currentImg, playerCellAlpha);
        }
    }

    private bool IsMinimapVisible()
    {
        return gridParent != null && gridParent.gameObject.activeInHierarchy;
    }

    private bool TryGetPlayerGridPosition(out Vector2Int playerGridPos, out Room playerRoom)
    {
        playerGridPos = default;
        playerRoom = null;

        if (player == null)
        {
            return false;
        }

        MinimapNode currentNode = null;
        Room activeRoom = GameController.instance != null ? GameController.instance.ActiveRoom : null;

        if (activeRoom != null)
        {
            currentNode = generatedMapData.Find(node => node.roomReference == activeRoom);
        }

        if (currentNode == null)
        {
            foreach (MinimapNode node in generatedMapData)
            {
                if (node.roomReference != null && node.roomReference.roomBounds.Contains(player.position))
                {
                    currentNode = node;
                    break;
                }
            }
        }

        if (currentNode == null || currentNode.roomReference == null)
        {
            return false;
        }

        playerRoom = currentNode.roomReference;

        Bounds roomBounds = currentNode.roomReference.roomBounds;
        int localX = Mathf.FloorToInt((player.position.x - roomBounds.min.x) / blockWidthWorld);
        int localY = Mathf.FloorToInt((roomBounds.max.y - player.position.y) / blockHeightWorld);

        localX = Mathf.Clamp(localX, 0, currentNode.gridSize.x - 1);
        localY = Mathf.Clamp(localY, 0, currentNode.gridSize.y - 1);

        playerGridPos = new Vector2Int(
            currentNode.gridPosition.x + localX,
            currentNode.gridPosition.y + localY
        );
        return true;
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
