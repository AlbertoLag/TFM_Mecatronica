//Este script va en el Agente
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Text;
using System.IO;
using System;

//using TMPro; //Para el textMeshPro, texto

public class AgentLogic : Agent
{
    /*
    //Los stats
    private TextMeshProUGUI rewardValue = null;
    private TextMeshProUGUI episodesValue = null;
    private TextMeshProUGUI stepValue = null;
    private float overallReward = 0;
    private float overallSteps = 0;
    */
    //Para el Spawn, creamos las siguientes variables
    public GameObject ground; //No es necesario?
    public GameObject area;

    //Para los límites del área
    [HideInInspector]
    public Bounds areaBounds;

    //Para llamar a la clase DockingSettings 
    private GeneralSettings generalSettings;

    //Se crea el objeto de la estacion de carga y los obstáculos
    public GameObject dockingStation; //QUITAR
    public GameObject obstacle; //
    public GameObject robot;

    //Para detectar que se ha hecho el docking bien.
    [HideInInspector]
    public CorrectDocking correctDocking;

    public bool useVectorObs;
        
    Rigidbody m_agentRigid; //Tenemos que declarar el rigid body. (Initialization)
    
    //Para poner el ground material de succes o fail
    Material m_GroundMaterial; //Awake()
    Renderer m_GroundRenderer;

    EnvironmentParameters m_ResetParams;

    //Nuevas variables
    //Saved data
    private float dockingTime;
    private float stepsEpoch = 0;
    private float successfulDocking;
    private float checkpoint;
    private Vector3 robotPos;
    private float spawningAngle;
    StringBuilder csv = new StringBuilder();
    public int epoch = 0;
    public int steps = 0;


    void Awake()
    {
        generalSettings = FindObjectOfType<GeneralSettings>();
    }

    public override void Initialize()
    {
        //AQUÍ DEBERÍA DE PONER CUANDO EL HITBOX TOCA EL DOCKING
        //correctDocking = block.GetComponent<CorrectDocking>();
        //correctDocking.agent = this; ************************************

        /*dockingStation = GameObject.Find("Docking station");
        if (dockingStation == null)
        {
            dockingStation = GameObject.Find("Docking station(Clone)");
        }*/

        //Para la representación de los datos
        dockingStation = GameObject.Find("dock");

        //Debug.Log(MaxStep);
        obstacle = GameObject.Find("obstacle");
        // Cache the agent rigidbody
        m_agentRigid = GetComponent<Rigidbody>();
        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        /*
        //Obtengo la posición del Docking Station
        
        var dockingPos = Vector3.zero;
        dockingPos = dockingStation.transform.position;
        Debug.Log(dockingPos);
        SetResetParameters();
        */
    }

    // Use the ground's bounds to pick a random spawn position.
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = UnityEngine.Random.Range(-areaBounds.extents.x * generalSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * generalSettings.spawnAreaMarginMultiplier);

            var randomPosZ = UnityEngine.Random.Range(-areaBounds.extents.z * generalSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * generalSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            robotPos = randomSpawnPos;
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }
    // Cuando el robot hace un correcto Docking
    public void Success()
    {
        // Establezco la recompensa de 5.
        AddReward(5f);

        // Swap ground material for a bit to indicate we scored.
        StartCoroutine(SwapGroundMaterial(generalSettings.correctDockingMaterial, 0.5f));
        //Guardo datos
        successfulDocking = 1;
        dockingTime = 0.02f * stepsEpoch;

        String first = robotPos.ToString();
        String second = dockingTime.ToString();
        String third = stepsEpoch.ToString();
        String fourth = successfulDocking.ToString();
        String fifth = checkpoint.ToString();

        var newLine = string.Format("{0};{1};{2};{3};{4}", first, second, third, fourth, fifth);
        Debug.Log(newLine);
        csv.AppendLine(newLine);

        //Reseteo la variable que cuenta los epochs de cada episodio
        stepsEpoch = 0;

        EndEpisode();
    }

    public void Fail()
    {
        // Establezco la recompensa de -1.
        AddReward(-0.1f);

        // By marking an agent as done AgentReset() will be called automatically.
        

        // Swap ground material for a bit to indicate we failed.
        StartCoroutine(SwapGroundMaterial(generalSettings.failMaterial, 0.5f));
        Debug.Log("FRACASO");
        dockingTime = 0.02f * stepsEpoch;
        successfulDocking = -1;
        String first = robotPos.ToString();
        String second = dockingTime.ToString();
        String third = stepsEpoch.ToString();
        String fourth = successfulDocking.ToString();
        String fifth = checkpoint.ToString();
        //Debug.Log("Docking not successful in epoch " + stepsEpoch);
        var newLine = string.Format("{0};{1};{2};{3};{4}", first, second, third, fourth, fifth);
        Debug.Log(newLine);
        csv.AppendLine(newLine);

        stepsEpoch = 0;
        EndEpisode();
    }

    public void DetectGoodCheckpoint()
    {
        AddReward(0.1f);
        //Debug.Log("Galletita" );
        //Para guardar en Excel que se ha hecho un acoplamiento correcto.
        checkpoint = 1;
    }
    public void DetectBadCheckpoint()
    {
        AddReward(-1f);
        //Debug.Log("No Galleta");
        //Para guardar en Excel que se ha hecho un acoplamiento incierto.
        checkpoint = -1;
    }

    /*
      private void UpdateStats()
      {
          overallReward += this.GetCumulativeReward();
          overallSteps += this.StepCount;
          rewardValue.text = $"{overallReward.ToString("F2")}";
          episodesValue.text = $"{this.CompletedEpisodes}";
          stepValue.text = $"{overallSteps}";

      }
    */
    // Swap ground material, wait time seconds, then swap back to the regular material.

    IEnumerator SwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    //Para mover el agente
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
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        // Penalty given each step to encourage agent to finish task quickly.****
        AddReward(-1f / MaxStep);
        stepsEpoch++;
        if (stepsEpoch == 2250)  //CAMBIAR ESTO PARA CAMBIAR EL TIEMPO 30S = 1500   MaxStep-1  45S = 2250
        {
            Fail();
            dockingTime = 0.02f * MaxStep;

        }
    }

//Si lo tengo puesto en heuristic, para las demostraciones humanas:**
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
    // In the editor, if "Reset On Done" is checked then AgentReset() will be
    // called automatically anytime we mark done = true in an agent script.
    public override void OnEpisodeBegin()
    {
        File.WriteAllText(@"C:\Users\usuario\Desktop\Datos\data_" + epoch + ".csv", csv.ToString());
        checkpoint = 0;
        var rotation = UnityEngine.Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        //ResetBlock(); --------------
        transform.position = GetRandomSpawnPos();
        
        //Calcular Distancia al Docking
        //distanceFromDocker = Vector3.Distance(robot.transform.position, dockingStation.transform.position);
        m_agentRigid.velocity = Vector3.zero;
        m_agentRigid.angularVelocity = Vector3.zero;

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
