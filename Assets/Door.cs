using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    public bool IsSatisfied;
    public GameObject Obs;
    BoxCollider2D col;
    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        Obs.GetComponent<BoxCollider2D>().size = col.size;
        Obs.GetComponent<BoxCollider2D>().offset = col.offset;
    }

    public override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
    }

    public override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
    }
    public void Open()
    {

    }

    public override void Interact()
    {
        if (IsSatisfied)
        {
            Obs.SetActive(false);
            Open();
        }
    }
}
