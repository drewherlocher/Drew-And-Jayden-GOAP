using UnityEngine;
using System.Collections;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Planted, Ready }
    [Header("Plot State")]
    [SerializeField]
    public PlotState currentState = PlotState.Empty; // All plots start as empty

    private SpriteRenderer spriteRenderer;

    [Header("Debug Options")]
    public bool enableDebugLogs = true; // Toggle debug logs
    public float growthTime = 5f; // Time it takes for crops to grow

    // Initialize spriteRenderer in Awake to avoid NullReferenceException in OnValidate
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnValidate()
    {
        if (spriteRenderer != null)
        {
            UpdatePlotColor(); // Update the plot color when the state is changed in the Inspector
        }
    }

    void Start()
    {
        UpdatePlotColor();

        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] Initialized as {currentState}");
        }
    }

    // Called when a villager interacts with the plot
    public void Interact()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] Interact called. Current State: {currentState}");
        }

        switch (currentState)
        {
            case PlotState.Empty:
                PlantSeed();
                break;
            case PlotState.Ready:
                HarvestCrop();
                break;
            case PlotState.Planted:
                if (enableDebugLogs)
                {
                    Debug.Log($"[{name}] Plot is currently planted, cannot interact further until it's ready.");
                }
                break;
        }
    }

    private void PlantSeed()
    {
        currentState = PlotState.Planted;
        UpdatePlotColor();

        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] State changed to Planted.");
        }

        StartCoroutine(GrowCrops()); // Simulate crop growth
    }

    private void HarvestCrop()
    {
        currentState = PlotState.Empty;
        UpdatePlotColor();

        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] State changed to Empty (Harvested).");
        }
    }

    private void UpdatePlotColor()
    {
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case PlotState.Empty:
                spriteRenderer.color = new Color(0.6f, 0.3f, 0f); // Brown
                break;
            case PlotState.Planted:
                spriteRenderer.color = Color.yellow; // Yellow
                break;
            case PlotState.Ready:
                spriteRenderer.color = Color.green; // Green
                break;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] Color updated to match state: {currentState}");
        }
    }

    private IEnumerator GrowCrops()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] Crops are growing... Time remaining: {growthTime}s");
        }

        yield return new WaitForSeconds(growthTime); // Simulate growth time

        currentState = PlotState.Ready;
        UpdatePlotColor();

        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] Crops are ready to harvest!");
        }
    }

    // Draw Gizmos in the Scene View to indicate plot state
    private void OnDrawGizmos()
    {
        if (spriteRenderer == null) return;

        Gizmos.color = currentState switch
        {
            PlotState.Empty => new Color(0.6f, 0.3f, 0f), // Brown
            PlotState.Planted => Color.yellow, // Yellow
            PlotState.Ready => Color.green, // Green
            _ => Color.white
        };

        Gizmos.DrawCube(transform.position, spriteRenderer.bounds.size);
    }
}
