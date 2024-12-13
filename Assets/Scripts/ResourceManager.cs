using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    // Dictionary of resource locations, where key is resource type (based on GameObject tags)
    public Dictionary<string, List<Transform>> resourceLocations = new Dictionary<string, List<Transform>>();
    private Dictionary<string, int> resources = new Dictionary<string, int>();
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize the resource locations
        InitializeResourceLocations();
        resources["Wood"] = 50;
        resources["Food"] = 30;
        resources["Stone"] = 20;
        resources["Water"] = 100;
    }

    void InitializeResourceLocations()
    {
        // Find all GameObjects tagged with resource types (e.g., "Food", "Wood", etc.)
        var allResourceObjects = GameObject.FindGameObjectsWithTag("Food")
            .Concat(GameObject.FindGameObjectsWithTag("Wood"))
            .Concat(GameObject.FindGameObjectsWithTag("Stone"))
            .Concat(GameObject.FindGameObjectsWithTag("Water"))
            .ToList();

        Debug.Log("Found " + allResourceObjects.Count + " objects");

        foreach (var resourceObject in allResourceObjects)
        {
            string resourceTag = resourceObject.tag;
            if (!resourceLocations.ContainsKey(resourceTag))
            {
                resourceLocations[resourceTag] = new List<Transform>();
            }
            resourceLocations[resourceTag].Add(resourceObject.transform);
        }
    }
    public bool RequestResource(string resourceType)
    {
        if (!resources.ContainsKey(resourceType))
        {
            Debug.LogWarning($"Resource type '{resourceType}' does not exist.");
            return false;
        }

        if (resources[resourceType] > 0)
        {
            resources[resourceType]--;
            Debug.Log($"Resource '{resourceType}' decremented. Remaining: {resources[resourceType]}.");
            return true;
        }
        else
        {
            Debug.LogWarning($"Resource '{resourceType}' is depleted!");
            return false;
        }
    }
    public Transform GetNearestResourcePosition(string resourceType, Vector3 currentPosition)
    {
        if (!resourceLocations.ContainsKey(resourceType)) return null;

        // Find the nearest resource position
        Transform nearestTransform = null;
        float shortestDistance = float.MaxValue;

        foreach (var resourceTransform in resourceLocations[resourceType])
        {
            float distance = Vector3.Distance(currentPosition, resourceTransform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestTransform = resourceTransform;
            }
        }

        return nearestTransform;
    }

    public bool IsResourceAvailable(string resourceType, float amount)
    {
        // Assuming each resource location provides a unit of the resource
        if (resourceLocations.ContainsKey(resourceType) && resourceLocations[resourceType].Count > 0)
        {
            return true;  // If there are resource locations for that type
        }
        return false; // No available resource of this type
    }

    public void GatherResource(string resourceType, float amount)
    {
        // Handle resource gathering (e.g., remove resource or reduce quantity)
        Debug.Log($"Gathering {amount} of {resourceType}");
    }

    public float GetTotalResource(string resourceType)
    {
        return resourceLocations.ContainsKey(resourceType) ? resourceLocations[resourceType].Count : 0f;
    }

    public float GetResourceNeed(string resourceType)
    {
        return 5f; // Placeholder value
    }
}
