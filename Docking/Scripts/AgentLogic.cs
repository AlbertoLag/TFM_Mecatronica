//Librerías y paquetes utilizados
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;

//Clase asociada al agente, robot.
public class AgentLogic : Agent
{
    //Para la reaparición creamos las siguientes variables
    public GameObject ground; 
    public GameObject area;

    //Para los límites del área
    [HideInInspector]
    public Bounds areaBounds;

    //Los parámetros generales se especifican con:
    private GeneralSettings generalSettings;

    //Se crear el objeto "obstacle"
    public GameObject obstacle;

    //Para detectar que se ha hecho el docking bien.
    [HideInInspector]
    public CorrectDocking correctDocking;

    public bool useVectorObs;

    //Se declara el rigid body, lo que se va a mover del robot. 
    Rigidbody m_agentRigid; 
    
    //Para poner el ground material de succes o fail.
    Material m_GroundMaterial; 
    Renderer m_GroundRenderer;

    EnvironmentParameters m_ResetParams;

    //Se declaran ciertas variables
    private float stepsEpoch = 0;
    private float dockingTime;

    //Se obtienen los parámetros generales una vez.
    void Awake()
    {
        generalSettings = FindObjectOfType<GeneralSettings>();
    }

    public override void Initialize()
    {
        obstacle = GameObject.Find("obstacle");
        // Cache the agent rigidbody
        m_agentRigid = GetComponent<Rigidbody>();
        // Se obtienen los límites del área
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    // Se utilizan los límites del área para crear posiciones aleatorias del robot.
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * generalSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * generalSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * generalSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * generalSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }
    // Cuando el robot hace un acoplamiento correcto.
    public void Success()
    {
        // Establezco la recompensa de +5.
        AddReward(5f);
        // Reseteo la variable que cuenta los epochs de cada episodio
        stepsEpoch = 0;
        // Se cambia el material para indicar que se ha realizado un acoplamiento.
        StartCoroutine(SwapGroundMaterial(generalSettings.correctDockingMaterial, 0.5f));
        // Se termina el episodio
        EndEpisode();
    }
    // Cuando se ha excedido el tiempo estipulado para realizar el acoplamiento.
    public void Fail()
    {
        // Establezco una penalización de -1.
        AddReward(-1f);

        // Se cambia el material para indicar que ha fracasado en el acoplamiento.
        StartCoroutine(SwapGroundMaterial(generalSettings.failMaterial, 0.5f));
        Debug.Log("FRACASO");
        // Se termina el episodio
        EndEpisode();

    }
    // Cuando detecta el chekcpoint frontal.
    public void DetectGoodCheckpoint()
    {
        AddReward(0.1f);
    }
    // Cuando detecta los checkpoints laterales.
    public void DetectBadCheckpoint()
    {
        AddReward(-1f);
    }

    // Para cambiar el material del suelo
    IEnumerator SwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    // Con lo siguiente se define el movimiento del agente.
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
                //Se quitan las siguiente dos opciones para establecer
                //las restricciones no holonómicas.
            /*case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
            */
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_agentRigid.AddForce(dirToGo * generalSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    // En cada paso se llama a esta funcion. El agente toma la acción necesaria.
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Se mueve el agente.
        MoveAgent(actionBuffers.DiscreteActions);

        // Se le pone una pequeña penalización para motivar al agente a realizar los menos pasos posibles.
        AddReward(-1f / MaxStep);
        stepsEpoch++;
        if (stepsEpoch == 1500)  // Si pasan 45 segundos sin aparcar, ha fracasado.2250
        {
            Fail();
            dockingTime = 0.02f * MaxStep;

        }
    }

    //Si establezco el control en heuristic, lo controla el experto:
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }

    }

    // Cada vez que se inicia un episodio se hace lo siguiente:
    public override void OnEpisodeBegin()
    {
        // Se obtiene un ángulo y posición aleatoria del robot.
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        transform.position = GetRandomSpawnPos();
        m_agentRigid.velocity = Vector3.zero;
        m_agentRigid.angularVelocity = Vector3.zero;
        // Se reestablece el entorno.
        SetResetParameters();
    }

    public void SetGroundMaterialFriction()
    {
        var groundCollider = ground.GetComponent<Collider>();

        groundCollider.material.dynamicFriction = m_ResetParams.GetWithDefault("dynamic_friction", 0);
        groundCollider.material.staticFriction = m_ResetParams.GetWithDefault("static_friction", 0);
    }

    void SetResetParameters()
    {
        SetGroundMaterialFriction();
    }
}



//CAMBIAR ESTO PARA CAMBIAR EL TIEMPO 30S = 1500   MaxStep-1