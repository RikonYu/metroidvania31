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

        if (bossLockedDoorRoom != null)
        {
            EnsurePlayerInsideClosedDoors(bossLockedDoorRoom);

            if (AreAllBossesDefeated(bossLockedDoorRoom))
            {
                OpenBossLockedDoors();
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
        OpenBossLockedDoors();
        Door.OpenAllDoors();
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

    public void ResolvePlayerDoorOverlap(Room room)
    {
        EnsurePlayerInsideClosedDoors(room);
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
        if (playerCollider == null)
        {
            return;
        }

        List<BoxCollider2D> doorColliders = GetClosedDoorColliders(room);
        if (doorColliders.Count == 0 || !IsOverlappingClosedDoor(playerCollider, doorColliders))
        {
            return;
        }

        Rigidbody2D playerRb = mc.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
        }

        Vector2 current = mc.transform.position;
        Vector2 extents = playerCollider.bounds.extents;
        current = ClampPointInsideRoom(room, current, extents);
        mc.transform.position = new Vector3(current.x, current.y, mc.transform.position.z);
        Physics2D.SyncTransforms();

        const int maxIterations = 24;
        const float separationPadding = 0.02f;
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            if (!IsOverlappingClosedDoor(playerCollider, doorColliders))
            {
                return;
            }

            Vector2 correction = Vector2.zero;
            int overlapCount = 0;

            for (int i = 0; i < doorColliders.Count; i++)
            {
                BoxCollider2D doorCollider = doorColliders[i];
                if (doorCollider == null || !doorCollider.enabled)
                {
                    continue;
                }

                ColliderDistance2D distance = playerCollider.Distance(doorCollider);
                if (!distance.isOverlapped)
                {
                    continue;
                }

                Vector2 normal = distance.normal;
                if (normal.sqrMagnitude <= 0.0001f)
                {
                    normal = ((Vector2)mc.transform.position - (Vector2)room.roomBounds.center).normalized;
                    if (normal.sqrMagnitude <= 0.0001f)
                    {
                        normal = Vector2.up;
                    }
                }

                correction += -normal.normalized * (Mathf.Abs(distance.distance) + separationPadding);
                overlapCount++;
            }

            if (overlapCount <= 0)
            {
                return;
            }

            correction /= overlapCount;
            if (correction.sqrMagnitude <= 0.000001f)
            {
                correction = ((Vector2)room.roomBounds.center - (Vector2)mc.transform.position).normalized * 0.05f;
            }

            current = (Vector2)mc.transform.position + correction;
            current = ClampPointInsideRoom(room, current, extents);
            mc.transform.position = new Vector3(current.x, current.y, mc.transform.position.z);
            Physics2D.SyncTransforms();
        }

        if (TryFindSafePointInRoom(room, playerCollider, doorColliders, extents, out Vector2 safePoint))
        {
            mc.transform.position = new Vector3(safePoint.x, safePoint.y, mc.transform.position.z);
            Physics2D.SyncTransforms();
        }
    }

    private bool IsOverlappingClosedDoor(Collider2D playerCollider, List<BoxCollider2D> doorColliders)
    {
        if (playerCollider == null || doorColliders == null)
        {
            return false;
        }

        for (int i = 0; i < doorColliders.Count; i++)
        {
            BoxCollider2D doorCollider = doorColliders[i];
            if (doorCollider == null || !doorCollider.enabled || doorCollider.isTrigger)
            {
                continue;
            }

            ColliderDistance2D distance = playerCollider.Distance(doorCollider);
            if (distance.isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    private List<BoxCollider2D> GetClosedDoorColliders(Room room)
    {
        List<BoxCollider2D> colliders = new List<BoxCollider2D>();
        if (room == null)
        {
            return colliders;
        }

        Door[] doors = room.GetComponentsInChildren<Door>(true);
        for (int i = 0; i < doors.Length; i++)
        {
            Door door = doors[i];
            if (door == null)
            {
                continue;
            }

            BoxCollider2D doorCollider = door.GetComponent<BoxCollider2D>();
            if (doorCollider != null && doorCollider.enabled && !doorCollider.isTrigger)
            {
                colliders.Add(doorCollider);
            }
        }

        return colliders;
    }

    private bool TryFindSafePointInRoom(
        Room room,
        Collider2D playerCollider,
        List<BoxCollider2D> doorColliders,
        Vector2 playerExtents,
        out Vector2 safePoint)
    {
        safePoint = room != null ? (Vector2)room.roomBounds.center : Vector2.zero;
        if (room == null || playerCollider == null)
        {
            return false;
        }

        Vector2 center = ClampPointInsideRoom(room, room.roomBounds.center, playerExtents);
        if (IsCandidatePointSafe(room, playerCollider, doorColliders, playerExtents, center))
        {
            safePoint = center;
            return true;
        }

        float maxRadius = Mathf.Max(1f, Mathf.Max(room.roomBounds.extents.x, room.roomBounds.extents.y));
        const float radiusStep = 0.35f;
        const int angleSamples = 20;

        for (float radius = radiusStep; radius <= maxRadius; radius += radiusStep)
        {
            for (int i = 0; i < angleSamples; i++)
            {
                float angle = (360f / angleSamples) * i;
                Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                Vector2 candidate = center + offset;

                if (IsCandidatePointSafe(room, playerCollider, doorColliders, playerExtents, candidate))
                {
                    safePoint = ClampPointInsideRoom(room, candidate, playerExtents);
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsCandidatePointSafe(
        Room room,
        Collider2D playerCollider,
        List<BoxCollider2D> doorColliders,
        Vector2 playerExtents,
        Vector2 candidate)
    {
        Vector2 clamped = ClampPointInsideRoom(room, candidate, playerExtents);
        mc.transform.position = new Vector3(clamped.x, clamped.y, mc.transform.position.z);
        Physics2D.SyncTransforms();
        return !IsOverlappingClosedDoor(playerCollider, doorColliders);
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
