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
    WaypointMaster wm;
    // Start is called before the first frame update
    void Start()
    {
        wm = Waypoints.GetComponent<WaypointMaster>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
