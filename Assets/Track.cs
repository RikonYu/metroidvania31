using UnityEngine;

public class Track : MonoBehaviour
{
    public float MoveSpeed;
    public bool IsLeft;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }

        UpdateAnimatorSpeed();

    }

    void Update()
    {
        UpdateAnimatorSpeed();
    }

    public Vector2 GetVelocity()
    {
        float direction = IsLeft ? -1f : 1f;
        return new Vector2(direction * Mathf.Abs(MoveSpeed), 0f);
    }

    public void SetDirection(bool isLeft)
    {
        IsLeft = isLeft;
    }

    public void SetMoveSpeed(float speed)
    {
        MoveSpeed = speed;
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("Speed", MoveSpeed *(IsLeft?-1:1) / 3f);
    }

}
