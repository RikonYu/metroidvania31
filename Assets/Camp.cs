using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

public class Camp : Interactable
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

        Utils.SnapToGround(gameObject, snapDistance, groundLayer);

        if (infoObject != null)
        {
            infoObject.SetActive(false);
        }
    }


    public override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<MCController>() != null)
        {
            if (infoObject != null)
            {
                infoObject.SetActive(true);
            }
        }
        base.OnTriggerEnter2D(other);
    }

    public override void Interact()
    {
        GameController.instance.LastCamp = this;
        infoObject.SetActive(false);
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
        base.OnTriggerExit2D(other);
    }

}