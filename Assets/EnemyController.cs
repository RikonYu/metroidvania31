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
    private void Awake()
    {
        StartPos = transform.position;
    }
    // Start is called before the first frame update
    void Start()
    {
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
        Destroy(gameObject);
    }
}
