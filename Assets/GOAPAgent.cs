using Mono.Cecil;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GOAPAgent : MonoBehaviour
{
    [Header("Sensors")]
    [SerializeField] Sensor moveSensor;
    [SerializeField] Sensor doSensor;

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

    [SerializeField] private ResourceManager resourceManager;

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

        // Basic agent status beliefs
        factory.AddBelief("Nothing", () => false);  // Nothing is happening
        factory.AddBelief("AgentIdle", () => !navAgent.IsAtDestination());
        factory.AddBelief("AgentMoving", () => !navAgent.IsAtDestination());

        // Beliefs for resource needs (this can be used to prioritize actions)
        factory.AddBelief("WaterNeeded", () => ResourceManager.Instance.GetResourceNeed("Water") > 0f);
        factory.AddBelief("FoodNeeded", () => ResourceManager.Instance.GetResourceNeed("Food") > 0f);
        factory.AddBelief("WoodNeeded", () => ResourceManager.Instance.GetResourceNeed("Wood") > 0f);
        factory.AddBelief("StoneNeeded", () => ResourceManager.Instance.GetResourceNeed("Stone") > 0f);

        // Add dynamic location-based beliefs for each resource
        foreach (var resourceType in ResourceManager.Instance.resourceLocations.Keys)
        {
            foreach (var position in ResourceManager.Instance.resourceLocations[resourceType])
            {
                string beliefKey = $"AgentAt{resourceType}Location_{position.name}";
                Debug.Log($"Adding belief for {beliefKey} at position {position.position}");
                factory.AddLocationBelief(beliefKey, 2f, position);
            }
        }

        // Add availability beliefs for resources
        AddResourceAvailabilityBelief("Food");
        AddResourceAvailabilityBelief("Wood");
        AddResourceAvailabilityBelief("Stone");
        AddResourceAvailabilityBelief("Water");
    }

    void AddResourceAvailabilityBelief(string resourceType)
    {
        beliefs.Add($"{resourceType}Available", new AgentBelief.BeliefBuilder($"{resourceType}Available")
            .WithCondition(() => ResourceManager.Instance.IsResourceAvailable(resourceType, 1f))  // Checks if the resource is available
            .Build());
    }



    public bool RequestResource(string resourceType, Transform agent, float amount)
    {
        Debug.Log($"Requesting {resourceType} for agent at position {agent.position}");

        // Ensure the resource is available
        if (!ResourceManager.Instance.IsResourceAvailable(resourceType, amount))
        {
            Debug.Log($"Resource {resourceType} not available.");
            return false;
        }

        // Gather the resource if it's available
        ResourceManager.Instance.GatherResource(resourceType, amount);
        Debug.Log($"{resourceType} gathered.");
        return true;
    }

    void SetupActions()
    {
        actions = new HashSet<AgentAction>();

        // Add idle action (Agent does nothing)
        actions.Add(new AgentAction.ActionBuilder("Relax")
            .WithStrategy(new IdleStrategy(5))
            .AddEffect(beliefs["Nothing"])
            .Build());

        // Add action to wander around
        actions.Add(new AgentAction.ActionBuilder("Wander")
            .WithStrategy(new WanderStrategy(navAgent, 10))
            .AddEffect(beliefs["AgentMoving"])
            .Build());

        // Action to collect Wood
        actions.Add(new AgentAction.ActionBuilder("CollectWood")
     .WithStrategy(new MoveStrategy(
         navAgent,
         () => ResourceManager.Instance.GetNearestResourcePosition("Wood", transform.position).position,
         () => ResourceManager.Instance.RequestResource("Wood")))  // Callback
     .AddPrecondition(beliefs["WoodAvailable"])
     .AddEffect(beliefs["Nothing"])
     .Build());

        // Action to collect Food
        actions.Add(new AgentAction.ActionBuilder("CollectFood")
            .WithStrategy(new MoveStrategy(
                navAgent,
                () => ResourceManager.Instance.GetNearestResourcePosition("Food", transform.position).position,
                () => ResourceManager.Instance.RequestResource("Food")))  // Callback
            .AddPrecondition(beliefs["FoodAvailable"])
            .AddEffect(beliefs["Nothing"])
            .Build());

        // Action to collect Stone
        actions.Add(new AgentAction.ActionBuilder("CollectStone")
             .WithStrategy(new MoveStrategy(
                 navAgent,
                 () => ResourceManager.Instance.GetNearestResourcePosition("Stone", transform.position).position,
                 () => ResourceManager.Instance.RequestResource("Stone")))  // Callback
             .AddPrecondition(beliefs["StoneAvailable"])
             .AddEffect(beliefs["Nothing"])
             .Build());


        // Action to collect Water
        actions.Add(new AgentAction.ActionBuilder("CollectWater")
             .WithStrategy(new MoveStrategy(
                 navAgent,
                 () => ResourceManager.Instance.GetNearestResourcePosition("Water", transform.position).position,
                 () => ResourceManager.Instance.RequestResource("Water")))  // Callback
             .AddPrecondition(beliefs["WaterAvailable"])
             .AddEffect(beliefs["Nothing"])
             .Build());

    }


    void SetupGoals()
    {
        goals = new HashSet<AgentGoal>();
        goals.Add(new AgentGoal.GoalBuilder("Relax")
            .WithPriority(1)
            .WithDesiredEffect(beliefs["Nothing"])
            .Build());

        goals.Add(new AgentGoal.GoalBuilder("Wander")
            .WithPriority(2)
            .WithDesiredEffect(beliefs["AgentMoving"])
            .Build());

        goals.Add(new AgentGoal.GoalBuilder("CollectResources")
            .WithPriority(3)
            .WithDesiredEffect(beliefs["FoodAvailable"])  // Collect food if it's available
            .WithDesiredEffect(beliefs["WoodAvailable"])  // Collect wood if it's available
            .WithDesiredEffect(beliefs["StoneAvailable"])  // Collect stone if it's available
            .WithDesiredEffect(beliefs["WaterAvailable"])  // Collect water if it's available
            .Build());

        // Ensure there are goals in the collection
        Debug.Log($"Goals count: {goals.Count}");
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
        // Update agent stats if needed
    }

    void OnEnable() => moveSensor.OnTargetChanged += HandleTargetChanged;
    void OnDisable() => moveSensor.OnTargetChanged -= HandleTargetChanged;




    void HandleTargetChanged()
    {
        Debug.Log("Target changed, clearing GOAP");
        currentAction = null;
        currentGoal = null;
    }

    private void Update()
    {
        statsTimer.Tick(Time.deltaTime);

        if (currentAction == null)
        {
            Debug.Log("Calculating new plan");
            CalculatePlan();

            if (actionPlan != null && actionPlan.Actions.Count > 0)
            {
                navAgent.ResetPath();

                currentGoal = actionPlan.AgentGoal;
                Debug.Log($"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
                currentAction = actionPlan.Actions.Pop();
                Debug.Log($"Popped action: {currentAction.Name}");
                currentAction.Start();
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
