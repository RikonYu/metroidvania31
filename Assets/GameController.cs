using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public MCController mc;
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

    [SerializeField] private Transform bulletContainer;

    private void Awake()
    {
        instance = this;
        Rooms = new List<Room>();
        Camps = new List<Camp>();
        AllEnemies = new List<EnemyController>();
        mainCam = camParent.transform.Find("Main Camera").GetComponent<Camera>();
    }
    private void Start()
    {
        foreach (var i in Rooms)
            i.gameObject.SetActive(false);
        ActiveRoom.gameObject.SetActive(true);
    }
    public void ActivateRoom(Room des)
    {
        ActiveRoom.Deactivate();
        ClearBullets();
        ActiveRoom = des;
        des.Activate();
    }

    // Update is called once per frame
    void Update()
    {

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

        Vector3 targetPosition = mc.transform.position;

        float camHalfHeight = mainCam.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCam.aspect;

        float minX = ActiveRoom.roomBounds.min.x + camHalfWidth;
        float maxX = ActiveRoom.roomBounds.max.x - camHalfWidth;
        float minY = ActiveRoom.roomBounds.min.y + camHalfHeight;
        float maxY = ActiveRoom.roomBounds.max.y - camHalfHeight;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

        mainCam.transform.position = new Vector3(clampedX, clampedY, mainCam.transform.position.z);
    }
    public void FireBullet(GameObject prefab, Vector3 startPos, Vector2 dir, bool isEnemy)
    {
        dir = dir.normalized;
        if (prefab == null) return;

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
    }

    public void ReturnBullet(Bullet bullet)
    {
        print("returning");
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
            ReturnBullet(bullet);
        }
    }
}
