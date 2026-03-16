using UnityEngine;

public class Door : MonoBehaviour
{
    public Sprite spr1; // opened
    public Sprite spr2; // closed
    public bool startOpened = true;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D obstacleCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        obstacleCollider = GetComponent<BoxCollider2D>();

        if (obstacleCollider != null)
        {
            obstacleCollider.isTrigger = false;
        }

        SetOpen(startOpened);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool isOpen)
    {
        if (spriteRenderer != null)
        {
            if (isOpen && spr1 != null)
            {
                spriteRenderer.sprite = spr1;
            }
            else if (!isOpen && spr2 != null)
            {
                spriteRenderer.sprite = spr2;
            }
        }

        if (obstacleCollider != null)
        {
            obstacleCollider.enabled = !isOpen;
        }
    }

    public static void OpenAllDoors()
    {
        Door[] allDoors = FindObjectsOfType<Door>(true);
        for (int i = 0; i < allDoors.Length; i++)
        {
            if (allDoors[i] != null)
            {
                allDoors[i].Open();
            }
        }
    }
}
