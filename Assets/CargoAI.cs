using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoAI : EnemyAI
{
    [Header("Cargo Fire Spots")]
    [SerializeField] private Transform downFireSpot;
    [SerializeField] private Transform upFireSpot;

    private SpriteRenderer spr;
    private bool isUp;
    private bool hasChoice;
    private int sameChoiceStreak;

    protected override void Start()
    {
        base.Start();
        Transform spriteTransform = transform.Find("Sprite");
        if (spriteTransform != null)
        {
            spr = spriteTransform.GetComponent<SpriteRenderer>();
        }

        ResolveFireSpots();
    }

    protected override void Attack()
    {
        bool newUp = PickNextFireLevel();

        Transform selectedFireSpot = newUp ? upFireSpot : downFireSpot;
        if (selectedFireSpot != null && GameController.instance != null)
        {
            Vector3 firePos = selectedFireSpot.position;
            Vector2 fireDir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
            GameController.instance.FireBullet(Bullet, firePos, fireDir, true);
        }

        if (!hasChoice)
        {
            hasChoice = true;
            isUp = newUp;
            return;
        }

        isUp = newUp;
    }

    private bool PickNextFireLevel()
    {
        bool candidate = Random.Range(0, 2) == 1;

        if (hasChoice && sameChoiceStreak >= 2 && candidate == isUp)
        {
            candidate = !candidate;
        }

        if (!hasChoice)
        {
            sameChoiceStreak = 1;
            return candidate;
        }

        if (candidate == isUp)
        {
            sameChoiceStreak++;
        }
        else
        {
            sameChoiceStreak = 1;
        }

        return candidate;
    }

    private void ResolveFireSpots()
    {
        if (downFireSpot == null)
        {
            downFireSpot = FindFireSpotByName(
                "firespot_down",
                "down_firespot",
                "downfirespot",
                "firespotdown",
                "firespot1");
        }

        if (upFireSpot == null)
        {
            upFireSpot = FindFireSpotByName(
                "firespot_up",
                "up_firespot",
                "upfirespot",
                "firespotup",
                "firespot2");
        }

        if (downFireSpot != null && upFireSpot != null)
        {
            return;
        }

        Transform[] all = GetComponentsInChildren<Transform>(true);
        Transform minY = null;
        Transform maxY = null;
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
            {
                continue;
            }

            string lower = t.name.ToLowerInvariant();
            if (!lower.Contains("firespot"))
            {
                continue;
            }

            float y = t.localPosition.y;
            if (y < minValue)
            {
                minValue = y;
                minY = t;
            }

            if (y > maxValue)
            {
                maxValue = y;
                maxY = t;
            }
        }

        if (downFireSpot == null)
        {
            downFireSpot = minY != null ? minY : transform.Find("firespot");
        }

        if (upFireSpot == null)
        {
            upFireSpot = maxY != null ? maxY : downFireSpot;
        }
    }

    private Transform FindFireSpotByName(params string[] names)
    {
        if (names == null || names.Length == 0)
        {
            return null;
        }

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
            {
                continue;
            }

            string lower = t.name.ToLowerInvariant();
            for (int j = 0; j < names.Length; j++)
            {
                if (lower == names[j])
                {
                    return t;
                }
            }
        }

        return null;
    }

}
