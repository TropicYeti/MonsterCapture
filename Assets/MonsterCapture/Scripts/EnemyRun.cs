using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRun : MonoBehaviour, ITrappable, IState
{

    //The player gameobject
    private GameObject player;

    //The speed for the ai when wandering and running
    [SerializeField] private float wanderSpeed, runSpeed;

    [Tooltip("the max amount of time the mob will be interested in the player even when it's no longer being observed")]
    [SerializeField] private float maxRememberTime;

    [Tooltip("The amount of time the ai will remain alerted after spotting the player")]
    [SerializeField] private float maxAlertTime;

    //What the current ai is determining to do
    [Tooltip("Keeps track of what the current state is")]
    [SerializeField] public State state;

    [Tooltip("the centrepoint from where the calulation of where to move next will occur")]
    [SerializeField] private Transform wanderCentrePoint;

    [Tooltip("The range of distances the ai will calculate its next movements")]
    [SerializeField] private float minRange, maxRange;

    [Tooltip("The range of time the ai will take before moving again")]
    [SerializeField] private float minWaitTime, maxWaitTime;

    [Tooltip("The distance the ai will react to the player")]
    [SerializeField] private float alertDistance, chaseDistance, tooCloseDistance, attackDistance;


    private Vector3 oldDestination;


    public bool isBeingCaptured { get; set; }
    private float timer = 1;
    private Vector3 originalScale;
    public enum State
    {
        Idle,
        Wandering,
        Alerted,
        Running
    };

    private Rigidbody rb;

    private NavMeshAgent agent;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Getting components for rigidbody and navmesh agent
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

        player = FindFirstObjectByType<PlayerMovement>().gameObject;

        originalScale = transform.localScale;

        //starts the ai in an idle state
        state = State.Idle;
        NextState();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void NextState()
    {
        switch (state)
        {
            case State.Idle:
                StartCoroutine(IdleState());
                break;
            case State.Wandering:
                StartCoroutine(WanderingState());
                break;
            case State.Alerted:
                StartCoroutine(AlertedState());
                break;
            case State.Running:
                StartCoroutine(RunningState());
                break;
        }
    }

    IEnumerator IdleState()
    {
        Debug.Log("Entering Idle State");
        //Make sure the object has no active destination while idle
        agent.ResetPath();
        bool finishedWaiting = false;
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        while (state == State.Idle)
        {
            if (CheckLOS(player, alertDistance))
            {

                finishedWaiting = true;
                state = State.Alerted;
            }
            waitTime -= Time.deltaTime;
            if (waitTime <= 0.0f)
            {
                Vector3 point;
                if (RandomPoint(agent.transform.position, maxRange, out point))
                {
                    //draws point the AI will navigate to
                    NextIdleDestination(point);
                    //sets the state to wandering
                    finishedWaiting = true;
                    state = State.Wandering;
                }
            }
            yield return null; //waits for a frame
        }
        Debug.Log("Exiting Idle State");
        NextState();
    }

    IEnumerator WanderingState()
    {
        Debug.Log("Entering Wandering State");
        float chance = Random.Range(0f, 1f);
        //set the agent's speed to the wandering speed
        agent.speed = wanderSpeed;
        while (state == State.Wandering)
        {
            if (CheckLOS(player, alertDistance))
            {
                state = State.Alerted;
                break;
            }
            if (agent.remainingDistance <= agent.stoppingDistance) //if the agent is done with its current path...
            {

                if (chance <= 0.5f)
                {
                    //finds a new point and immediately heads towards it
                    Vector3 point;
                    if (RandomPoint(agent.transform.position, maxRange, out point))
                    {
                        //draws point the AI will navigate to
                        NextIdleDestination(point);
                    }
                    chance = Random.Range(0f, 1f);
                }
                else if (chance > 0.5f)
                {
                    //the ai is done wandering and will Idle
                    state = State.Idle;
                }
            }
            yield return null;
        }
        Debug.Log("Exiting Wandering State");
        NextState();
    }

    IEnumerator AlertedState()
    {
        Debug.Log("Entering Alerted State");
        //save the original destination before resetting it 
        oldDestination = agent.destination; agent.ResetPath();

        //the original alert/remember time upon entering this state
        float ogAlertTime = maxAlertTime;
        float ogRememberTime = maxRememberTime;

        while (state == State.Alerted)
        {
            //The distance between the player and ai
            float distance = Vector3.Distance(agent.transform.position, player.transform.position);


            //check if the player is still within LOS
            if (CheckLOS(player, alertDistance))
            {
                //reset the remember time
                maxRememberTime = ogRememberTime;

                //check if the player is too close
                if (distance <= tooCloseDistance)
                {
                    state = State.Running;
                }
            }
            else
            {
                //as the player isn't being observed, remember time goes down
                maxRememberTime -= Time.deltaTime;
                maxAlertTime += Time.deltaTime / 2;
                if (maxAlertTime >= 5f)
                {
                    maxAlertTime = 5f;
                }

                if (maxRememberTime <= 0f)
                {
                    state = State.Idle;
                }
            }
            yield return null;
        }
        //reset the alert/remember time
        maxAlertTime = ogAlertTime;
        maxRememberTime = ogRememberTime;

        Debug.Log("Exiting Alerted State");
        NextState();
    }

    IEnumerator RunningState()
    {
        Debug.Log("Entering Running State");

        //Set the speed to the run speed
        agent.speed = runSpeed;

        //the original remember time
        float ogRememberTime = maxRememberTime;

        while (state == State.Running)
        {
            Vector3 dirFromPlayer = (transform.position - player.transform.position );


            dirFromPlayer = dirFromPlayer.normalized * 4f;
            agent.destination = transform.position +  dirFromPlayer;



            if (!CheckLOS(player, alertDistance))
            {
                maxRememberTime -= Time.deltaTime;
                if (maxRememberTime <= 0f)
                {
                    state = State.Alerted;
                }
            }
            else
            {
                maxRememberTime = ogRememberTime;
            }
                yield return null;
        }
        maxRememberTime = ogRememberTime;
        Debug.Log("Exiting Running State");
        NextState();
    }

    bool RandomPoint(Vector3 center, float distance, out Vector3 result)
    {
        Vector2 randompoint = Random.insideUnitCircle * (maxRange - minRange);
        randompoint += randompoint.normalized * minRange;


        //Vector3 randompoint = center + Random.insideUnitSphere *  distance; //random point in a sphere
        NavMeshHit hit;
        if (NavMesh.SamplePosition(center + new Vector3(randompoint.x, 0f, randompoint.y), out hit, maxRange, NavMesh.AllAreas)) //Ddocumentation: https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html
        {
            result = hit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    [Tooltip("Sets the next idle destination for the ai")]
    private void NextIdleDestination(Vector3 point)
    {
        //draws point the AI will navigate to
        Debug.DrawRay(point, Vector3.up, Color.magenta, 1.0f);
        agent.SetDestination(point);
    }

    private void OnDrawGizmosSelected()
    {
        if (wanderCentrePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wanderCentrePoint.position, maxRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wanderCentrePoint.position, minRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wanderCentrePoint.position, alertDistance);
        }
    }

    public bool CheckLOS(GameObject target, float detectionRange)
    {
        //The position of the observer
        Vector3 observerPosition = transform.position;

        //The direction of the target
        Vector3 direction = (target.transform.position - observerPosition).normalized;

        //the distance of the raycast


        //Check if the raycast between the two is uninterrupted
        RaycastHit hit;
        bool hitTarget = Physics.Raycast(observerPosition, direction, out hit, detectionRange);

        //check for collisions
        if (hitTarget)
        {

            //if there is nothing between the ai and target...
            if (hit.collider.gameObject == target)
            {
                return true;
            }
            else
            {
                //LOS is blocked
                return false;
            }
        }
        else
        {
            //target is out of range
            return false;
        }
    }


    public bool CaptureAnimation()
    {
        isBeingCaptured = true;
        timer -= Time.deltaTime * 1f;
        transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, timer);

        if (timer <= 0)
        {
            return false;
        }

        return true;
    }

    public int PointValue()
    {
        return 2;
    }

    public string StateDisplay()
    {
        switch (state)
        {
            case State.Idle:
                return "Idle";
            case State.Wandering:
                return "Wandering";
            case State.Alerted:
                return "Alerted";
            case State.Running:
                return "Running";
            default:
                return "Error";
        }
        //   return "Error";
    }

    //Search up Capela Games on github to find the other scripts
}
