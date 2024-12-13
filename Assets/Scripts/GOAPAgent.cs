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
        /*        beliefs.Add($"{resourceType}Available", new AgentBelief.BeliefBuilder($"{resourceType}Available")
                    .WithCondition(() =>
                    {
                        bool available = ResourceManager.Instance.IsResourceAvailable(resourceType, 1f);
                        Debug.Log($"{resourceType} available:::::::::: {available}");
                        return available;
                    })
                    .Build());*/

        //Debug.Log("Erm Add Resource Availabiltiy Belief???");
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
            .WithStrategy(new MoveStrategy
            (navAgent,() => ResourceManager.Instance.GetNearestResourcePosition("Wood", transform.position).position,
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


        /*        goals.Add(new AgentGoal.GoalBuilder("CollectResources")/
            .WithPriority(3)
            .WithDesiredEffect(beliefs["WoodAvailable"])  // Collect food if it's available
            .Build());

                goals.Add(new AgentGoal.GoalBuilder("CollectResources")
            .WithPriority(3)
            .WithDesiredEffect(beliefs["StoneAvailable"])  // Collect food if it's available
            .Build());

                goals.Add(new AgentGoal.GoalBuilder("CollectResources")
            .WithPriority(3)
            .WithDesiredEffect(beliefs["WaterAvailable"])  // Collect food if it's available
            .Build());*/

        // Ensure there are goals in the collection
        //Debug.Log($"Goals count: {goals.Count}");
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
        //Debug.Log("Target changed, clearing GOAP");
        currentAction = null;
        currentGoal = null;
    }

    private void Update()
    {
        statsTimer.Tick(Time.deltaTime);

        if (currentAction == null || actionPlan.Actions.Count ==0)
        {
            //Debug.Log("Calculating new plan");
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

    /*    void CalculatePlan()
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
                Debug.Log("Potential Plan Found!");
                actionPlan = potentialPlan;
            }
        }*/

    void CalculatePlan()
    {
        var priorityLevel = currentGoal?.Priority ?? 0;

        // Start by considering all goals
        HashSet<AgentGoal> goalsToCheck = goals;

        if (currentGoal != null)
        {
            Debug.Log("Found current goal, checking if more important ones exist");
            // Filter out goals with lower priority than the current goal
            goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
        }

        // Now dynamically prioritize goals based on available resources
        var dynamicGoalsToCheck = new HashSet<AgentGoal>();

        foreach (var goal in goalsToCheck)
        {
            if (goal.Name == "CollectResources")
            {
                Debug.Log($"Evaluating goal: {goal.Name} with priority {goal.Priority}, checking if resources are available...");
                bool isResourceAvailable = false;

                // Check if any of the DesiredEffects correspond to a resource that is available
                foreach (var effect in goal.DesiredEffects)
                {
                    // Check if the resource is available in the ResourceManager
                    if (effect.Name == effect.Name)  // We assume a fixed amount for now
                    {
                        isResourceAvailable = true;
                        Debug.Log($"Resource {effect.Name} is available.");
                        break;  // No need to check further if at least one resource is available
                    }
                }

                // If any required resource is available, add this goal to the dynamic set
                if (isResourceAvailable)
                {
                    Debug.Log("Goal Added: " + goal.Name);
                    dynamicGoalsToCheck.Add(goal);
                }

            }
            else
            {
                // Add non-resource-based goals (like "Relax" or "Wander")
                dynamicGoalsToCheck.Add(goal);
            }
        }

        // After filtering, sort goals by priority (higher priority goals first)
        var sortedGoals = dynamicGoalsToCheck.OrderByDescending(g => g.Priority).ToList();

        // If we have any valid goals after filtering, set the current goal and calculate the plan
        if (sortedGoals.Count > 0)
        {
            currentGoal = sortedGoals[0];
            Debug.Log($"Current Goal: {currentGoal.Name} (Priority: {currentGoal.Priority})");

            // Use the goal planner to generate a potential plan based on the filtered goals
            lastGoal = currentGoal;
            Debug.Log("Last Goal: " + lastGoal.Name);
            var potentialPlan = gPlanner.Plan(this, dynamicGoalsToCheck, lastGoal);

            if (potentialPlan != null)
            {
                actionPlan = potentialPlan;
                Debug.Log("Potential Plan Found!: " + $"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
            }
            else
            {
                Debug.Log("Potential Path is nulll :(");
            }
        }
        else
        {
            Debug.Log("No valid goals available.");
        }
    }

}

//drews old one i didnt want to delete it, i copied most of it tbh

/*using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Rendering.VolumeComponent;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GOAPAgent : MonoBehaviour
{
    [Header("Sensors")]
    [SerializeField] Sensor chaseSensor;
    [SerializeField] Sensor attackSensor;

    [Header("Known Locations")]
    [SerializeField] Transform restingPosition;
    [SerializeField] Transform woodPlace;

    NavMeshAgent navMeshAgent;
    Rigidbody2D rb;

    [Header("Stats")]
    public float health = 100;
    public float stamina = 100;

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

    IGOAPPlanner gPlanner;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        gPlanner = new GOAPPlanner();
    }

    void Start()
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

        factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
        factory.AddBelief("AgentWander", () => !navMeshAgent.hasPath);
        factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);

*//*        factory.AddBelief("AgentHealthLow", () => health < 30);
        factory.AddBelief("AgentIsHealthy", () => health >= 50);
        factory.AddBelief("AgentStaminaLow", () => stamina < 10);
        factory.AddBelief("AgentIsRested", () => stamina >= 50);

        factory.AddLocationBelief("AgentAtDoorOne", 3f, doorOnePosition);
        factory.AddLocationBelief("AgentAtDoorTwo", 3f, doorTwoPosition);
        factory.AddLocationBelief("AgentAtRestingPosition", 3f, restingPosition);
        factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

        factory.AddSensorBelief("PlayerInChaseRange", chaseSensor);
        factory.AddSensorBelief("PlayerInAttackRange", attackSensor);
        factory.AddBelief("AttackingPlayer", () => false); // Player can always be attacked, this will never become true*//*
    }

    void SetupActions()
    {
        actions = new HashSet<AgentAction>();

        actions.Add(new AgentAction.ActionBuilder("Relax")
            .WithStrategy(new IdleStrategy(5))
            .AddEffect(beliefs["Nothing"])
            .Build());

        actions.Add(new AgentAction.ActionBuilder("Wander Around")
            .WithStrategy(new WanderStrategy(navMeshAgent, 10))
            .AddEffect(beliefs["AgentMoving"])
            .Build());

        actions.Add(new AgentAction.ActionBuilder("MoveToEatingPosition")
            .WithStrategy(new TestingMoveStrat(navMeshAgent, () => woodPlace.position))
            .AddEffect(beliefs["AgentMoving"])
            .Build());
    }

    void SetupGoals()
    {
        goals = new HashSet<AgentGoal>();

        goals.Add(new AgentGoal.GoalBuilder("Chill Out")
            .WithPriority(1)
            .WithDesiredEffect(beliefs["Nothing"])
            .Build());

        goals.Add(new AgentGoal.GoalBuilder("Wander")
            .WithPriority(1)
            .WithDesiredEffect(beliefs["AgentMoving"])
            .Build());

        goals.Add(new AgentGoal.GoalBuilder("KeepWoodUp")
            .WithPriority(2)
            .WithDesiredEffect(beliefs["AgentMoving"])
            .Build());
    }

    void SetupTimers()
    {
        statsTimer = new CountdownTimer(2f);
        statsTimer.OnTimerStop += () => {
            UpdateStats();
            statsTimer.Start();
        };
        statsTimer.Start();
    }

    // TODO move to stats system
    void UpdateStats()
    {
    }

    bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;

    void OnEnable() => chaseSensor.OnTargetChanged += HandleTargetChanged;
    void OnDisable() => chaseSensor.OnTargetChanged -= HandleTargetChanged;

    void HandleTargetChanged()
    {
        Debug.Log("Target changed, clearing current action and goal");
        // Force the planner to re-evaluate the plan
        currentAction = null;
        currentGoal = null;
    }

    void Update()
    {
        statsTimer.Tick(Time.deltaTime);

        // Update the plan and current action if there is one
        if (currentAction == null)
        {
            Debug.Log("Calculating any potential new plan");
            CalculatePlan();

            if (actionPlan != null && actionPlan.Actions.Count > 0)
            {
                Debug.Log($"NavMeshAgent enabled: {navMeshAgent.enabled}, isOnNavMesh: {navMeshAgent.isOnNavMesh}" + " " + );
                navMeshAgent.ResetPath();

                currentGoal = actionPlan.AgentGoal;
                Debug.Log($"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
                currentAction = actionPlan.Actions.Pop();
                Debug.Log($"Popped action: {currentAction.Name}");
                // Verify all precondition effects are true
                if (currentAction.Preconditions.All(b => b.Evaluate()))
                {
                    currentAction.Start();
                }
                else
                {
                    Debug.Log("Preconditions not met, clearing current action and goal");
                    currentAction = null;
                    currentGoal = null;
                }
            }
        }

        // If we have a current action, execute it
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

        // If we have a current goal, we only want to check goals with higher priority
        if (currentGoal != null)
        {
            Debug.Log("Current goal exists, checking goals with higher priority");
            goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
        }

        var potentialPlan = gPlanner.Plan(this, goalsToCheck, lastGoal);
        if (potentialPlan != null)
        {
            actionPlan = potentialPlan;
        }
    }
}*/