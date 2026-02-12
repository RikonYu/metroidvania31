using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public Tilemap Tiles;
    public List<EnemyController> Enemies;

    public Bounds roomBounds;
    public bool IsBossRoom;
    private Camera mainCam;
    private Transform playerTransform;

    void Awake()
    {
        GameController.instance.Rooms.Add(this);
        Enemies = new List<EnemyController>();
        roomBounds = Tiles.localBounds;
        roomBounds.center += transform.position;

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
}