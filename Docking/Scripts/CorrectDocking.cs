//Detecta cuando se hace un correcto docking.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrectDocking : MonoBehaviour
{
    // Para detectar que la colisión entre el conector del robot y el cargador del dock.
    void OnTriggerEnter(Collider col)
    {
        
        // Se detecta contacto.
        if (col.gameObject.CompareTag("hitbox"))
        {
            // Si se detecta contacto, se llama a Success.
            col.gameObject.transform.parent.GetComponent<AgentLogic>().Success();
            // Se escribe en el terminal que ha entrado en contacto con el cargador.
            Debug.Log("Estoy chocando con: " + col.gameObject.name);
        }
    }

}
