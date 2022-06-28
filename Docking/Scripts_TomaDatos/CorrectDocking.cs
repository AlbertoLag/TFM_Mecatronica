//Detecta cuando se hace un correcto docking.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrectDocking : MonoBehaviour
{
    void OnTriggerEnter(Collider col)
    {
        
        // Correcto Docking.
        if (col.gameObject.CompareTag("hitbox"))
        {
            col.gameObject.transform.parent.GetComponent<AgentLogic>().Success();
            Debug.Log("Estoy chocando con: " + col.gameObject.name);
        }
    }

}
