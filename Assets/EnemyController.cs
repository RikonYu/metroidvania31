using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float MaxHP;
    public float CurrentHP;
    public float MoveSpeed;
    public float CollideDamage;
    public float stunDuration = 0.25f;
    public float knockbackForce = 10f;
    public bool IsFlying;
    public GameObject Waypoints;
    public WaypointMaster wm;
    protected Vector3 StartPos;
    bool IsDead;
    public bool IsBoss;
    public bool CombatEnabled = true;
    [SerializeField] private bool destroyOnEncounterReset;
    EnemyAI AI;

    // --- 新增：闪光效果所需变量 ---
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    public float flashDuration = 0.15f; // 闪光持续时间
    public Color flashColor = Color.white; // 闪光的颜色

    // --- 新增：破损粒子效果所需变量 ---
    private ParticleSystem damageParticles;
    public event System.Action<float> Damaged;
    public event System.Action<bool> FrozenStateChanged;
    private int lastPlayerContactFrame = -1;
    private float freezeTimer;

    public bool IsFrozen => freezeTimer > 0f;
    public bool DestroyOnEncounterReset => destroyOnEncounterReset;

    private void Awake()
    {
        AI = gameObject.GetComponent<EnemyAI>();
        StartPos = transform.position;
        wm = Waypoints.GetComponent<WaypointMaster>();

        // 获取 SpriteRenderer 以便改变颜色
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 初始化受损粒子系统
        InitDamageParticles();

        transform.parent.gameObject.GetComponent<Room>()?.Enemies.Add(this);
        GameController.instance.AllEnemies.Add(this);
        if (IsFlying)
            GetComponent<Rigidbody2D>().gravityScale = 0f;
    }

    public void Respawn()
    {
        IsDead = false;
        CurrentHP = MaxHP;
        transform.position = StartPos;
        freezeTimer = 0f;

        // 恢复正常外观和状态
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        UpdateDamageEffect();
        RefreshBossHpUI();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (IsDead)
        {
            gameObject.SetActive(false);
            return;
        }
        Respawn();
    }

    public void ResetAggro()
    {
        AI.ResetToPatrol();
    }

    // Update is called once per frame
    void Update()
    {
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                freezeTimer = 0f;
                FrozenStateChanged?.Invoke(false);
            }
        }

        if (CurrentHP <= 0 && !IsDead)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        IsDead = true;
        RefreshBossHpUI();

        // 死亡时停止粒子发射
        if (damageParticles != null) damageParticles.Stop();
        if (IsBoss && Shaker.instance != null)
        {
            Shaker.instance.ShakeBossDefeat();
        }

        if (IsBoss && GameController.instance != null)
        {
            GameController.instance.OnBossDefeated(this);
        }

        gameObject.SetActive(false);
    }

    public void SetCombatEnabled(bool enabled)
    {
        CombatEnabled = enabled;
    }

    public void MarkAsRuntimeSpawned()
    {
        destroyOnEncounterReset = true;
    }

    public void Hurt(float dmg)
    {
        CurrentHP -= dmg;
        RefreshBossHpUI();
        Damaged?.Invoke(dmg);

        // 1. 触发或重置闪白光效果
        if (spriteRenderer != null)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine); // 中断当前的闪光，实现“重置”
            }
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        // 2. 检查是否达到破损血线（小于一半）
        UpdateDamageEffect();
    }

    private void RefreshBossHpUI()
    {
        if (!IsBoss || UIController.instance == null || GameController.instance == null)
        {
            return;
        }

        Room ownerRoom = GetComponentInParent<Room>();
        if (ownerRoom == null || GameController.instance.ActiveRoom != ownerRoom)
        {
            return;
        }

        UIController.instance.SetBossHP(CurrentHP, MaxHP);
    }

    public void Freeze(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        bool wasFrozen = IsFrozen;
        freezeTimer = Mathf.Max(freezeTimer, duration);
        if (!wasFrozen)
        {
            FrozenStateChanged?.Invoke(true);
        }

        Rigidbody2D enemyRb = GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            enemyRb.velocity = Vector2.zero;
            enemyRb.angularVelocity = 0f;
        }
    }

    // --- 新增：处理闪光渐变的协程 ---
    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        // 从最亮的 flashColor 开始
        spriteRenderer.color = flashColor;

        while (elapsed < flashDuration)
        {
            // 随着时间逐渐变回原本的颜色
            spriteRenderer.color = Color.Lerp(flashColor, originalColor, elapsed / flashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终颜色完全恢复
        spriteRenderer.color = originalColor;
    }

    // --- 新增：动态创建并配置破损粒子系统 ---
    // --- 更新：动态创建并配置破损粒子系统（增强飞溅效果） ---
    private void InitDamageParticles()
    {
        GameObject particleObj = new GameObject("DamageParticles");
        particleObj.transform.SetParent(this.transform);
        particleObj.transform.localPosition = Vector3.zero;

        damageParticles = particleObj.AddComponent<ParticleSystem>();
        damageParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = damageParticles.main;
        main.playOnAwake = false; 
        ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();

        psRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // 基础设置 (Main Module)
        main.duration = 1f;
        main.loop = true;
        main.startSize = 0.125f;

        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);

        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);

        main.gravityModifier = 1.5f;

        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.red);
        damageParticles.gameObject.GetComponent<ParticleSystemRenderer>().sortingOrder = 10;

        // 发射率设置 (Emission Module)
        var emission = damageParticles.emission;
        emission.rateOverTime = 12f; // 每秒发射数量稍微提高一点

        // 4. 【核心修改：发射形状】
        var shape = damageParticles.shape;
        // 改为圆形(Circle)，这样粒子会向360度四面八方爆射出去
        shape.shapeType = ParticleSystemShapeType.Circle;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // 将发射半径限制在怪物的尺寸内，使得从怪物内部向外炸
            shape.radius = Mathf.Max(col.bounds.size.x, col.bounds.size.y) * 0.35f;
        }
        else
        {
            shape.radius = 0.5f;
        }

        // 初始状态为不播放
        damageParticles.Stop();
        main.duration = 1f;
        main.loop = true;
    }

    private void UpdateDamageEffect()
    {
        if (damageParticles == null) return;

        if (CurrentHP < MaxHP * 0.5f && CurrentHP > 0)
        {
            if (!damageParticles.isPlaying)
            {
                damageParticles.Play();
            }
        }
        else
        {
            if (damageParticles.isPlaying)
            {
                damageParticles.Stop();
            }
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        ApplyContactDamage(collision.gameObject, transform.position);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        ApplyContactDamage(other.gameObject, transform.position);
    }

    public void ApplyContactDamage(GameObject target, Vector3 hitSourcePosition)
    {
        if (target == null)
        {
            return;
        }

        MCController player = target.GetComponentInParent<MCController>();
        if (player == null || Time.frameCount == lastPlayerContactFrame)
        {
            return;
        }

        lastPlayerContactFrame = Time.frameCount;
        bool isdead = CollideDamage >= player.CurrentHealth;
        player.ApplyDamageAndStun(CollideDamage, stunDuration);

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null || isdead)
        {
            return;
        }

        Vector2 direction = ((Vector2)player.transform.position - (Vector2)hitSourcePosition).normalized;
        if (direction.y <= 0.2f)
        {
            direction.y = 0.8f;
        }

        direction = direction.normalized;
        playerRb.velocity = Vector2.zero;
        playerRb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }
}
