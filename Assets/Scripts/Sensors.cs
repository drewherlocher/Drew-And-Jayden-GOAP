using Newtonsoft.Json.Bson;
using System;
using UnityEngine;


[RequireComponent(typeof(CircleCollider2D))]
public class Sensor : MonoBehaviour
{
    [SerializeField] float detectionRadius = 3f;
    [SerializeField] float timerInterval = 1f;

    CircleCollider2D detectionRange;

    public event Action OnTargetChanged = delegate { };
    public Vector3 TargetPosition => target ? target.transform.position : Vector3.zero;
    public bool IsTargetInRange => TargetPosition != Vector3.zero;

    GameObject target;
    Vector3 lastPosition;
    CountdownTimer timer;

    private void Awake()
    {
        detectionRange = GetComponent<CircleCollider2D>();
        detectionRange.isTrigger = true;
        detectionRange.radius = detectionRadius;
    }

    private void Start()
    {
        timer = new CountdownTimer(timerInterval);
        timer.OnTimerStop += () =>
        {
            if(target != null)
            UpdateTargetPosition(target);
            timer.Start();
        };
        timer.Start();
    }

    private void Update()
    {
        timer.Tick(Time.deltaTime);
    }

    void UpdateTargetPosition(GameObject target = null)
    {
        this.target = target;
        if (IsTargetInRange && (lastPosition != TargetPosition || lastPosition != Vector3.zero))
        {
            lastPosition = TargetPosition;
            OnTargetChanged.Invoke();
        }
    }

/*    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Resource")) return;
        UpdateTargetPosition(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Resource")) return;
        UpdateTargetPosition();
    }*/
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        UpdateTargetPosition(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        UpdateTargetPosition();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsTargetInRange? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
