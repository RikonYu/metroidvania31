    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class Interactable: MonoBehaviour
    {
        public virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<MCController>() != null)
            {
                GameController.instance.InteractingObject = this;
            }
        }
        public virtual void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<MCController>() != null)
            {
                if (Object.ReferenceEquals(GameController.instance.InteractingObject,this))
                {
                    GameController.instance.InteractingObject = null;
                }
            }
        }
        public abstract void Interact();
    }
