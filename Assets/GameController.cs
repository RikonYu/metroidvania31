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
    [SerializeField] private float bossIntroCameraDuration = 2.2f;
    [SerializeField] private float deathFadeOutDuration = 0.6f;
    [SerializeField] private float deathFadeInDuration = 0.7f;
    [SerializeField] private float droppedRespawnDelay = 1.2f;

    List<GameObject> AcquiredBullets;
    private Coroutine bossIntroCoroutine;
    private Room bossIntroRoom;
    private bool bossIntroCameraOverrideActive;
    private Vector3 bossIntroCameraOverridePosition;
    private bool deathCameraOverrideActive;
    private Vector3 deathCameraOverridePosition;
    private Coroutine deathRoutine;
    private Coroutine campTeleportRoutine;
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
                UIController.instance.HideBossHP();
                SetBossCombatEnabled(des, false);
                bossIntroCoroutine = StartCoroutine(BossIntroRoutine(des, boss));
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
        HashSet<GameObject> spawnerManagedEnemies = GetAllSpawnerManagedEnemies();

        for (int i = AllEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = AllEnemies[i];
            if (enemy == null)
            {
                AllEnemies.RemoveAt(i);
                continue;
            }

            if (enemy.DestroyOnEncounterReset)
            {
                DestroyRuntimeEnemy(enemy);
                continue;
            }

            if (ShouldSkipEnemyForGlobalReset(enemy, spawnerManagedEnemies))
            {
                continue;
            }

            enemy.gameObject.SetActive(true);
            enemy.Respawn();
        }

        mc.CurrentHealth = mc.MaxHealth;
        mc.CurrentEnergy = mc.MaxEnergy;
    }

    private bool ShouldSkipEnemyForGlobalReset(EnemyController enemy, HashSet<GameObject> spawnerManagedEnemies)
    {
        if (enemy == null)
        {
            return true;
        }

        if (enemy.IsBoss)
        {
            return true;
        }

        if (spawnerManagedEnemies != null && spawnerManagedEnemies.Contains(enemy.gameObject))
        {
            return true;
        }

        string enemyTag = enemy.gameObject.tag;
        if (string.Equals(enemyTag, "elite", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string enemyName = enemy.gameObject.name;
        return enemyName.IndexOf("elite", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
        if (deathRoutine != null || campTeleportRoutine != null)
        {
            return;
        }

        deathRoutine = StartCoroutine(DeathRoutine(isDropped));
    }

    private IEnumerator DeathRoutine(bool isDropped)
    {
        UIController.instance?.ShowLose();
        UIController.instance?.SetBlackBgAlpha(0f);
        StopBossIntroSequence();
        ClearAllProjectiles();

        if (mc != null)
        {
            mc.SetControlLocked(true);
            Rigidbody2D playerRb = mc.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
            }
        }

        SetDeathCameraOverride(mainCam != null ? mainCam.transform.position : Vector3.zero);

        if (isDropped)
        {
            float waitTime = Mathf.Max(0f, droppedRespawnDelay);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            ClearAllProjectiles();
            ExecuteDeathRespawn(isDropped);
            SnapCameraToPlayer();
            yield return null;
        }
        else
        {
            yield return StartCoroutine(FadeBlack(0f, 1f, deathFadeOutDuration));
            ClearAllProjectiles();
            ExecuteDeathRespawn(isDropped);
            SnapCameraToPlayer();
            yield return StartCoroutine(FadeBlack(1f, 0f, deathFadeInDuration));
        }

        ClearDeathCameraOverride();
        if (mc != null)
        {
            mc.SetControlLocked(false);
        }

        deathRoutine = null;
    }

    public bool TryTeleportToCampFromMinimap(Camp targetCamp)
    {
        if (targetCamp == null || campTeleportRoutine != null || deathRoutine != null)
        {
            return false;
        }

        campTeleportRoutine = StartCoroutine(CampTeleportRoutine(targetCamp));
        return true;
    }

    private IEnumerator CampTeleportRoutine(Camp targetCamp)
    {
        UIController.instance?.ShowLose();
        StopBossIntroSequence();
        ClearAllProjectiles();

        if (mc != null)
        {
            mc.SetControlLocked(true);
            Rigidbody2D rb = mc.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        SetDeathCameraOverride(mainCam != null ? mainCam.transform.position : Vector3.zero);
        yield return StartCoroutine(FadeBlack(0f, 1f, deathFadeOutDuration));
        ClearAllProjectiles();

        CleanupUnownedEnemies();

        Room targetRoom = targetCamp.GetComponentInParent<Room>(true);
        if (targetRoom != null && targetRoom != ActiveRoom)
        {
            ActivateRoom(targetRoom);
        }

        if (mc != null)
        {
            Vector3 destination = GetCampRespawnPosition(targetCamp);
            mc.transform.position = destination;
            LastCamp = targetCamp;

            ResetGameState();
            ResetAggro();

            mc.CurrentHealth = mc.MaxHealth;
            mc.CurrentEnergy = mc.MaxEnergy;
            UIController.instance?.SetHP(mc.CurrentHealth, mc.MaxHealth);
            UIController.instance?.SetEnergy(mc.CurrentEnergy, mc.MaxEnergy);
        }

        SnapCameraToPlayer();
        yield return StartCoroutine(FadeBlack(1f, 0f, deathFadeInDuration));

        ClearDeathCameraOverride();
        if (mc != null)
        {
            mc.SetControlLocked(false);
        }

        campTeleportRoutine = null;
    }

    private Vector3 GetCampRespawnPosition(Camp camp)
    {
        if (camp == null || mc == null)
        {
            return mc != null ? mc.transform.position : Vector3.zero;
        }

        Vector3 basePos = camp.transform.position;
        float topY = basePos.y;
        Collider2D campCollider = camp.GetComponent<Collider2D>();
        if (campCollider != null)
        {
            topY = campCollider.bounds.max.y;
        }

        float playerHalfHeight = 0.5f;
        Collider2D playerCollider = mc.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerHalfHeight = playerCollider.bounds.extents.y;
        }

        return new Vector3(basePos.x, topY + playerHalfHeight + 0.01f, mc.transform.position.z);
    }

    private void ExecuteDeathRespawn(bool isDropped)
    {
        CleanupUnownedEnemies();
        Room deathRoom = ActiveRoom;
        bool diedInBossRoom = !isDropped && deathRoom != null && deathRoom.IsBossRoom;

        if (diedInBossRoom)
        {
            ResetRoomEncounterState(deathRoom, true, true);
        }

        OpenBossLockedDoors();
        Door.OpenAllDoors();
        if (!isDropped)
        {
            ResetGameState();
        }

        ResetAggro();
        if (mc != null)
        {
            mc.Respawn(isDropped);
            mc.SetControlLocked(true);
        }
    }

    private void CleanupUnownedEnemies()
    {
        for (int i = AllEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = AllEnemies[i];
            if (enemy == null)
            {
                AllEnemies.RemoveAt(i);
                continue;
            }

            Room ownerRoom = enemy.GetComponentInParent<Room>(true);
            if (ownerRoom != null)
            {
                continue;
            }

            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
            AllEnemies.RemoveAt(i);
        }
    }

    private void DestroyRuntimeEnemy(EnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Room ownerRoom = enemy.GetComponentInParent<Room>(true);
        if (ownerRoom != null && ownerRoom.Enemies != null)
        {
            ownerRoom.Enemies.Remove(enemy);
        }

        AllEnemies.Remove(enemy);
        enemy.gameObject.SetActive(false);
        Destroy(enemy.gameObject);
    }

    private IEnumerator FadeBlack(float from, float to, float duration)
    {
        UIController ui = UIController.instance;
        if (ui == null)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            yield break;
        }

        ui.SetBlackBgAlpha(from);
        if (duration <= 0f)
        {
            ui.SetBlackBgAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ui.SetBlackBgAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        ui.SetBlackBgAlpha(to);
    }

    private void SetDeathCameraOverride(Vector3 worldPosition)
    {
        deathCameraOverridePosition = worldPosition;
        if (mainCam != null)
        {
            deathCameraOverridePosition.z = mainCam.transform.position.z;
        }

        deathCameraOverrideActive = true;
    }

    private void ClearDeathCameraOverride()
    {
        deathCameraOverrideActive = false;
    }

    private void SnapCameraToPlayer()
    {
        if (mc == null || mainCam == null)
        {
            return;
        }

        Room clampRoom = ActiveRoom;
        if (RoomBounds != null)
        {
            foreach (KeyValuePair<Room, Rect> pair in RoomBounds)
            {
                if (pair.Key != null && pair.Value.Contains(mc.transform.position))
                {
                    clampRoom = pair.Key;
                    break;
                }
            }
        }

        Vector3 target = GetClampedCameraPosition(clampRoom, mc.transform.position);
        SetDeathCameraOverride(target);
        mainCam.transform.position = target;
    }

    public void ResolvePlayerDoorOverlap(Room room)
    {
        EnsurePlayerInsideClosedDoors(room);
    }

    public void OnBossDefeated(EnemyController boss)
    {
        if (boss == null)
        {
            return;
        }

        UIController.instance?.HideBossHP();

        Room ownerRoom = ResolveBossRoom(boss);
        if (ownerRoom != null && ownerRoom.BossBound != null)
        {
            ownerRoom.BossBound.SetActive(false);
        }

        if (ownerRoom != null && bossLockedDoorRoom == ownerRoom)
        {
            OpenBossLockedDoors();
            return;
        }

        if (ownerRoom != null && ownerRoom == ActiveRoom)
        {
            OpenDoorsInRoom(ownerRoom);
        }
    }
    private void LateUpdate()
    {
        if (ActiveRoom == null || mc == null || mainCam == null) return;

        if (deathCameraOverrideActive)
        {
            mainCam.transform.position = deathCameraOverridePosition;
            return;
        }

        if (bossIntroCameraOverrideActive)
        {
            mainCam.transform.position = bossIntroCameraOverridePosition;
            return;
        }

        mainCam.transform.position = GetClampedCameraPosition(ActiveRoom, mc.transform.position);
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

        if (isEnemy)
        {
            if (!(bullet is LaserBullet))
            {
                AudioMaster.instance?.PlayEnemyBullet();
            }
        }

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

    public void ClearAllProjectiles()
    {
        ClearBullets();

        Bullet[] allBullets = FindObjectsOfType<Bullet>(true);
        for (int i = 0; i < allBullets.Length; i++)
        {
            Bullet bullet = allBullets[i];
            if (bullet == null || !bullet.gameObject.activeSelf)
            {
                continue;
            }

            if (activeBullets.Contains(bullet))
            {
                ReturnBullet(bullet);
                continue;
            }

            string key = bullet.PoolKey;
            if (!string.IsNullOrEmpty(key))
            {
                if (!bulletPools.ContainsKey(key))
                {
                    bulletPools[key] = new Queue<Bullet>();
                }

                bullet.gameObject.SetActive(false);
                bulletPools[key].Enqueue(bullet);
            }
            else
            {
                bullet.gameObject.SetActive(false);
            }
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

    private IEnumerator BossIntroRoutine(Room room, EnemyController boss)
    {
        bossIntroRoom = room;

        if (mc != null)
        {
            mc.SetControlLocked(true);
        }

        yield return StartCoroutine(RunBossIntroCameraPhase(room));

        ClearBossIntroCameraOverride();
        if (ActiveRoom == room && mc != null && mainCam != null)
        {
            mainCam.transform.position = GetClampedCameraPosition(room, mc.transform.position);
        }

        if (ActiveRoom == room)
        {
            EnemyController bossToShow = boss != null ? boss : GetFirstAliveBoss(room);
            if (bossToShow != null)
            {
                UIController.instance?.ShowBossHP(bossToShow.name, bossToShow.MaxHP);
                UIController.instance?.SetBossHPRevealScale(0f);
                yield return StartCoroutine(RunBossIntroUIPhase(room));
                UIController.instance?.SetBossHPRevealScale(1f);
            }
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

    private IEnumerator RunBossIntroCameraPhase(Room room)
    {
        float duration = Mathf.Max(0.01f, bossIntroCameraDuration);
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        if (Shaker.instance != null)
        {
            Shaker.instance.StartBossIntroShake();
        }

        while (elapsed < halfDuration)
        {
            if (ActiveRoom != room)
            {
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, halfDuration));
            SetBossIntroCameraLerp(room, t);
            yield return null;
        }

        if (Shaker.instance != null)
        {
            Shaker.instance.StopBossIntroShake();
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (ActiveRoom != room)
            {
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, halfDuration));
            SetBossIntroCameraLerp(room, 1f - t);
            yield return null;
        }
    }

    private IEnumerator RunBossIntroUIPhase(Room room)
    {
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
    }

    private void StopBossIntroSequence()
    {
        if (bossIntroCoroutine != null)
        {
            StopCoroutine(bossIntroCoroutine);
            bossIntroCoroutine = null;
        }

        ClearBossIntroCameraOverride();
        if (ActiveRoom != null && mc != null && mainCam != null)
        {
            mainCam.transform.position = GetClampedCameraPosition(ActiveRoom, mc.transform.position);
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

    private void SetBossIntroCameraLerp(Room room, float towardTop)
    {
        if (room == null || mc == null || mainCam == null)
        {
            return;
        }

        Vector3 playerClamped = GetClampedCameraPosition(room, mc.transform.position);
        Vector3 topPoint = playerClamped;
        topPoint.y = GetRoomCameraTopY(room);

        float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(towardTop));
        bossIntroCameraOverridePosition = Vector3.Lerp(playerClamped, topPoint, smooth);
        bossIntroCameraOverridePosition.z = mainCam.transform.position.z;
        bossIntroCameraOverrideActive = true;
    }

    private void ClearBossIntroCameraOverride()
    {
        bossIntroCameraOverrideActive = false;
    }

    private Vector3 GetClampedCameraPosition(Room room, Vector3 targetPosition)
    {
        if (room == null || mainCam == null)
        {
            return targetPosition;
        }

        float camHalfHeight = mainCam.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCam.aspect;

        float minX = room.roomBounds.min.x + camHalfWidth;
        float maxX = room.roomBounds.max.x - camHalfWidth;
        float minY = room.roomBounds.min.y + camHalfHeight;
        float maxY = room.roomBounds.max.y - camHalfHeight;

        float clampedX = minX > maxX ? room.roomBounds.center.x : Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = minY > maxY ? room.roomBounds.center.y : Mathf.Clamp(targetPosition.y, minY, maxY);
        return new Vector3(clampedX, clampedY, mainCam.transform.position.z);
    }

    private float GetRoomCameraTopY(Room room)
    {
        if (room == null || mainCam == null)
        {
            return mainCam != null ? mainCam.transform.position.y : 0f;
        }

        float camHalfHeight = mainCam.orthographicSize;
        float minY = room.roomBounds.min.y + camHalfHeight;
        float maxY = room.roomBounds.max.y - camHalfHeight;
        return minY > maxY ? room.roomBounds.center.y : maxY;
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

        Door[] doors = GetDoorsOwnedByRoom(room);
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

        Door[] doors = GetDoorsOwnedByRoom(room);
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

    private void ResetRoomEncounterState(Room room, bool resetBosses, bool resetSpawners)
    {
        if (room == null)
        {
            return;
        }

        if (room.IsBossRoom)
        {
            StopBossIntroSequence();
            if (room.BossBound != null)
            {
                room.BossBound.SetActive(true);
            }
        }

        OpenDoorsInRoom(room);

        HashSet<GameObject> spawnerManaged = GetSpawnerManagedEnemiesInRoom(room);
        if (resetSpawners)
        {
            Spawner[] spawners = room.GetComponentsInChildren<Spawner>(true);
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i] != null)
                {
                    spawners[i].ResetEncounterState();
                }
            }
        }

        if (room.Enemies == null)
        {
            return;
        }

        for (int i = room.Enemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = room.Enemies[i];
            if (enemy == null)
            {
                room.Enemies.RemoveAt(i);
                continue;
            }

            if (enemy.DestroyOnEncounterReset)
            {
                DestroyRuntimeEnemy(enemy);
                continue;
            }

            if (!resetBosses && enemy.IsBoss)
            {
                continue;
            }

            if (spawnerManaged.Contains(enemy.gameObject))
            {
                continue;
            }

            enemy.gameObject.SetActive(true);
            enemy.Respawn();
            enemy.ResetAggro();
            enemy.SetCombatEnabled(true);
            InvokeEncounterReset(enemy.gameObject);
        }
    }

    private void OpenDoorsInRoom(Room room)
    {
        if (room == null)
        {
            return;
        }

        Door[] doors = GetDoorsOwnedByRoom(room);
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
            {
                doors[i].Open();
            }
        }
    }

    private Door[] GetDoorsOwnedByRoom(Room room)
    {
        if (room == null)
        {
            return System.Array.Empty<Door>();
        }

        Door[] candidates = room.GetComponentsInChildren<Door>(true);
        List<Door> ownedDoors = new List<Door>(candidates.Length);
        for (int i = 0; i < candidates.Length; i++)
        {
            Door door = candidates[i];
            if (door == null)
            {
                continue;
            }

            Room nearestOwner = door.GetComponentInParent<Room>(true);
            if (nearestOwner == room)
            {
                ownedDoors.Add(door);
            }
        }

        return ownedDoors.ToArray();
    }

    private Room ResolveBossRoom(EnemyController boss)
    {
        if (boss == null)
        {
            return null;
        }

        Room parentRoom = boss.GetComponentInParent<Room>(true);
        if (parentRoom != null && parentRoom.Enemies != null && parentRoom.Enemies.Contains(boss))
        {
            return parentRoom;
        }

        for (int i = 0; i < Rooms.Count; i++)
        {
            Room room = Rooms[i];
            if (room == null || room.Enemies == null)
            {
                continue;
            }

            if (room.Enemies.Contains(boss))
            {
                return room;
            }
        }

        return parentRoom;
    }

    private void InvokeEncounterReset(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        IEncounterResettable[] resettableComponents = root.GetComponentsInChildren<IEncounterResettable>(true);
        for (int i = 0; i < resettableComponents.Length; i++)
        {
            if (resettableComponents[i] != null)
            {
                resettableComponents[i].ResetEncounterState();
            }
        }
    }

    private HashSet<GameObject> GetAllSpawnerManagedEnemies()
    {
        HashSet<GameObject> managed = new HashSet<GameObject>();
        Spawner[] spawners = FindObjectsOfType<Spawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            Spawner spawner = spawners[i];
            if (spawner == null)
            {
                continue;
            }

            for (int j = 0; j < AllEnemies.Count; j++)
            {
                EnemyController enemy = AllEnemies[j];
                if (enemy == null)
                {
                    continue;
                }

                if (spawner.ContainsEnemy(enemy.gameObject))
                {
                    managed.Add(enemy.gameObject);
                }
            }
        }

        return managed;
    }

    private HashSet<GameObject> GetSpawnerManagedEnemiesInRoom(Room room)
    {
        HashSet<GameObject> managed = new HashSet<GameObject>();
        if (room == null)
        {
            return managed;
        }

        Spawner[] spawners = room.GetComponentsInChildren<Spawner>(true);
        if (room.Enemies == null)
        {
            return managed;
        }

        for (int i = 0; i < spawners.Length; i++)
        {
            Spawner spawner = spawners[i];
            if (spawner == null)
            {
                continue;
            }

            for (int j = 0; j < room.Enemies.Count; j++)
            {
                EnemyController enemy = room.Enemies[j];
                if (enemy == null)
                {
                    continue;
                }

                if (spawner.ContainsEnemy(enemy.gameObject))
                {
                    managed.Add(enemy.gameObject);
                }
            }
        }

        return managed;
    }
}
