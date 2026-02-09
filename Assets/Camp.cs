using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camp : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("需要显示/隐藏的提示信息子物体")]
    [SerializeField] private GameObject infoObject;

    [Tooltip("地面层级，用于初始下落对齐")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("向下探测地面的最大距离")]
    [SerializeField] private float snapDistance = 10f;

    void Start()
    {
        if (GameController.instance != null)
        {
            GameController.instance.Camps.Add(this);
        }

        SnapToGround();

        if (infoObject != null)
        {
            infoObject.SetActive(false);
        }
    }

    private void SnapToGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, snapDistance, groundLayer);
        if (hit.collider != null)
        {
            transform.position = new Vector3(hit.point.x, hit.point.y + GetComponent<BoxCollider2D>().size.y/2f, transform.position.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<MCController>() != null)
        {
            if (infoObject != null)
            {
                infoObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<MCController>() != null)
        {
            if (infoObject != null)
            {
                infoObject.SetActive(false);
            }
        }
    }
}