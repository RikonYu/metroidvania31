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
    public bool IsDead;
    private void Awake()
    {
        StartPos = transform.position;
        transform.parent.gameObject.GetComponent<Room>().Enemies.Add(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        if (IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        CurrentHP = MaxHP;
        transform.position = StartPos;
        wm = Waypoints.GetComponent<WaypointMaster>();
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
