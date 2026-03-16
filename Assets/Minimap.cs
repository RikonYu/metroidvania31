using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class GeneratedMapData
{
    public Room room;
    public Vector2Int origin;
    public Vector2Int size;
}

public class Minimap : MonoBehaviour
{
    public static Minimap instance;

    public Transform player;
    public float cellSizeWorldUnits = 2f;
    [Range(0f, 1f)] public float exploredRoomAlpha = 0.33f;
    [Range(0f, 1f)] public float currentRoomAlpha = 0.67f;
    [Range(0f, 1f)] public float playerCellAlpha = 1f;
    public Vector2 tileUnitRatio = new Vector2(16f, 9f);
    public RectTransform gridParent;
    public GameObject uiGridPrefab;
    public float uiCellSize = 50f;
    public List<GeneratedMapData> generatedMapData = new List<GeneratedMapData>();

    private class MapCellVisual
    {
        public Room room;
        public Image image;
        public RectTransform rect;
    }

    private readonly Dictionary<Vector2Int, MapCellVisual> cellVisuals = new Dictionary<Vector2Int, MapCellVisual>();
    private readonly Dictionary<Room, GeneratedMapData> roomDataLookup = new Dictionary<Room, GeneratedMapData>();
    private readonly Dictionary<Vector2Int, Camp> campByCell = new Dictionary<Vector2Int, Camp>();
    private readonly List<Image> saveIcons = new List<Image>();

    private Image chosenIconImage;
    private Vector2 mapWorldMin;
    private Vector2 cellWorldSize;
    private Vector2Int mapSizeCells = Vector2Int.one;
    private Vector2Int focusedCell;
    private Vector2Int playerCell;
    private bool hasFocus;
    private bool mapBuilt;
    private bool isSessionOpen;
    private bool teleportEnabledForCurrentOpen;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        EnsurePlayerReference();

        if (!IsMinimapVisible())
        {
            isSessionOpen = false;
            return;
        }

        if (!isSessionOpen)
        {
            OnMinimapShown();
        }

        HandleFocusInput();
        RefreshCells();
        HandleTeleportInput();
    }

    public void OnMinimapShown()
    {
        EnsurePlayerReference();
        EnsureMapBuilt();

        teleportEnabledForCurrentOpen = GameController.instance != null && GameController.instance.InteractingObject is Camp;
        isSessionOpen = true;

        if (!TryGetPlayerCell(out playerCell))
        {
            playerCell = GetFallbackFocusCell();
        }

        focusedCell = playerCell;
        hasFocus = true;

        RebuildCampIcons();
        RefreshCells();
        UpdateChosenIcon();
        CenterFocusedCell();
    }

    public void OnMinimapHidden()
    {
        isSessionOpen = false;
        teleportEnabledForCurrentOpen = false;
        if (gridParent != null)
        {
            gridParent.anchoredPosition = Vector2.zero;
        }
    }

    private void EnsurePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        if (GameController.instance != null && GameController.instance.mc != null)
        {
            player = GameController.instance.mc.transform;
        }
    }

    private bool IsMinimapVisible()
    {
        if (UIController.instance == null || UIController.instance.MinimapBG == null || gridParent == null)
        {
            return false;
        }

        return UIController.instance.MinimapBG.activeSelf && gridParent.gameObject.activeSelf;
    }

    private void EnsureMapBuilt()
    {
        if (mapBuilt && cellVisuals.Count > 0)
        {
            return;
        }

        BuildMap();
    }

    private void BuildMap()
    {
        mapBuilt = false;
        generatedMapData.Clear();
        roomDataLookup.Clear();
        cellVisuals.Clear();
        campByCell.Clear();
        mapSizeCells = Vector2Int.one;
        hasFocus = false;

        if (gridParent == null || uiGridPrefab == null)
        {
            return;
        }

        ClearGridChildren();
        chosenIconImage = null;

        List<Room> rooms = CollectRooms();
        if (rooms.Count == 0)
        {
            return;
        }

        cellWorldSize = GetCellWorldSize();
        if (cellWorldSize.x <= 0.0001f || cellWorldSize.y <= 0.0001f)
        {
            return;
        }

        mapWorldMin = new Vector2(float.MaxValue, float.MaxValue);
        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (room == null)
            {
                continue;
            }

            Vector3 roomPos = room.transform.position;
            if (roomPos.x < mapWorldMin.x)
            {
                mapWorldMin.x = roomPos.x;
            }
            if (roomPos.y < mapWorldMin.y)
            {
                mapWorldMin.y = roomPos.y;
            }
        }

        int maxX = 0;
        int maxY = 0;
        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (room == null)
            {
                continue;
            }

            Vector2Int roomSize = GetRoomCellSize(room);
            Vector3 roomPos = room.transform.position;
            int originX = Mathf.RoundToInt((roomPos.x - mapWorldMin.x) / cellWorldSize.x);
            int originY = Mathf.RoundToInt((roomPos.y - mapWorldMin.y) / cellWorldSize.y);

            GeneratedMapData data = new GeneratedMapData
            {
                room = room,
                origin = new Vector2Int(originX, originY),
                size = roomSize
            };

            generatedMapData.Add(data);
            roomDataLookup[room] = data;

            maxX = Mathf.Max(maxX, originX + roomSize.x);
            maxY = Mathf.Max(maxY, originY + roomSize.y);
        }

        mapSizeCells = new Vector2Int(Mathf.Max(1, maxX), Mathf.Max(1, maxY));

        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        for (int i = 0; i < generatedMapData.Count; i++)
        {
            GeneratedMapData data = generatedMapData[i];
            if (data.room == null)
            {
                continue;
            }

            for (int y = 0; y < data.size.y; y++)
            {
                for (int x = 0; x < data.size.x; x++)
                {
                    Vector2Int cell = data.origin + new Vector2Int(x, y);
                    if (!occupiedCells.Add(cell))
                    {
                        continue;
                    }

                    CreateCellVisual(cell, data.room);
                }
            }
        }

        CreateOrUpdateChosenIcon();
        mapBuilt = true;
    }

    private List<Room> CollectRooms()
    {
        List<Room> rooms = new List<Room>();
        if (GameController.instance != null && GameController.instance.Rooms != null)
        {
            for (int i = 0; i < GameController.instance.Rooms.Count; i++)
            {
                Room room = GameController.instance.Rooms[i];
                if (room != null)
                {
                    rooms.Add(room);
                }
            }
        }

        if (rooms.Count == 0)
        {
            Room[] sceneRooms = FindObjectsOfType<Room>(true);
            for (int i = 0; i < sceneRooms.Length; i++)
            {
                Room room = sceneRooms[i];
                if (room != null)
                {
                    rooms.Add(room);
                }
            }
        }

        return rooms;
    }

    private Vector2 GetCellWorldSize()
    {
        float width = Mathf.Abs(tileUnitRatio.x) * Mathf.Max(0.0001f, cellSizeWorldUnits);
        float height = Mathf.Abs(tileUnitRatio.y) * Mathf.Max(0.0001f, cellSizeWorldUnits);
        return new Vector2(width, height);
    }

    private Vector2Int GetRoomCellSize(Room room)
    {
        if (room == null)
        {
            return Vector2Int.one;
        }

        Vector2 size = room.GetMinimapSize();
        int width = Mathf.Max(1, Mathf.RoundToInt(size.x));
        int height = Mathf.Max(1, Mathf.RoundToInt(size.y));
        return new Vector2Int(width, height);
    }

    private void ClearGridChildren()
    {
        if (gridParent == null)
        {
            return;
        }

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = gridParent.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void CreateCellVisual(Vector2Int cell, Room room)
    {
        GameObject cellObject = Instantiate(uiGridPrefab, gridParent);
        cellObject.name = $"Cell_{cell.x}_{cell.y}";

        RectTransform rect = cellObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = cellObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;
        rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(uiCellSize, uiCellSize);
        rect.anchoredPosition = CellToAnchoredPosition(cell);

        Image image = cellObject.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
            Color color = image.color;
            color.a = 0f;
            image.color = color;
        }

        cellVisuals[cell] = new MapCellVisual
        {
            room = room,
            image = image,
            rect = rect
        };
    }

    private void CreateOrUpdateChosenIcon()
    {
        UIController ui = UIController.instance;
        if (gridParent == null || ui == null)
        {
            return;
        }

        if (chosenIconImage == null)
        {
            GameObject iconObj = new GameObject("ChosenIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(gridParent, false);
            chosenIconImage = iconObj.GetComponent<Image>();
            chosenIconImage.raycastTarget = false;
        }

        RectTransform rect = chosenIconImage.rectTransform;
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;
        rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(uiCellSize, uiCellSize);
        chosenIconImage.sprite = ui.ChosenIcon;
        chosenIconImage.color = Color.white;
        chosenIconImage.gameObject.SetActive(hasFocus);
        chosenIconImage.transform.SetAsLastSibling();
    }

    private void UpdateChosenIcon()
    {
        CreateOrUpdateChosenIcon();
        if (chosenIconImage == null)
        {
            return;
        }

        chosenIconImage.gameObject.SetActive(hasFocus);
        if (!hasFocus)
        {
            return;
        }

        chosenIconImage.rectTransform.anchoredPosition = CellToAnchoredPosition(focusedCell);
        chosenIconImage.transform.SetAsLastSibling();
    }

    private void RebuildCampIcons()
    {
        campByCell.Clear();
        for (int i = saveIcons.Count - 1; i >= 0; i--)
        {
            Image image = saveIcons[i];
            if (image != null)
            {
                Destroy(image.gameObject);
            }
        }
        saveIcons.Clear();

        UIController ui = UIController.instance;
        if (ui == null || ui.SaveIcon == null || gridParent == null)
        {
            return;
        }

        if (GameController.instance == null || GameController.instance.Camps == null)
        {
            return;
        }

        float iconSize = uiCellSize * 0.5f;
        for (int i = 0; i < GameController.instance.Camps.Count; i++)
        {
            Camp camp = GameController.instance.Camps[i];
            if (camp == null || !camp.IsVisited || IsExcludedCamp(camp))
            {
                continue;
            }

            if (!TryWorldToCell(camp.transform.position, out Vector2Int cell))
            {
                continue;
            }

            cell = ClampCell(cell);
            if (campByCell.ContainsKey(cell))
            {
                continue;
            }

            campByCell[cell] = camp;

            GameObject iconObj = new GameObject($"CampIcon_{cell.x}_{cell.y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(gridParent, false);

            Image iconImage = iconObj.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.sprite = ui.SaveIcon;
            iconImage.color = Color.white;

            RectTransform rect = iconImage.rectTransform;
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(iconSize, iconSize);
            rect.anchoredPosition = CellToAnchoredPosition(cell);

            saveIcons.Add(iconImage);
        }

        if (chosenIconImage != null)
        {
            chosenIconImage.transform.SetAsLastSibling();
        }
    }

    private bool IsExcludedCamp(Camp camp)
    {
        if (camp == null)
        {
            return true;
        }

        string campName = camp.gameObject.name;
        const string cloneSuffix = "(Clone)";
        if (campName.EndsWith(cloneSuffix))
        {
            campName = campName.Substring(0, campName.Length - cloneSuffix.Length).Trim();
        }

        return string.Equals(campName, "testcamp", System.StringComparison.OrdinalIgnoreCase);
    }

    private void HandleFocusInput()
    {
        if (!hasFocus)
        {
            return;
        }

        Vector2Int delta = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            delta.x = -1;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            delta.x = 1;
        }
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            delta.y = 1;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            delta.y = -1;
        }

        if (delta == Vector2Int.zero)
        {
            return;
        }

        focusedCell = ClampCell(focusedCell + delta);
        UpdateChosenIcon();
        CenterFocusedCell();
    }

    private void CenterFocusedCell()
    {
        if (gridParent == null || !hasFocus)
        {
            return;
        }

        gridParent.anchoredPosition = -CellToAnchoredPosition(focusedCell);
    }

    private void HandleTeleportInput()
    {
        if (!teleportEnabledForCurrentOpen || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (!campByCell.TryGetValue(focusedCell, out Camp camp) || camp == null)
        {
            return;
        }

        if (GameController.instance != null && GameController.instance.TryTeleportToCampFromMinimap(camp))
        {
            UIController.instance?.HideMinimap();
        }
    }

    private void RefreshCells()
    {
        if (!mapBuilt)
        {
            return;
        }

        TryGetPlayerCell(out playerCell);
        Room activeRoom = GameController.instance != null ? GameController.instance.ActiveRoom : null;

        foreach (KeyValuePair<Vector2Int, MapCellVisual> entry in cellVisuals)
        {
            MapCellVisual visual = entry.Value;
            if (visual == null || visual.image == null)
            {
                continue;
            }

            float alpha = 0f;
            Room room = visual.room;
            if (room != null && room.Visited)
            {
                alpha = exploredRoomAlpha;
                if (room == activeRoom)
                {
                    alpha = currentRoomAlpha;
                }
            }

            if (entry.Key == playerCell)
            {
                alpha = playerCellAlpha;
            }

            Color color = visual.image.color;
            color.a = Mathf.Clamp01(alpha);
            visual.image.color = color;
        }

        UpdateChosenIcon();
    }

    private bool TryGetPlayerCell(out Vector2Int cell)
    {
        cell = GetFallbackFocusCell();
        if (player == null)
        {
            return false;
        }

        if (TryWorldToCell(player.position, out Vector2Int mapped))
        {
            cell = ClampCell(mapped);
            return true;
        }

        return false;
    }

    private Vector2Int GetFallbackFocusCell()
    {
        Vector2Int fallback = new Vector2Int(Mathf.Max(0, mapSizeCells.x / 2), Mathf.Max(0, mapSizeCells.y / 2));
        Room activeRoom = GameController.instance != null ? GameController.instance.ActiveRoom : null;
        if (activeRoom != null && roomDataLookup.TryGetValue(activeRoom, out GeneratedMapData data))
        {
            fallback = data.origin + new Vector2Int(Mathf.Max(0, data.size.x / 2), Mathf.Max(0, data.size.y / 2));
        }

        return ClampCell(fallback);
    }

    private bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
    {
        if (cellWorldSize.x <= 0.0001f || cellWorldSize.y <= 0.0001f)
        {
            cell = Vector2Int.zero;
            return false;
        }

        if (GameController.instance != null && GameController.instance.RoomBounds != null)
        {
            foreach (KeyValuePair<Room, Rect> pair in GameController.instance.RoomBounds)
            {
                Room room = pair.Key;
                if (room == null || !pair.Value.Contains(worldPosition))
                {
                    continue;
                }

                if (!roomDataLookup.TryGetValue(room, out GeneratedMapData data))
                {
                    continue;
                }

                int localX = Mathf.FloorToInt((worldPosition.x - room.transform.position.x) / cellWorldSize.x);
                int localY = Mathf.FloorToInt((worldPosition.y - room.transform.position.y) / cellWorldSize.y);
                localX = Mathf.Clamp(localX, 0, Mathf.Max(0, data.size.x - 1));
                localY = Mathf.Clamp(localY, 0, Mathf.Max(0, data.size.y - 1));
                cell = data.origin + new Vector2Int(localX, localY);
                return true;
            }
        }

        int x = Mathf.FloorToInt((worldPosition.x - mapWorldMin.x) / cellWorldSize.x);
        int y = Mathf.FloorToInt((worldPosition.y - mapWorldMin.y) / cellWorldSize.y);
        cell = new Vector2Int(x, y);
        return true;
    }

    private Vector2Int ClampCell(Vector2Int cell)
    {
        int x = Mathf.Clamp(cell.x, 0, Mathf.Max(0, mapSizeCells.x - 1));
        int y = Mathf.Clamp(cell.y, 0, Mathf.Max(0, mapSizeCells.y - 1));
        return new Vector2Int(x, y);
    }

    private Vector2 CellToAnchoredPosition(Vector2Int cell)
    {
        float centerOffsetX = (mapSizeCells.x - 1) * 0.5f;
        float centerOffsetY = (mapSizeCells.y - 1) * 0.5f;
        float x = (cell.x - centerOffsetX) * uiCellSize;
        float y = (cell.y - centerOffsetY) * uiCellSize;
        return new Vector2(x, y);
    }
}
