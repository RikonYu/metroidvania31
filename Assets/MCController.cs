using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MCController : MonoBehaviour
{
    public float MaxHealth, CurrentHealth;
    public GameObject MyBullet;
    public float FireCoolDown;
    private float firecd;
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float decceleration = 10f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 4f;
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Game Feel")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Hard Landing")]
    [SerializeField] private float hardLandingThreshold = -20f;
    [SerializeField] private float stunDuration = 0.5f;

    [Header("Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Respawn System")]
    [Tooltip("复活时相对于地面接触点的偏移量")]
    [SerializeField] private Vector2 respawnOffset = new Vector2(0f, 0.5f);
    [Tooltip("判定为平坦表面的法线阈值")]
    [SerializeField] private float safeSlopeThreshold = 0.7f;
    

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isStunned;
    private float lastVerticalVelocity;
    private Vector3 lastSafePosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        lastSafePosition = transform.position;
        firecd = 0f;
        CurrentHealth = MaxHealth;
        UIController.instance.SetHP(CurrentHealth, MaxHealth);
        
    }

    void Update()
    {
        if (isStunned) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameController.instance.InteractingObject?.Interact();
        }
        if (Input.GetKeyDown(KeyCode.W))
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        firecd -= Time.deltaTime;
        if (Input.GetMouseButton(0))
        {
            if (firecd <= 0f)
            {
                firecd = FireCoolDown;
                var x = Instantiate(MyBullet, transform.position, Quaternion.identity);
                Vector3 screenPosition = Input.mousePosition;
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                worldPosition.z = 0;
                x.GetComponent<Bullet>().Init(false, worldPosition - transform.position);
            }
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            PerformJump();
        }

        lastVerticalVelocity = rb.velocity.y;
    }

    void FixedUpdate()
    {
        CheckGround();
        UpdateSafePosition();

        if (isStunned)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            ApplyGravity();
            return;
        }

        Move();
        ModifyPhysics();
    }

    private void Move()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        float movement = speedDif * accelRate * Time.fixedDeltaTime;

        rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);
    }

    private void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void ModifyPhysics()
    {
        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }

        if (rb.velocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.W))
        {
            rb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    private void ApplyGravity()
    {
        rb.gravityScale = (rb.velocity.y < 0) ? fallMultiplier : 1f;
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            OnLand();
        }
    }

    private void UpdateSafePosition()
    {
        if (isGrounded && rb.velocity.y <= 0.1f)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckRadius * 2f, groundLayer);

            if (hit.collider != null)
            {
                if (hit.normal.y > safeSlopeThreshold)
                {
                    lastSafePosition = hit.point + respawnOffset;
                }
            }
        }
    }

    public void Respawn(bool isDropped)
    {
        firecd = 0f;
        rb.velocity = Vector2.zero;
        isStunned = false;
        if (isDropped)
            transform.position = lastSafePosition;
        else
        {
            CurrentHealth = MaxHealth;
            transform.position = GameController.instance.LastCamp.transform.position;

            UIController.instance.SetHP(CurrentHealth, MaxHealth);
        }
    }

    private void OnLand()
    {
        if (lastVerticalVelocity <= hardLandingThreshold)
        {
            StartCoroutine(HardLandingStun());
        }
    }

    public void Hurt(float dmg)
    {
        CurrentHealth -= dmg;
        if (CurrentHealth <= 0f) CurrentHealth = 0f;
        UIController.instance.SetHP(CurrentHealth, MaxHealth);
        if (CurrentHealth <= 0f)
            GameController.instance.Die(false);
    }

    IEnumerator HardLandingStun()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckRadius * 2f);
        }
    }
}