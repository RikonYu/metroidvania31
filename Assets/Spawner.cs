using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnerWave
{
    public List<GameObject> enemies = new List<GameObject>();
}

public class Spawner : MonoBehaviour, IEncounterResettable
{
    [Header("Waves")]
    public List<SpawnerWave> waves = new List<SpawnerWave>();

    [Header("Rewards")]
    public List<GameObject> rewards = new List<GameObject>();

    private int currentWaveIndex = -1;
    private bool initialized;
    private bool completed;
    private bool doorsLocked;
    private Room ownerRoom;
    private readonly List<Door> doorsClosedBySpawner = new List<Door>();

    private void Awake()
    {
        ownerRoom = GetComponentInParent<Room>();
    }

    private void OnEnable()
    {
        if (completed)
        {
            return;
        }

        if (!initialized)
        {
            initialized = true;
            SetAllWaveEnemiesActive(false);
            SetRewardsActive(false);
            ActivateNextWave();
        }

        if (!completed && !doorsLocked && ShouldLockDoorsNow())
        {
            LockRoomDoors();
        }
    }

    private void Update()
    {
        if (!completed && !doorsLocked && ShouldLockDoorsNow())
        {
            LockRoomDoors();
        }

        if (completed || currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
        {
            return;
        }

        if (IsWaveCleared(waves[currentWaveIndex]))
        {
            ActivateNextWave();
        }
    }

    private void ActivateNextWave()
    {
        while (true)
        {
            currentWaveIndex++;

            if (currentWaveIndex >= waves.Count)
            {
                CompleteSpawner();
                return;
            }

            ActivateWave(waves[currentWaveIndex]);

            if (!IsWaveCleared(waves[currentWaveIndex]))
            {
                return;
            }
        }
    }

    private void ActivateWave(SpawnerWave wave)
    {
        if (wave == null || wave.enemies == null)
        {
            return;
        }

        for (int i = 0; i < wave.enemies.Count; i++)
        {
            GameObject enemy = wave.enemies[i];
            if (enemy != null)
            {
                enemy.SetActive(true);
            }
        }
    }

    private bool IsWaveCleared(SpawnerWave wave)
    {
        if (wave == null || wave.enemies == null || wave.enemies.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < wave.enemies.Count; i++)
        {
            GameObject enemy = wave.enemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (enemy.activeInHierarchy)
            {
                return false;
            }
        }

        return true;
    }

    private void CompleteSpawner()
    {
        completed = true;
        UnlockRoomDoors();
        SetRewardsActive(true);
        Destroy(gameObject);
    }

    private void SetAllWaveEnemiesActive(bool active)
    {
        if (waves == null)
        {
            return;
        }

        for (int i = 0; i < waves.Count; i++)
        {
            SpawnerWave wave = waves[i];
            if (wave == null || wave.enemies == null)
            {
                continue;
            }

            for (int j = 0; j < wave.enemies.Count; j++)
            {
                GameObject enemy = wave.enemies[j];
                if (enemy != null)
                {
                    enemy.SetActive(active);
                }
            }
        }
    }

    private void SetRewardsActive(bool active)
    {
        if (rewards == null)
        {
            return;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            GameObject reward = rewards[i];
            if (reward != null)
            {
                reward.SetActive(active);
            }
        }
    }

    private bool ShouldLockDoorsNow()
    {
        if (ownerRoom == null || GameController.instance == null || GameController.instance.mc == null)
        {
            return false;
        }

        return GameController.instance.ActiveRoom == ownerRoom;
    }

    private void LockRoomDoors()
    {
        if (doorsLocked || ownerRoom == null)
        {
            return;
        }

        doorsLocked = true;
        doorsClosedBySpawner.Clear();

        Door[] doors = ownerRoom.GetComponentsInChildren<Door>(true);
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
                doorsClosedBySpawner.Add(door);
            }
        }

        GameController.instance.ResolvePlayerDoorOverlap(ownerRoom);
    }

    private void UnlockRoomDoors()
    {
        if (!doorsLocked)
        {
            return;
        }

        for (int i = 0; i < doorsClosedBySpawner.Count; i++)
        {
            Door door = doorsClosedBySpawner[i];
            if (door != null)
            {
                door.Open();
            }
        }

        doorsClosedBySpawner.Clear();
        doorsLocked = false;
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

    public bool ContainsEnemy(GameObject enemyObject)
    {
        if (enemyObject == null || waves == null)
        {
            return false;
        }

        for (int i = 0; i < waves.Count; i++)
        {
            SpawnerWave wave = waves[i];
            if (wave == null || wave.enemies == null)
            {
                continue;
            }

            for (int j = 0; j < wave.enemies.Count; j++)
            {
                if (wave.enemies[j] == enemyObject)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void ResetEncounterState()
    {
        UnlockRoomDoors();
        completed = false;
        initialized = false;
        currentWaveIndex = -1;
        SetAllWaveEnemiesActive(false);
        SetRewardsActive(false);
    }
}
