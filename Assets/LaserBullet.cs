using System.Collections.Generic;
using UnityEngine;

public class LaserBullet : Bullet
{
    public int Direction = 6;
    public float Width = 1f;
    public LayerMask BlockingMask;
    public float MaxLength = 20f;

    [Header("Beam Setup")]
    [SerializeField] private OneBeam beamTemplate;

    private readonly List<OneBeam> beams = new List<OneBeam>();
    private Rigidbody2D body;
    private EnemyController ownerController;
    private float remainingDuration;
    private bool enemyLaserAudioRegistered;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        CacheTemplate();
    }

    public override void Init(bool isenemy, Vector2 firedDirection)
    {
        CacheTemplate();

        IsEnemy = isenemy;
        remainingDuration = Duration;

        if (firedDirection.sqrMagnitude > 0.0001f)
        {
            Direction = DirectionFromVector(firedDirection);
        }

        dir = GetDirectionVector(Direction);
        if (dir == Vector2.zero)
        {
            Direction = 6;
            dir = Vector2.right;
        }

        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        ownerController = isenemy ? FindOwnerController() : null;
        ConfigureBeams();
        SetLayerRecursively(gameObject, isenemy ? "EnemyBullet" : "MyBullet");
        SyncEnemyLaserAudioRegistration();
    }

    public void SetDuration(float duration)
    {
        Duration = Mathf.Max(0f, duration);
        remainingDuration = Duration;
    }

    private void Update()
    {
        if (Duration > 0f)
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                ReturnToPool();
                return;
            }
        }

        if (ownerController != null && !ownerController.gameObject.activeInHierarchy)
        {
            DestroyLaser();
        }
    }

    public void DestroyLaser()
    {
        ReturnToPool();
    }

    public void SetOwner(EnemyController owner)
    {
        ownerController = owner;
    }

    private void OnDisable()
    {
        UnregisterEnemyLaserAudio();
        for (int i = 0; i < beams.Count; i++)
        {
            if (beams[i] != null)
            {
                beams[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        UnregisterEnemyLaserAudio();
    }

    private void CacheTemplate()
    {
        if (beamTemplate == null)
        {
            beamTemplate = GetComponentInChildren<OneBeam>(true);
        }

        if (beamTemplate != null && !beams.Contains(beamTemplate))
        {
            beams.Add(beamTemplate);
        }
    }

    private void ConfigureBeams()
    {
        int beamCount = Mathf.Max(1, Mathf.RoundToInt(Width * 8f));
        EnsureBeamCount(beamCount);
        float beamSpacing = GetBeamSpacingWorld();
        for (int i = 0; i < beams.Count; i++)
        {
            OneBeam beam = beams[i];
            if (beam == null)
            {
                continue;
            }

            bool active = i < beamCount;
            if (!active)
            {
                beam.gameObject.SetActive(false);
                continue;
            }

            float lateralOffset = GetLateralOffset(i, beamCount, beamSpacing);
            beam.Init(IsEnemy, Direction, Damage, BlockingMask, MaxLength, lateralOffset);
            beam.gameObject.SetActive(true);
        }
    }

    private void EnsureBeamCount(int beamCount)
    {
        if (beamTemplate == null)
        {
            return;
        }

        while (beams.Count < beamCount)
        {
            OneBeam clone = Instantiate(beamTemplate, transform);
            clone.name = beamTemplate.name + "_" + beams.Count;
            beams.Add(clone);
        }
    }

    private float GetLateralOffset(int index, int beamCount, float beamSpacing)
    {
        if (beamCount <= 1 || beamSpacing <= 0f)
        {
            return 0f;
        }

        float centerIndex = (beamCount - 1) * 0.5f;
        return (index - centerIndex) * beamSpacing;
    }

    private float GetBeamSpacingWorld()
    {
        OneBeam reference = beamTemplate;
        if (reference == null && beams.Count > 0)
        {
            reference = beams[0];
        }

        if (reference == null)
        {
            return 0f;
        }

        float localThickness = Mathf.Max(0f, reference.GetLocalThickness());
        if (localThickness <= 0f)
        {
            return 0f;
        }

        Vector2 beamDirection = GetDirectionVector(Direction);
        Vector2 perpendicular = Vector2.Perpendicular(beamDirection);
        Vector3 scale = transform.lossyScale;
        float worldScale = Mathf.Abs(perpendicular.x) > Mathf.Abs(perpendicular.y)
            ? Mathf.Abs(scale.x)
            : Mathf.Abs(scale.y);

        return localThickness * Mathf.Max(0.0001f, worldScale);
    }

    private EnemyController FindOwnerController()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        EnemyController best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyController candidate = hits[i].GetComponentInParent<EnemyController>();
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private Vector2 GetDirectionVector(int direction)
    {
        switch (direction)
        {
            case 2: return Vector2.down;
            case 4: return Vector2.left;
            case 6: return Vector2.right;
            case 8: return Vector2.up;
            default: return Vector2.zero;
        }
    }

    private int DirectionFromVector(Vector2 vector)
    {
        if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
        {
            return vector.x >= 0f ? 6 : 4;
        }

        return vector.y >= 0f ? 8 : 2;
    }

    private void SetLayerRecursively(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0 || root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.layer = layer;
        }
    }

    private void SyncEnemyLaserAudioRegistration()
    {
        if (IsEnemy)
        {
            if (!enemyLaserAudioRegistered)
            {
                AudioMaster.instance?.StartEnemyLaserBeam();
                enemyLaserAudioRegistered = true;
            }
            return;
        }

        UnregisterEnemyLaserAudio();
    }

    private void UnregisterEnemyLaserAudio()
    {
        if (!enemyLaserAudioRegistered)
        {
            return;
        }

        enemyLaserAudioRegistered = false;
        AudioMaster.instance?.StopEnemyLaserBeam();
    }
}
