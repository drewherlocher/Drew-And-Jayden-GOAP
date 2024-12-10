using JetBrains.Annotations;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Analytics;

public interface IActionStrategy
{
    bool CanPerform {  get; }
    bool Complete { get; }

    void Start() { }

    void Update(float deltaTime) { }
    
    void Stop() { }
}

public class WanderStrategy : IActionStrategy
{

    readonly NavAgent agent;
    readonly float wanderRadius;
    readonly float stopDistance = 2f;

    public bool CanPerform => !Complete;
    public bool Complete => agent.IsAtDestination();

    public WanderStrategy(NavAgent agent, float wanderRadius)
    {
        this.agent = agent;
        this.wanderRadius = wanderRadius;
    }

    public void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle * wanderRadius;
            Vector3 randomPosition = agent.transform.position + (Vector3)randomDirection;

            // Check if path to the random position is clear
            if (agent.HasPath(randomPosition))
            {
                agent.SetDestination(randomPosition);
                return;
            }
        }

        // Fallback: If no valid random position, stop
        agent.StopMovement();
    }
}

public class IdleStrategy : IActionStrategy
{
    public bool CanPerform => true;
    public bool Complete { get; private set; }
    
    readonly CountdownTimer timer;
   
    public IdleStrategy(float duration)
    {
        timer = new CountdownTimer(duration);
        timer.OnTimerStart += () => Complete = false;
        timer.OnTimerStop += () => Complete = true;
    }

    public void Start() => timer.Start();
    public void Update(float deltaTime) => timer.Tick(deltaTime);
}