using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public Tilemap Tiles;
    public List<EnemyController> Enemies;

    public Bounds roomBounds;
    public bool IsBossRoom;
    public bool IsSpaceRoom;

    public Vector2 cameraCenterOffset;

    private Camera mainCam;
    private Transform playerTransform;

    void Awake()
    {
        GameController.instance.Rooms.Add(this);
        Enemies = new List<EnemyController>();

        if (Tiles != null)
        {
            Tiles.CompressBounds();

            roomBounds = new Bounds(
                Tiles.transform.position + Tiles.localBounds.center,
                Tiles.localBounds.size
            );

            roomBounds.center += (Vector3)cameraCenterOffset;
        }
    }

    public Vector2 GetMinimapSize()
    {
        if (Minimap.instance == null) return Vector2.one;
        return Vector2Int.FloorToInt(roomBounds.size / Minimap.instance.cellSizeWorldUnits);
    }

    private void Start()
    {

    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        Minimap map = FindObjectOfType<Minimap>();
        if (map == null) return;

        float blockWidth = 16f * map.cellSizeWorldUnits;
        float blockHeight = 9f * map.cellSizeWorldUnits;

        float diffX = 0f;
        float diffY = 0f;

        if (Tiles != null)
        {
            float maxTileX = Tiles.transform.position.x + Tiles.localBounds.max.x;
            float maxTileY = Tiles.transform.position.y + Tiles.localBounds.max.y;

            diffX = maxTileX - transform.position.x;
            diffY = maxTileY - transform.position.y;
        }

        int x = Mathf.Max(1, Mathf.CeilToInt(diffX / blockWidth));
        int y = Mathf.Max(1, Mathf.CeilToInt(diffY / blockHeight));

        float finalWidth = x * blockWidth;
        float finalHeight = y * blockHeight;

        Vector3 center = transform.position + new Vector3(finalWidth / 2f, finalHeight / 2f, 0f);
        Vector3 size = new Vector3(finalWidth, finalHeight, 1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}