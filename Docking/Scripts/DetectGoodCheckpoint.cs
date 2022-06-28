using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectGoodCheckpoint : MonoBehaviour
{
    // Si detecta un checkpoint bueno de la misma forma que el docking station.
    void OnTriggerEnter(Collider col)
    {

        if (col.gameObject.CompareTag("checkpoint"))
        {
            col.gameObject.transform.parent.GetComponent<AgentLogic>().DetectGoodCheckpoint();
            Debug.Log("He pasado el " + col.gameObject.name);
        }
    }
}
