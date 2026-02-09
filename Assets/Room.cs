using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public Tilemap Tiles;
    public GameObject Enemies;

    public Bounds roomBounds;
    private Camera mainCam;
    private Transform playerTransform;

    void Awake()
    {
        GameController.instance.Rooms.Add(this);

        roomBounds = Tiles.localBounds;
        roomBounds.center += transform.position;

    }
    public void Deactivate()
    {

    }

    public void Activate()
    {

    }
}