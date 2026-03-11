using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoAI : EnemyAI
{
    Animator anim;
    SpriteRenderer spr;
    bool isup = false;

    protected override void Start()
    {
        base.Start();
        anim = transform.Find("Sprite").GetComponent<Animator>();
        spr = transform.Find("Sprite").GetComponent<SpriteRenderer>();
    }

    protected override void Attack()
    {
        var firepos = transform.Find("firespot").position;

        GameController.instance.FireBullet(Bullet, firepos + Vector3.up * 1.5f,  spr.flipX?Vector3.right:Vector3.left, true);

        int currentState = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
        var newup = Random.Range(0, 2) == 1;
        if (newup != isup)
        {
            anim.enabled = true;
            if (isup)
            {
                anim.speed = 1f;
                anim.Play(currentState, 0, 0f);
            }
            else
            {
                anim.speed = -1f;
                anim.Play(currentState, 0, 1f);
            }
        }
        isup = newup;
    }
}