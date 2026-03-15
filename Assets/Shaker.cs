using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shaker : MonoBehaviour
{
    public static Shaker instance;
    float amount;
    GameObject cam;
    // Start is called before the first frame update
    void Start()
    {
        cam = transform.Find("Main Camera").gameObject;
        instance = this;
        
    }

    public void Shake(float amt)
    {
        amount = amt;
    }
    // Update is called once per frame
    void Update()
    {
        if (amount > 0)
        {
            amount -= Time.deltaTime;
            cam.transform.position = Random.insideUnitCircle.normalized * amount;
        }
    }
}
