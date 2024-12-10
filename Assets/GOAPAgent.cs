using Newtonsoft.Json.Bson;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

// TODO make stats and have them update and gotten from world state

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GOAPAgent : MonoBehaviour
{
    [Header("Sensors")]
    [SerializeField] Sensor moveSensor;
    [SerializeField] Sensor doSensor;

    [Header("Known Locations")]
    [SerializeField] Transform restingPosition;
    [SerializeField] Transform foodPosition;
    [SerializeField] Transform waterPosition;
    [SerializeField] Transform woodPosition;
    [SerializeField] Transform stonePosition;

    CircleCollider2D circleCollider;
    SpriteRenderer spriteRenderer;
    Rigidbody2D rb;

    [Header("Stats")]
    [SerializeField] float foodNeeded;

    CountdownTimer statsTimer;

    GameObject target;
    Vector3 destination;

    AgentGoal lastGoal;
    public AgentGoal currentGoal;
    public ActionPlan actionPlan;
    public AgentAction currentAction;

    public Dictionary<string, AgentBelief> beliefs;
    public HashSet<AgentAction> actions;
    public HashSet<AgentGoal> goals;

    NavAgent navAgent;

    IGOAPPlanner gPlanner;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        navAgent = GetComponent<NavAgent>();
        rb.freezeRotation = true;

        gPlanner = new GOAPPlanner();
    }

    private void Start()
    {
        SetupTimers();
        SetupBeliefs();
        SetupActions();
        SetupGoals();
    }

    void SetupBeliefs()
    {
        beliefs = new Dictionary<string, AgentBelief>();
        BeliefFactory factory = new BeliefFactory(this, beliefs);

        factory.AddBelief("Nothing", () => false);
        factory.AddBelief("AgentIdle", () => !navAgent.IsAtDestination());
        factory.AddBelief("AgentMoving", () => !navAgent.IsAtDestination());

    }

    void SetupActions()
    {
        actions = new HashSet<AgentAction>();

        actions.Add(new AgentAction.ActionBuilder("Relax").WithStrategy(new IdleStrategy(5)).AddEffect(beliefs["Nothing"]).Build());
        actions.Add(new AgentAction.ActionBuilder("Wander").WithStrategy(new WanderStrategy(navAgent, 10)).AddEffect(beliefs["AgentMoving"]).Build());
    }

    void SetupGoals()
    {
        goals = new HashSet<AgentGoal>();
        goals.Add(new AgentGoal.GoalBuilder("Relax").WithPriority(1).WithDesiredEffect(beliefs["Nothing"]).Build());
        goals.Add(new AgentGoal.GoalBuilder("Wander").WithPriority(1).WithDesiredEffect(beliefs["AgentMoving"]).Build());
    }

    bool HasPath(Vector3 targetPosition)
    {
        return navAgent.HasPath(targetPosition);  // Now relies on NavAgent's HasPath
    }
    void SetupTimers()
    {
        statsTimer = new CountdownTimer(2f);
        statsTimer.OnTimerStop += () =>
        {
            UpdateStats();
            statsTimer.Start();
        };
        statsTimer.Start();
    }

    void UpdateStats()
    {

    }

    bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;
    void OnEnable() => moveSensor.OnTargetChanged += HandleTargetChanged;
    void OnDisable() => moveSensor.OnTargetChanged -= HandleTargetChanged;
    void MoveToTarget(Vector3 targetPosition)
    {
        if (navAgent.HasPath(targetPosition))
        {
            navAgent.SetDestination(targetPosition);
        }
    }
    void HandleTargetChanged()
    {
        Debug.Log("Target changed clearning GOAP");
        currentAction = null;
        currentGoal = null;
    }

    private void Update()
    {
        statsTimer.Tick(Time.deltaTime);
        
        if(currentAction == null)
        {
            Debug.Log("Calculating new plan");
            CalculatePlan();

            if (actionPlan != null && actionPlan.Actions.Count > 0)
            {
                navAgent.ResetPath();

                currentGoal = actionPlan.AgentGoal;
                currentAction = actionPlan.Actions.Pop();
                currentAction.Start();
                Debug.Log($"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
                Debug.Log($"Popped action: {currentAction.Name}");
            }
        }

        if (actionPlan != null && currentAction != null)
        {
            currentAction.Update(Time.deltaTime);

            if (currentAction.Complete)
            {
                Debug.Log($"{currentAction.Name} complete");
                currentAction.Stop();
                currentAction = null;

                if (actionPlan.Actions.Count == 0)
                {
                    Debug.Log("Plan complete");
                    lastGoal = currentGoal;
                    currentGoal = null;
                }
            }
        }
    }

    void CalculatePlan()
    {
        var priorityLevel = currentGoal?.Priority ?? 0;

        HashSet<AgentGoal> goalsToCheck = goals;

        if (currentGoal != null)
        {
            Debug.Log("Found current goal, checking if other more important ones");
            goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
        }

        var potentialPlan = gPlanner.Plan(this, goalsToCheck, lastGoal);
        if (potentialPlan != null)
        {
            actionPlan = potentialPlan;
        }
    }
}
