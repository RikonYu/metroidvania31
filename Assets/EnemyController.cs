using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float MaxHP;
    public float CurrentHP;
    public float MoveSpeed;
    public bool IsFlying;
    public GameObject Waypoints;
    public WaypointMaster wm;
    protected Vector3 StartPos;
    bool IsDead;
    public bool IsBoss;
    EnemyAI AI;
    private void Awake()
    {
        AI = gameObject.GetComponent<EnemyAI>();
        StartPos = transform.position;
        wm = Waypoints.GetComponent<WaypointMaster>();
        transform.parent.gameObject.GetComponent<Room>().Enemies.Add(this);
        GameController.instance.AllEnemies.Add(this);
    }
    public void Respawn()
    {
        IsDead = false;
        CurrentHP = MaxHP;
        transform.position = StartPos;
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
        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        IsDead = true;
        gameObject.SetActive(false);
    }
    public void Hurt(float dmg)
    {
        CurrentHP -= dmg;
    }
}
