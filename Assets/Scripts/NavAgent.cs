using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class NavAgent : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float stoppingDistance = 0.2f;
    [SerializeField] LayerMask obstacleLayer; // Layer mask for obstacles (e.g., walls)
    [SerializeField] float pathUpdateInterval = 0.5f; // Time between path updates

    private Rigidbody2D rb;
    private Vector3 destination;
    private bool isMoving;
    private float currentPathUpdateTime;
    private bool pathPending;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Handle movement
        if (isMoving && !pathPending)
        {
            MoveToTarget(destination);
        }

        // Optionally update path periodically (if pathfinding system is expanded in the future)
        if (currentPathUpdateTime <= 0)
        {
            currentPathUpdateTime = pathUpdateInterval;
            UpdatePath();
        }
        else
        {
            currentPathUpdateTime -= Time.deltaTime;
        }
    }

    public void SetDestination(Vector3 targetPosition)
    {
        destination = targetPosition;
        isMoving = true;
        pathPending = true; // Indicate that the agent is calculating the path
    }

    private void MoveToTarget(Vector3 targetPosition)
    {
        // Move the agent towards the destination
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget > stoppingDistance)
        {
            Vector2 direction = (targetPosition - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
        }
    }

    public bool IsAtDestination()
    {
        return Vector3.Distance(transform.position, destination) <= stoppingDistance;
    }

    // Checks if there is a clear path to the target destination
    public bool HasPath(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);

        // Perform a raycast to check for obstacles between the agent and the target
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);

        return hit.collider == null; // No obstacles, path is clear
    }

    // Checks if the agent is still processing its path (simulating NavMeshAgent's pathPending)
    public bool PathPending()
    {
        return pathPending;
    }

    // Simulate pathfinding completion (e.g., when the agent reaches the destination or path is clear)
    private void UpdatePath()
    {
        if (IsAtDestination() || HasPath(destination))
        {
            pathPending = false; // Pathfinding is complete
        }
    }

    // Optionally stop movement manually
    public void StopMovement()
    {
        isMoving = false;
        rb.linearVelocity = Vector2.zero;
    }

    // Optionally reset destination and pathfinding state
    public void ResetPath()
    {
        pathPending = false;
        isMoving = false;
        rb.linearVelocity = Vector2.zero;
    }
}