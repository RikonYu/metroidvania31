    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public MCController mc, McPrefab;
    public GameObject camParent;
    Camera mainCam;
    public List<Room> Rooms;
    public Room ActiveRoom;

    public List<Camp> Camps;
    public Camp LastCamp;
    public List<EnemyController> AllEnemies;

    public Interactable InteractingObject;

    private Dictionary<string, Queue<Bullet>> bulletPools = new Dictionary<string, Queue<Bullet>>();
    private HashSet<Bullet> activeBullets = new HashSet<Bullet>();

    public Dictionary<Room, Rect> RoomBounds=new Dictionary<Room, Rect>(); 

    public bool CanDoubleJump;
    public bool CanSlide;

    [SerializeField] private Transform bulletContainer;

    List<GameObject> AcquiredBullets;

    private void Awake()
    {
        instance = this;
        Rooms = new List<Room>();
        Camps = new List<Camp>();
        AcquiredBullets = new List<GameObject>();
        AllEnemies = new List<EnemyController>();
        mainCam = camParent.transform.Find("Main Camera").GetComponent<Camera>();
    }
    private void Start()
    {
        foreach (var i in Rooms)
            i.gameObject.SetActive(false);
        ActiveRoom.gameObject.SetActive(true);
        mc = Instantiate(McPrefab, LastCamp.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        
    }
    public void ActivateRoom(Room des)
    {
        
        if (des.IsBossRoom)
        {
            UIController.instance.ShowBossHP(des.Enemies[0].name, des.Enemies[0].MaxHP);
        }
        else
            UIController.instance.HideBossHP();
        ActiveRoom.Deactivate();
        ClearBullets();
        ActiveRoom = des;
        des.Activate();
        mc.IsInSpace = des.IsSpaceRoom;
    }

    // Update is called once per frame
    void Update()
    {
        foreach(var i in RoomBounds)
        {
            if (i.Value.Contains(mc.transform.position) &&i.Key!=ActiveRoom)
            {
                ActivateRoom(i.Key);
            }
        }
    }
    public void ResetGameState()
    {
        foreach (var i in AllEnemies)
            if (!i.IsBoss)
            {
                i.gameObject.SetActive(true);
                i.Respawn();
            }
        mc.CurrentHealth = mc.MaxHealth;
    }
    public void ResetAggro()
    {
        foreach (var i in AllEnemies)
            if (i.gameObject.activeSelf == true)
            {
                i.ResetAggro();
            }

    }
    public void Die(bool isDropped)
    {
        UIController.instance.ShowLose();
        ClearBullets();
        if (isDropped)
        {

        }
        else
        {
            ResetGameState();
        }
        ResetAggro();
        mc.Respawn(isDropped);
    }
    private void LateUpdate()
    {
        if (ActiveRoom == null || mc == null || mainCam == null) return;

        Vector3 targetPosition = mc.transform.position;

        float camHalfHeight = mainCam.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCam.aspect;

        float minX = ActiveRoom.roomBounds.min.x + camHalfWidth;
        float maxX = ActiveRoom.roomBounds.max.x - camHalfWidth;
        float minY = ActiveRoom.roomBounds.min.y + camHalfHeight;
        float maxY = ActiveRoom.roomBounds.max.y - camHalfHeight;

        float clampedX = minX > maxX ? ActiveRoom.roomBounds.center.x : Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = minY > maxY ? ActiveRoom.roomBounds.center.y : Mathf.Clamp(targetPosition.y, minY, maxY);
        mainCam.transform.position = new Vector3(clampedX, clampedY, mainCam.transform.position.z);
    }
    public Bullet FireBullet(GameObject prefab, Vector3 startPos, Vector2 dir, bool isEnemy)
    {
        dir = dir.normalized;
        if (prefab == null) return null;

        string key = prefab.name;
        Bullet bullet = null;

        if (bulletPools.ContainsKey(key) && bulletPools[key].Count > 0)
        {
            bullet = bulletPools[key].Dequeue();
        }
        else
        {
            GameObject go = Instantiate(prefab, bulletContainer);
            
            bullet = go.GetComponent<Bullet>();
            go.name = key; 
            bullet.PoolKey = key;
        }

        bullet.transform.position = startPos;
        bullet.gameObject.SetActive(true);
        bullet.Init(isEnemy, dir);
        activeBullets.Add(bullet);
        return bullet;
    }

    public void ReturnBullet(Bullet bullet)
    {
        if (!activeBullets.Contains(bullet)) return;

        activeBullets.Remove(bullet);

        string key = bullet.PoolKey;

        if (!bulletPools.ContainsKey(key))
        {
            bulletPools[key] = new Queue<Bullet>();
        }
        bullet.gameObject.SetActive(false);
        bulletPools[key].Enqueue(bullet);
    }

    public void ClearBullets()
    {
        List<Bullet> bulletsToRemove = new List<Bullet>(activeBullets);
        foreach (var bullet in bulletsToRemove)
        {
            if(bullet!=null)
            ReturnBullet(bullet);
        }
    }

    public void PickupBulllet(GameObject blt)
    {
        AcquiredBullets.Add(blt);
        mc.SwapBullet(blt);
    }
}
