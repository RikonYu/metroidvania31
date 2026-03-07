using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public Tilemap Tiles;
    public List<EnemyController> Enemies = new List<EnemyController>();

    public Bounds roomBounds;
    public bool IsBossRoom;
    public bool IsSpaceRoom;

    BoxCollider2D roomBound;
    public Vector2 cameraCenterOffset;
    float diffX = 0f;
    float diffY = 0f;

    public GameObject BossBound;

    void Awake()
    {
        float maxTileX = Tiles.transform.position.x + Tiles.localBounds.max.x;
        float maxTileY = Tiles.transform.position.y + Tiles.localBounds.max.y;

        diffX = maxTileX - transform.position.x;
        diffY = maxTileY - transform.position.y;

        diffX = Mathf.Ceil(diffX / 32f) * 32f;
        diffY = Mathf.Ceil(diffY / 18f) * 18f;

        GameController.instance.Rooms.Add(this);
        GameController.instance.RoomBounds[this] = new Rect((Vector2)this.transform.position, new Vector2(diffX, diffY)-Vector2.one*0.1f);

        if (Tiles != null)
        {
            Tiles.CompressBounds();

            roomBounds = new Bounds(
                Tiles.transform.position + Tiles.localBounds.center,
                Tiles.localBounds.size
            );

            roomBounds.center += (Vector3)cameraCenterOffset;
        }
        BossBound.GetComponent<EdgeCollider2D>().points = new Vector2[4] { new Vector2(0, 0), new Vector2(0, diffY), new Vector2(diffX, diffY), new Vector2(diffX, 0) };
        BossBound.SetActive(IsBossRoom);
    }

    public Vector2 GetMinimapSize()
    {
        if (Minimap.instance == null) return Vector2.one;
        return new Vector2Int(Mathf.RoundToInt(diffX / 32f), Mathf.RoundToInt(diffY / 18f));
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (IsBossRoom)
        {
            foreach(var i in Enemies)
            {
                if (i != null && i.gameObject.activeSelf == true)
                    return;
            }
            BossBound.SetActive(false);
        }
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

        float maxTileX = Tiles.transform.position.x + Tiles.localBounds.max.x;
        float maxTileY = Tiles.transform.position.y + Tiles.localBounds.max.y;

        diffX = maxTileX - transform.position.x;
        diffY = maxTileY - transform.position.y;

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