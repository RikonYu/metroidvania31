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
    [SerializeField] private float bossIntroDuration = 1.1f;

    List<GameObject> AcquiredBullets;
    private Coroutine bossIntroCoroutine;
    private Room bossIntroRoom;
    private Room bossLockedDoorRoom;
    private readonly List<Door> bossClosedDoors = new List<Door>();

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
        StopBossIntroSequence();

        ActiveRoom.Deactivate();
        ClearBullets();
        ActiveRoom = des;
        des.Activate();
        mc.IsInSpace = des.IsSpaceRoom;

        if (des.IsBossRoom)
        {
            EnemyController boss = GetFirstAliveBoss(des);
            if (boss != null)
            {
                CloseBossRoomDoors(des);
                UIController.instance.ShowBossHP(boss.name, boss.MaxHP);
                UIController.instance.SetBossHPRevealScale(0f);
                SetBossCombatEnabled(des, false);
                bossIntroCoroutine = StartCoroutine(BossIntroRoutine(des));
                return;
            }

            if (bossLockedDoorRoom == des)
            {
                OpenBossLockedDoors();
            }
        }

        UIController.instance.HideBossHP();
        mc?.SetControlLocked(false);
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

        if (bossLockedDoorRoom != null && AreAllBossesDefeated(bossLockedDoorRoom))
        {
            OpenBossLockedDoors();
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
        if (blt == null)
        {
            return;
        }

        if (!AcquiredBullets.Contains(blt))
        {
            AcquiredBullets.Add(blt);
        }

        if (mc != null)
        {
            mc.AcquireWeapon(blt);
        }
    }

    private IEnumerator BossIntroRoutine(Room room)
    {
        bossIntroRoom = room;

        if (mc != null)
        {
            mc.SetControlLocked(true);
        }

        if (Shaker.instance != null)
        {
            Shaker.instance.StartBossIntroShake();
        }

        float duration = Mathf.Max(0.01f, bossIntroDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ActiveRoom != room)
            {
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            UIController.instance?.SetBossHPRevealScale(t);
            yield return null;
        }

        UIController.instance?.SetBossHPRevealScale(1f);

        if (Shaker.instance != null)
        {
            Shaker.instance.StopBossIntroShake();
        }

        if (ActiveRoom == room)
        {
            SetBossCombatEnabled(room, true);
        }

        if (mc != null)
        {
            mc.SetControlLocked(false);
        }

        bossIntroCoroutine = null;
        bossIntroRoom = null;
    }

    private void StopBossIntroSequence()
    {
        if (bossIntroCoroutine != null)
        {
            StopCoroutine(bossIntroCoroutine);
            bossIntroCoroutine = null;
        }

        if (Shaker.instance != null)
        {
            Shaker.instance.StopBossIntroShake();
        }

        if (mc != null)
        {
            mc.SetControlLocked(false);
        }

        if (bossIntroRoom != null)
        {
            SetBossCombatEnabled(bossIntroRoom, true);
            bossIntroRoom = null;
        }
    }

    private EnemyController GetFirstAliveBoss(Room room)
    {
        if (room == null || room.Enemies == null)
        {
            return null;
        }

        for (int i = 0; i < room.Enemies.Count; i++)
        {
            EnemyController enemy = room.Enemies[i];
            if (enemy != null && enemy.IsBoss && enemy.gameObject.activeInHierarchy)
            {
                return enemy;
            }
        }

        return null;
    }

    private void SetBossCombatEnabled(Room room, bool enabled)
    {
        if (room == null || room.Enemies == null)
        {
            return;
        }

        for (int i = 0; i < room.Enemies.Count; i++)
        {
            EnemyController enemy = room.Enemies[i];
            if (enemy != null && enemy.IsBoss)
            {
                enemy.SetCombatEnabled(enabled);
            }
        }
    }

    private void CloseBossRoomDoors(Room room)
    {
        if (room == null)
        {
            return;
        }

        if (bossLockedDoorRoom == room)
        {
            EnsurePlayerInsideClosedDoors(room);
            return;
        }

        bossLockedDoorRoom = room;
        bossClosedDoors.Clear();

        Door[] doors = room.GetComponentsInChildren<Door>(true);
        for (int i = 0; i < doors.Length; i++)
        {
            Door door = doors[i];
            if (door == null)
            {
                continue;
            }

            bool wasOpen = IsDoorOpen(door);
            door.Close();

            if (wasOpen)
            {
                bossClosedDoors.Add(door);
            }
        }

        EnsurePlayerInsideClosedDoors(room);
    }

    private void OpenBossLockedDoors()
    {
        for (int i = 0; i < bossClosedDoors.Count; i++)
        {
            Door door = bossClosedDoors[i];
            if (door != null)
            {
                door.Open();
            }
        }

        bossClosedDoors.Clear();
        bossLockedDoorRoom = null;
    }

    private bool IsDoorOpen(Door door)
    {
        if (door == null)
        {
            return false;
        }

        BoxCollider2D doorCollider = door.GetComponent<BoxCollider2D>();
        if (doorCollider == null)
        {
            return true;
        }

        return !doorCollider.enabled;
    }

    private bool AreAllBossesDefeated(Room room)
    {
        if (room == null || room.Enemies == null)
        {
            return false;
        }

        bool hasBoss = false;
        for (int i = 0; i < room.Enemies.Count; i++)
        {
            EnemyController enemy = room.Enemies[i];
            if (enemy == null || !enemy.IsBoss)
            {
                continue;
            }

            hasBoss = true;
            if (enemy.CurrentHP > 0f)
            {
                return false;
            }
        }

        return hasBoss;
    }

    private void EnsurePlayerInsideClosedDoors(Room room)
    {
        if (room == null || mc == null)
        {
            return;
        }

        Collider2D playerCollider = mc.GetComponent<Collider2D>();
        if (playerCollider == null || !IsOverlappingClosedDoor(room, playerCollider))
        {
            return;
        }

        Rigidbody2D playerRb = mc.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
        }

        Vector2 current = mc.transform.position;
        Vector2 center = room.roomBounds.center;
        Vector2 direction = center - current;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        direction.Normalize();
        Vector2 extents = playerCollider.bounds.extents;

        const int maxSteps = 160;
        const float stepSize = 0.2f;
        for (int i = 0; i < maxSteps; i++)
        {
            current += direction * stepSize;
            current = ClampPointInsideRoom(room, current, extents);
            mc.transform.position = new Vector3(current.x, current.y, mc.transform.position.z);
            Physics2D.SyncTransforms();

            if (!IsOverlappingClosedDoor(room, playerCollider))
            {
                return;
            }
        }

        Vector2 fallback = ClampPointInsideRoom(room, center, extents);
        mc.transform.position = new Vector3(fallback.x, fallback.y, mc.transform.position.z);
        Physics2D.SyncTransforms();
    }

    private bool IsOverlappingClosedDoor(Room room, Collider2D playerCollider)
    {
        if (room == null || playerCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Door[] doors = room.GetComponentsInChildren<Door>(true);
        for (int i = 0; i < doors.Length; i++)
        {
            Door door = doors[i];
            if (door == null)
            {
                continue;
            }

            BoxCollider2D doorCollider = door.GetComponent<BoxCollider2D>();
            if (doorCollider == null || !doorCollider.enabled || doorCollider.isTrigger)
            {
                continue;
            }

            if (doorCollider.bounds.Intersects(playerBounds))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 ClampPointInsideRoom(Room room, Vector2 point, Vector2 playerExtents)
    {
        if (room == null)
        {
            return point;
        }

        const float padding = 0.05f;
        Bounds bounds = room.roomBounds;

        float minX = bounds.min.x + playerExtents.x + padding;
        float maxX = bounds.max.x - playerExtents.x - padding;
        float minY = bounds.min.y + playerExtents.y + padding;
        float maxY = bounds.max.y - playerExtents.y - padding;

        float clampedX = minX > maxX ? bounds.center.x : Mathf.Clamp(point.x, minX, maxX);
        float clampedY = minY > maxY ? bounds.center.y : Mathf.Clamp(point.y, minY, maxY);
        return new Vector2(clampedX, clampedY);
    }
}
