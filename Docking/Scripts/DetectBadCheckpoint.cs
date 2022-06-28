using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectBadCheckpoint : MonoBehaviour
{
    // Si detecta un checkpoint malo de la misma forma que el docking station.
    void OnTriggerEnter(Collider col)
    {

        if (col.gameObject.CompareTag("badcheckpoint"))
        {
            col.gameObject.transform.parent.GetComponent<AgentLogic>().DetectBadCheckpoint();
            Debug.Log("He pasado el " + col.gameObject.name);
        }
    }
}
