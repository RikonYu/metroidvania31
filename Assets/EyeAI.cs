using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeAI : EnemyAI, IEncounterResettable
{
    [Header("Eye Control")]
    public float pulseInterval = 4f;
    public float pulseDuration = 2f;
    public float hurtTrackSpeedMultiplier = 2f;
    public float hurtShakeAmplitude = 0.35f;
    public bool openDoorsOnDeath = true;

    private Room ownerRoom;
    private readonly List<Track> tracks = new List<Track>();
    private readonly List<Trap> traps = new List<Trap>();
    private readonly List<CannonAI> cannons = new List<CannonAI>();
    private readonly List<Door> doors = new List<Door>();
    private readonly Dictionary<Track, float> baseTrackSpeeds = new Dictionary<Track, float>();

    private float pulseTimer;
    private bool encounterInitialized;
    private bool hurtReactionActive;
    private bool deathStateApplied;
    private Coroutine hurtRoutine;

    protected override void Start()
    {
        base.Start();
        ownerRoom = GetComponentInParent<Room>();
        CollectRoomObjects();
        SubscribeDamageEvent();
        ResetEncounterState();
    }

    private void OnEnable()
    {
        SubscribeDamageEvent();
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.Damaged -= HandleDamaged;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        RestoreTrackSpeeds();
        hurtReactionActive = false;

        if (controller != null && controller.CurrentHP <= 0f)
        {
            ApplyDeathState();
        }
    }

    protected override void Update()
    {
        if (IsCombatPaused())
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        moveInput = Vector2.zero;

        if (!encounterInitialized)
        {
            InitializeEncounter();
        }

        if (!encounterInitialized)
        {
            return;
        }

        if (pulseInterval > 0f)
        {
            pulseTimer -= Time.deltaTime;
            if (pulseTimer <= 0f)
            {
                TriggerPulse();
                pulseTimer = Mathf.Max(0.01f, pulseInterval);
            }
        }
    }

    protected override void Attack()
    {
    }

    public void ResetEncounterState()
    {
        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        RestoreTrackSpeeds();
        hurtReactionActive = false;
        deathStateApplied = false;
        encounterInitialized = false;
        pulseTimer = Mathf.Max(0.01f, pulseInterval);

        CollectRoomObjects();
    }

    private void InitializeEncounter()
    {
        CollectRoomObjects();
        CloseRoomDoors();
        RandomizeAllTrackDirections();
        SetAllTrapCycles(1f, 0f, true);

        pulseTimer = Mathf.Max(0.01f, pulseInterval);
        encounterInitialized = true;
    }

    private void SubscribeDamageEvent()
    {
        if (controller == null)
        {
            controller = GetComponent<EnemyController>();
        }

        if (controller == null)
        {
            return;
        }

        controller.Damaged -= HandleDamaged;
        controller.Damaged += HandleDamaged;
    }

    private void HandleDamaged(float damage)
    {
        if (damage <= 0f || hurtReactionActive || !isActiveAndEnabled || IsCombatPaused())
        {
            return;
        }

        if (!encounterInitialized)
        {
            InitializeEncounter();
        }

        if (Shaker.instance != null)
        {
            Shaker.instance.Shake(hurtShakeAmplitude);
        }

        TriggerPulse();
        hurtRoutine = StartCoroutine(HurtReactionRoutine());
    }

    private IEnumerator HurtReactionRoutine()
    {
        hurtReactionActive = true;
        ApplyTrackSpeedMultiplier(hurtTrackSpeedMultiplier);

        float duration = Mathf.Max(0f, pulseDuration);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        RestoreTrackSpeeds();
        hurtReactionActive = false;
        hurtRoutine = null;
    }

    private void TriggerPulse()
    {
        RandomizeAllTrackDirections();
        ForceCannonsFire(pulseDuration);
    }

    private void CollectRoomObjects()
    {
        if (ownerRoom == null)
        {
            ownerRoom = GetComponentInParent<Room>();
        }

        tracks.Clear();
        traps.Clear();
        cannons.Clear();
        doors.Clear();

        if (ownerRoom == null)
        {
            return;
        }

        tracks.AddRange(ownerRoom.GetComponentsInChildren<Track>(true));
        traps.AddRange(ownerRoom.GetComponentsInChildren<Trap>(true));
        cannons.AddRange(ownerRoom.GetComponentsInChildren<CannonAI>(true));
        doors.AddRange(ownerRoom.GetComponentsInChildren<Door>(true));

        List<Track> invalidKeys = new List<Track>();
        foreach (KeyValuePair<Track, float> pair in baseTrackSpeeds)
        {
            if (pair.Key == null)
            {
                invalidKeys.Add(pair.Key);
            }
        }
        for (int i = 0; i < invalidKeys.Count; i++)
        {
            baseTrackSpeeds.Remove(invalidKeys[i]);
        }

        for (int i = 0; i < tracks.Count; i++)
        {
            Track track = tracks[i];
            if (track == null)
            {
                continue;
            }

            if (!baseTrackSpeeds.ContainsKey(track))
            {
                baseTrackSpeeds[track] = track.MoveSpeed;
            }
        }
    }

    private void CloseRoomDoors()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            if (doors[i] != null)
            {
                doors[i].Close();
            }
        }

        if (GameController.instance != null && ownerRoom != null && GameController.instance.ActiveRoom == ownerRoom)
        {
            GameController.instance.ResolvePlayerDoorOverlap(ownerRoom);
        }
    }

    private void OpenRoomDoors()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            if (doors[i] != null)
            {
                doors[i].Open();
            }
        }
    }

    private void RandomizeAllTrackDirections()
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            Track track = tracks[i];
            if (track != null)
            {
                track.SetDirection(Random.value < 0.5f);
            }
        }
    }

    private void SetAllTracksRight()
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            Track track = tracks[i];
            if (track != null)
            {
                track.SetDirection(false);
            }
        }
    }

    private void ApplyTrackSpeedMultiplier(float multiplier)
    {
        float clampedMultiplier = Mathf.Max(0f, multiplier);
        for (int i = 0; i < tracks.Count; i++)
        {
            Track track = tracks[i];
            if (track == null)
            {
                continue;
            }

            float baseSpeed;
            if (!baseTrackSpeeds.TryGetValue(track, out baseSpeed))
            {
                baseSpeed = track.MoveSpeed;
                baseTrackSpeeds[track] = baseSpeed;
            }

            track.SetMoveSpeed(baseSpeed * clampedMultiplier);
        }
    }

    private void RestoreTrackSpeeds()
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            Track track = tracks[i];
            if (track == null)
            {
                continue;
            }

            float baseSpeed;
            if (baseTrackSpeeds.TryGetValue(track, out baseSpeed))
            {
                track.SetMoveSpeed(baseSpeed);
            }
        }
    }

    private void ForceCannonsFire(float duration)
    {
        float fireDuration = Mathf.Max(0f, duration);
        if (fireDuration <= 0f)
        {
            return;
        }

        for (int i = 0; i < cannons.Count; i++)
        {
            CannonAI cannon = cannons[i];
            if (!IsCannonAlive(cannon))
            {
                continue;
            }

            cannon.ForceFireFor(fireDuration);
        }
    }

    private bool IsCannonAlive(CannonAI cannon)
    {
        if (cannon == null || !cannon.gameObject.activeInHierarchy)
        {
            return false;
        }

        EnemyController cannonController = cannon.GetComponent<EnemyController>();
        if (cannonController == null)
        {
            return true;
        }

        return cannonController.CurrentHP > 0f;
    }

    private void SetAllTrapCycles(float upTime, float downTime, bool fixedUpState)
    {
        for (int i = 0; i < traps.Count; i++)
        {
            Trap trap = traps[i];
            if (trap == null)
            {
                continue;
            }

            trap.SetCycleTimes(upTime, downTime);
            trap.SetFixedState(fixedUpState);
        }
    }

    private void ApplyDeathState()
    {
        if (deathStateApplied)
        {
            return;
        }

        deathStateApplied = true;
        CollectRoomObjects();
        RestoreTrackSpeeds();
        SetAllTracksRight();
        SetAllTrapCycles(0f, 1f, false);

        if (openDoorsOnDeath)
        {
            OpenRoomDoors();
        }
    }
}
