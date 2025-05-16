using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaypointHelper : MonoBehaviour
{
    public TrackPathManager trackPathManager;
    public bool showConnectionGizmos = true;
    public float gizmoRadius = 0.5f;
    
    [Header("Terrain Settings")]
    public Terrain terrain;
    public float heightAboveTerrain = 0.5f;
    
    [Header("Track Mask Settings")]
    public Texture2D trackMask;
    public float waypointSpacing = 5f; // Distance between waypoints
    public int maxWaypoints = 50; // Maximum waypoints to create
    public float sampleRadius = 3f; // Radius to sample the track during path finding
    public float trackWidth = 10f; // Approximate width of your track
    
    [Header("Debug")]
    public bool showDebugLog = true; // Whether to show debug logs
    
    // Debug visualization
    private List<Vector3> _sampledPoints = new List<Vector3>();
    private List<Vector3> _rejectedPoints = new List<Vector3>();
    
    [ContextMenu("Generate Waypoints From Track Mask")]
    public void GenerateWaypointsFromTrackMask()
    {
        if (trackPathManager == null)
        {
            Debug.LogError("Track path manager not assigned!");
            return;
        }
        
        if (trackMask == null)
        {
            Debug.LogError("Track mask not assigned!");
            return;
        }
        
        if (terrain == null)
        {
            terrain = FindObjectOfType<Terrain>();
            if (terrain == null)
            {
                Debug.LogError("No terrain found in scene!");
                return;
            }
        }
        
        // Create or clear waypoints parent
        if (trackPathManager.waypointsParent == null)
        {
            GameObject parent = new GameObject("TrackWaypoints");
            trackPathManager.waypointsParent = parent.transform;
            trackPathManager.waypointsParent.parent = trackPathManager.transform;
        }
        else
        {
            // Clear existing waypoints
            while (trackPathManager.waypointsParent.childCount > 0)
            {
                DestroyImmediate(trackPathManager.waypointsParent.GetChild(0).gameObject);
            }
        }
        
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        
        // Log terrain info for debugging
        if (showDebugLog)
        {
            Debug.Log($"Terrain position: {terrainPos}");
            Debug.Log($"Terrain size: {terrainData.size}");
            Debug.Log($"Track mask size: {trackMask.width}x{trackMask.height}");
        }
        
        // Find a starting point on the track
        Vector3 startPoint = Vector3.zero;
        bool foundStart = false;
        
        // Clear debug data
        _sampledPoints.Clear();
        _rejectedPoints.Clear();
        
        // Sample points to find the track - FIX: Use proper terrain sampling
        float stepSize = Mathf.Min(terrainData.size.x, terrainData.size.z) / 20f; // Adjust step size based on terrain
        
        for (float xPercent = 0; xPercent < 1.0f; xPercent += 0.05f)
        {
            for (float zPercent = 0; zPercent < 1.0f; zPercent += 0.05f)
            {
                // Calculate world position
                float worldX = terrainPos.x + xPercent * terrainData.size.x;
                float worldZ = terrainPos.z + zPercent * terrainData.size.z;
                
                // Sample mask directly using the percentage
                Color maskColor = trackMask.GetPixelBilinear(xPercent, zPercent);
                
                if (maskColor.r > 0.1f) // Track found
                {
                    startPoint = new Vector3(
                        worldX,
                        terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + heightAboveTerrain,
                        worldZ
                    );
                    foundStart = true;
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"Found track start at normalized position: ({xPercent}, {zPercent})");
                        Debug.Log($"World position: {startPoint}");
                        Debug.Log($"Mask color: R={maskColor.r}, G={maskColor.g}, B={maskColor.b}");
                    }
                    
                    break;
                }
            }
            if (foundStart) break;
        }
        
        if (!foundStart)
        {
            Debug.LogError("Could not find track start point! Check your track mask.");
            
            // Debug mask colors
            if (showDebugLog)
            {
                Debug.Log("Sampling mask colors at various positions:");
                for (float x = 0; x < 1.0f; x += 0.2f)
                {
                    for (float z = 0; z < 1.0f; z += 0.2f)
                    {
                        Color color = trackMask.GetPixelBilinear(x, z);
                        Debug.Log($"Mask at ({x}, {z}): R={color.r}, G={color.g}, B={color.b}");
                    }
                }
            }
            
            return;
        }
        
        // Create first waypoint
        GameObject firstWaypoint = new GameObject("Waypoint1");
        firstWaypoint.transform.parent = trackPathManager.waypointsParent;
        firstWaypoint.transform.position = startPoint;
        
        // Keep track of visited areas to prevent backtracking
        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
        
        // Mark the starting cell as visited
        int cellSize = Mathf.Max(1, Mathf.RoundToInt(trackWidth / 2f)); // Ensure cell size is at least 1
        Vector2Int startCell = new Vector2Int(
            Mathf.RoundToInt(startPoint.x / cellSize),
            Mathf.RoundToInt(startPoint.z / cellSize)
        );
        visitedCells.Add(startCell);
        
        // Find initial direction using a wider search
        Vector3 initialDirection = FindTrackDirection(startPoint, terrainPos, terrainData, Vector3.forward);
        firstWaypoint.transform.forward = initialDirection;
        
        if (showDebugLog)
        {
            Debug.Log($"Initial track direction: {initialDirection}");
        }
        
        // Track traversal state
        Vector3 currentPos = startPoint;
        Vector3 lastDirection = initialDirection;
        int waypointCount = 1;
        float totalDistanceTraveled = 0f;
        
        // Create waypoints along the track
        while (waypointCount < maxWaypoints)
        {
            // Try to find the next point on the track following the current direction
            Vector3 nextPos = FindNextTrackPoint(
                currentPos, 
                lastDirection, 
                waypointSpacing,
                terrainPos,
                terrainData,
                visitedCells,
                cellSize
            );
            
            // If no valid next point found
            if (nextPos == Vector3.zero)
            {
                Debug.LogWarning("Could not find next point on track after " + waypointCount + " waypoints.");
                break;
            }
            
            // Check if we're close to the start point (loop completed)
            if (waypointCount > 10 && Vector3.Distance(nextPos, startPoint) < waypointSpacing)
            {
                Debug.Log("Track loop completed with " + waypointCount + " waypoints!");
                break;
            }
            
            // Create next waypoint
            waypointCount++;
            GameObject waypoint = new GameObject("Waypoint" + waypointCount);
            waypoint.transform.parent = trackPathManager.waypointsParent;
            waypoint.transform.position = nextPos;
            
            // Find direction at this point
            Vector3 newDirection = FindTrackDirection(nextPos, terrainPos, terrainData, lastDirection);
            waypoint.transform.forward = newDirection;
            
            // Mark this area as visited
            Vector2Int cell = new Vector2Int(
                Mathf.RoundToInt(nextPos.x / cellSize),
                Mathf.RoundToInt(nextPos.z / cellSize)
            );
            visitedCells.Add(cell);
            
            // Update state
            currentPos = nextPos;
            lastDirection = newDirection;
            totalDistanceTraveled += waypointSpacing;
        }
        
        // Set start and finish points
        trackPathManager.startPoint = trackPathManager.waypointsParent.GetChild(0);
        trackPathManager.finishPoint = trackPathManager.waypointsParent.GetChild(0);
        trackPathManager.isLooped = true;
        
        // Refresh waypoints list
        trackPathManager.RefreshWaypointsList();
        
        Debug.Log($"Created {waypointCount} waypoints along track with total approximate length of {totalDistanceTraveled}m");
    }
    
    private Vector3 FindNextTrackPoint(
        Vector3 currentPos,
        Vector3 currentDirection, 
        float distance,
        Vector3 terrainPos,
        TerrainData terrainData,
        HashSet<Vector2Int> visitedCells,
        int cellSize)
    {
        // We'll try different angles from our current direction
        // Prioritizing directions that:
        // 1. Stay on the track
        // 2. Don't go to already visited cells
        // 3. Are close to our current direction
        
        // Try angles in this order (center first, then alternating sides)
        float[] angles = { 0, -15, 15, -30, 30, -45, 45, -60, 60, -75, 75, -90, 90 };
        
        // For each angle, we'll check if it leads to a valid point
        foreach (float angle in angles)
        {
            Vector3 directionToTry = Quaternion.Euler(0, angle, 0) * currentDirection;
            
            // We'll check multiple distances along this direction
            for (float distMult = 0.95f; distMult <= 1.05f; distMult += 0.05f)
            {
                float distanceToTry = distance * distMult;
                Vector3 pointToCheck = currentPos + directionToTry * distanceToTry;
                
                // Calculate normalized position for track mask check - FIX: proper normalization
                float normX = Mathf.InverseLerp(terrainPos.x, terrainPos.x + terrainData.size.x, pointToCheck.x);
                float normZ = Mathf.InverseLerp(terrainPos.z, terrainPos.z + terrainData.size.z, pointToCheck.z);
                
                // Check if in bounds
                if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1)
                {
                    _rejectedPoints.Add(pointToCheck); // Debug visualization
                    continue;
                }
                
                // Check if on track
                Color maskColor = trackMask.GetPixelBilinear(normX, normZ);
                bool isOnTrack = maskColor.r > 0.1f;
                
                if (!isOnTrack)
                {
                    _rejectedPoints.Add(pointToCheck); // Debug visualization
                    continue;
                }
                
                // Check if the cell has been visited
                Vector2Int cell = new Vector2Int(
                    Mathf.RoundToInt(pointToCheck.x / cellSize),
                    Mathf.RoundToInt(pointToCheck.z / cellSize)
                );
                
                // Skip if we've already been here and we're not near the beginning
                if (visitedCells.Contains(cell) && visitedCells.Count > 10)
                {
                    _rejectedPoints.Add(pointToCheck); // Debug visualization
                    continue;
                }
                
                // Adjust height to terrain
                float height = terrain.SampleHeight(pointToCheck) + heightAboveTerrain;
                Vector3 finalPoint = new Vector3(pointToCheck.x, height, pointToCheck.z);
                
                _sampledPoints.Add(finalPoint); // Debug visualization
                return finalPoint;
            }
        }
        
        // If we get here, we couldn't find a valid next point
        return Vector3.zero;
    }
    
    private Vector3 FindTrackDirection(
        Vector3 position,
        Vector3 terrainPos,
        TerrainData terrainData,
        Vector3 defaultDirection)
    {
        // We'll sample points in a circle around our current position
        // to find the direction of the track
        
        const int numSamples = 16;
        const float sampleDistance = 3f; // Sample distance for direction
        
        List<Vector3> trackPoints = new List<Vector3>();
        
        for (int i = 0; i < numSamples; i++)
        {
            float angle = i * (360f / numSamples);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 samplePoint = position + direction * sampleDistance;
            
            // Get normalized position - FIX: proper normalization
            float normX = Mathf.InverseLerp(terrainPos.x, terrainPos.x + terrainData.size.x, samplePoint.x);
            float normZ = Mathf.InverseLerp(terrainPos.z, terrainPos.z + terrainData.size.z, samplePoint.z);
            
            // Check if in bounds
            if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1)
                continue;
                
            // Check if on track
            Color maskColor = trackMask.GetPixelBilinear(normX, normZ);
            bool isOnTrack = maskColor.r > 0.1f;
            
            if (isOnTrack)
            {
                trackPoints.Add(samplePoint);
            }
        }
        
        if (trackPoints.Count < 2)
        {
            // Not enough points to determine direction
            return defaultDirection;
        }
        
        // Find the two most distant points
        Vector3 point1 = Vector3.zero;
        Vector3 point2 = Vector3.zero;
        float maxDistance = 0f;
        
        for (int i = 0; i < trackPoints.Count; i++)
        {
            for (int j = i + 1; j < trackPoints.Count; j++)
            {
                float distance = Vector3.Distance(trackPoints[i], trackPoints[j]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    point1 = trackPoints[i];
                    point2 = trackPoints[j];
                }
            }
        }
        
        // Calculate direction vector
        Vector3 directionVector = point2 - point1;
        directionVector.y = 0f;
        
        // Ensure the direction is roughly aligned with the default direction
        // to avoid going backward
        if (Vector3.Dot(directionVector.normalized, defaultDirection) < 0)
        {
            directionVector = -directionVector;
        }
        
        return directionVector.normalized;
    }
    
    void OnDrawGizmos()
    {
        if (!showConnectionGizmos) return;
        
        // Draw sampled and rejected points for debugging
        Gizmos.color = Color.green;
        foreach (Vector3 point in _sampledPoints)
        {
            Gizmos.DrawSphere(point, gizmoRadius * 0.5f);
        }
        
        Gizmos.color = Color.red;
        foreach (Vector3 point in _rejectedPoints)
        {
            Gizmos.DrawSphere(point, gizmoRadius * 0.25f);
        }
        
        // Draw track mask grid on terrain for debugging
        if (terrain != null && trackMask != null)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;
            
            // Draw a few sample points to visualize mask mapping
            Gizmos.color = Color.blue;
            int gridSize = 10;
            for (int x = 0; x <= gridSize; x++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    float normX = x / (float)gridSize;
                    float normZ = z / (float)gridSize;
                    
                    // Sample mask
                    Color maskColor = trackMask.GetPixelBilinear(normX, normZ);
                    
                    // Calculate world position
                    Vector3 worldPos = new Vector3(
                        terrainPos.x + normX * terrainData.size.x,
                        terrainPos.y,
                        terrainPos.z + normZ * terrainData.size.z
                    );
                    
                    // Adjust height to terrain
                    worldPos.y = terrain.SampleHeight(worldPos) + 0.1f;
                    
                    // Draw different sized spheres based on if point is on track
                    if (maskColor.r > 0.1f)
                    {
                        // Track point
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawSphere(worldPos, gizmoRadius * 0.75f);
                    }
                    else
                    {
                        // Not track
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(worldPos, gizmoRadius * 0.2f);
                    }
                }
            }
        }
    }
    
    [ContextMenu("Snap All Waypoints to Terrain")]
    public void SnapWaypointsToTerrain()
    {
        if (terrain == null)
        {
            terrain = FindObjectOfType<Terrain>();
            if (terrain == null)
            {
                Debug.LogError("No terrain found in scene!");
                return;
            }
        }
        
        if (trackPathManager == null || trackPathManager.waypointsParent == null)
        {
            Debug.LogError("Track path manager or waypoints parent not assigned!");
            return;
        }
        
        int count = 0;
        foreach (Transform waypoint in trackPathManager.waypointsParent)
        {
            Vector3 position = waypoint.position;
            float terrainHeight = terrain.SampleHeight(position);
            
            waypoint.position = new Vector3(
                position.x,
                terrainHeight + heightAboveTerrain,
                position.z
            );
            
            count++;
        }
        
        Debug.Log($"Snapped {count} waypoints to terrain");
    }
    
    [ContextMenu("Adjust Waypoint Rotations")]
    public void AdjustWaypointRotations()
    {
        if (trackPathManager == null || trackPathManager.waypointsParent == null)
        {
            Debug.LogError("Track path manager or waypoints parent not assigned!");
            return;
        }
        
        // Force refresh waypoints list
        trackPathManager.RefreshWaypointsList();
        
        int count = 0;
        foreach (Transform waypoint in trackPathManager.waypointsParent)
        {
            // Get the next waypoint to point toward
            Transform nextWaypoint = trackPathManager.GetNextWaypoint(waypoint);
            
            if (nextWaypoint != waypoint) // Skip if there's only one waypoint
            {
                Vector3 direction = nextWaypoint.position - waypoint.position;
                direction.y = 0; // Keep rotation on horizontal plane
                
                if (direction != Vector3.zero)
                {
                    waypoint.forward = direction.normalized;
                    count++;
                }
            }
        }
        
        Debug.Log($"Adjusted rotation of {count} waypoints");
    }
    
    [ContextMenu("Adjust Waypoint Rotations (Strict Order)")]
    public void AdjustWaypointRotationsStrictOrder()
    {
        if (trackPathManager == null || trackPathManager.waypointsParent == null)
        {
            Debug.LogError("Track path manager or waypoints parent not assigned!");
            return;
        }

        // Collect all waypoints in order (by hierarchy)
        List<Transform> waypoints = new List<Transform>();
        foreach (Transform child in trackPathManager.waypointsParent)
        {
            waypoints.Add(child);
        }

        int count = 0;
        int n = waypoints.Count;
        if (n < 2)
        {
            Debug.LogWarning("Not enough waypoints to adjust rotations.");
            return;
        }

        for (int i = 0; i < n; i++)
        {
            Transform current = waypoints[i];
            Transform next = waypoints[(i + 1) % n]; // Wraps around to first

            Vector3 direction = next.position - current.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                current.forward = direction.normalized;
                count++;
            }
        }

        Debug.Log($"Adjusted rotation of {count} waypoints in strict order, loop closed.");
    }
    
    [ContextMenu("Visualize Track Mask on Terrain")]
    public void VisualizeTrackMask()
    {
        if (terrain == null || trackMask == null)
        {
            Debug.LogError("Terrain or track mask not assigned!");
            return;
        }
        
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        
        // Create visual markers to show track path on terrain
        GameObject visualizerParent = new GameObject("TrackMaskVisualizer");
        visualizerParent.transform.position = Vector3.zero;
        
        int sampleCount = 50; // Number of samples in each direction
        
        for (int x = 0; x < sampleCount; x++)
        {
            for (int z = 0; z < sampleCount; z++)
            {
                // Normalized coordinates
                float normX = x / (float)(sampleCount - 1);
                float normZ = z / (float)(sampleCount - 1);
                
                // Sample mask
                Color maskColor = trackMask.GetPixelBilinear(normX, normZ);
                
                // Only visualize track points
                if (maskColor.r > 0.1f)
                {
                    // Calculate world position
                    Vector3 worldPos = new Vector3(
                        terrainPos.x + normX * terrainData.size.x,
                        0,
                        terrainPos.z + normZ * terrainData.size.z
                    );
                    
                    // Adjust height to terrain
                    worldPos.y = terrain.SampleHeight(worldPos) + 0.1f;
                    
                    // Create visual marker
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.transform.position = worldPos;
                    marker.transform.localScale = Vector3.one * 0.5f;
                    marker.transform.parent = visualizerParent.transform;
                    
                    // Set color based on mask value
                    Renderer renderer = marker.GetComponent<Renderer>();
                    renderer.material.color = new Color(1, 0, 0, 0.5f);
                }
            }
        }
        
        Debug.Log("Created track mask visualizer. Delete the TrackMaskVisualizer object when you're done.");
    }
    
    [ContextMenu("Add Waypoint Between Selected")]
    public void AddWaypointBetween()
    {
#if UNITY_EDITOR
        if (trackPathManager == null || trackPathManager.waypointsParent == null)
        {
            Debug.LogError("Track path manager or waypoints parent not assigned!");
            return;
        }
        
        // Get currently selected waypoint in scene
        Transform selectedWaypoint = Selection.activeTransform;
        
        if (selectedWaypoint == null || selectedWaypoint.parent != trackPathManager.waypointsParent)
        {
            Debug.LogError("Please select a waypoint first!");
            return;
        }
        
        // Find the next waypoint
        trackPathManager.RefreshWaypointsList();
        Transform nextWaypoint = trackPathManager.GetNextWaypoint(selectedWaypoint);
        
        if (nextWaypoint == selectedWaypoint)
        {
            Debug.LogError("Cannot find next waypoint!");
            return;
        }
        
        // Create new waypoint between selected and next
        GameObject newWaypoint = new GameObject($"Waypoint{trackPathManager.waypointsParent.childCount + 1}");
        newWaypoint.transform.parent = trackPathManager.waypointsParent;
        
        // Position halfway between
        newWaypoint.transform.position = Vector3.Lerp(selectedWaypoint.position, nextWaypoint.position, 0.5f);
        
        // Set rotation to look toward next waypoint
        Vector3 direction = nextWaypoint.position - newWaypoint.transform.position;
        if (direction != Vector3.zero)
        {
            direction.y = 0;
            newWaypoint.transform.forward = direction.normalized;
        }
        
        // Snap to terrain if needed
        if (terrain != null)
        {
            float terrainHeight = terrain.SampleHeight(newWaypoint.transform.position);
            newWaypoint.transform.position = new Vector3(
                newWaypoint.transform.position.x,
                terrainHeight + heightAboveTerrain,
                newWaypoint.transform.position.z
            );
        }
        
        // Refresh the list
        trackPathManager.RefreshWaypointsList();
        
        // Select the new waypoint
        Selection.activeGameObject = newWaypoint;
        
        Debug.Log("Created new waypoint between selected points");
#else
        Debug.LogWarning("This function only works in the Unity Editor");
#endif
    }
    
    [ContextMenu("Rename Waypoints In Order")]
    public void RenameWaypointsInOrder()
    {
        if (trackPathManager == null || trackPathManager.waypointsParent == null)
        {
            Debug.LogError("Track path manager or waypoints parent not assigned!");
            return;
        }

        // Collect all waypoints
        List<Transform> waypoints = new List<Transform>();
        foreach (Transform child in trackPathManager.waypointsParent)
        {
            waypoints.Add(child);
        }

        // Sort by position along the path (using their current order in hierarchy)
        // If you want to sort by distance from the first, you could do that instead.
        // Here, we just use the order in the hierarchy.
        int total = waypoints.Count;
        for (int i = 0; i < total; i++)
        {
            waypoints[i].name = $"Waypoint{(i + 1).ToString("D3")}";
        }

        Debug.Log($"Renamed {waypoints.Count} waypoints in order with 3-digit zero padding.");
    }

    [ContextMenu("Sort Waypoints In Hierarchy By Name")]
    public void SortWaypointsInHierarchyByName()
    {
#if UNITY_EDITOR
        if (trackPathManager == null || trackPathManager.waypointsParent == null)
        {
            Debug.LogError("Track path manager or waypoints parent not assigned!");
            return;
        }

        // Collect all waypoints and sort by name
        List<Transform> waypoints = new List<Transform>();
        foreach (Transform child in trackPathManager.waypointsParent)
            waypoints.Add(child);

        waypoints.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        // Reorder in hierarchy
        for (int i = 0; i < waypoints.Count; i++)
            waypoints[i].SetSiblingIndex(i);

        Debug.Log("Waypoints sorted in hierarchy by name.");
#else
        Debug.LogWarning("This function only works in the Unity Editor.");
#endif
    }

    
}