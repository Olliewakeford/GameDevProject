using System.Collections.Generic;
using UnityEngine;

public class TrackPathManager : MonoBehaviour
{
    public Transform waypointsParent; // Parent object containing all waypoints
    public bool isLooped = true; // Whether the track is a closed loop
    public bool visualizeInEditor = true;
    public Color pathColor = Color.blue;
    public Color waypointColor = Color.yellow;
    public float waypointRadius = 0.5f;
    public float pathHeight = 0.2f; // Height above terrain to draw path
    
    [Header("Start/Finish")]
    public Transform startPoint; // Starting position
    public Transform finishPoint; // Finish position (can be same as start for a loop)
    public float checkpointWidth = 10f; // Width of the start/finish line
    public Color startLineColor = Color.green;
    public Color finishLineColor = Color.red;
    
    private List<Transform> _waypoints = new List<Transform>();
    private int _waypointCount;
    private Terrain _terrain;
    
    void Awake()
    {
        _terrain = FindObjectOfType<Terrain>();
        RefreshWaypointsList();
    }
    
    void Start()
    {
        if (startPoint == null && _waypointCount > 0)
        {
            startPoint = _waypoints[0];
            Debug.Log("Using first waypoint as start point");
        }
        
        if (finishPoint == null && isLooped && _waypointCount > 0)
        {
            finishPoint = _waypoints[0];
            Debug.Log("Using first waypoint as finish point for looped track");
        }
        else if (finishPoint == null && !isLooped && _waypointCount > 0)
        {
            finishPoint = _waypoints[_waypointCount - 1];
            Debug.Log("Using last waypoint as finish point");
        }
    }
    
    public void RefreshWaypointsList()
    {
        if (waypointsParent == null) return;
        
        _waypoints.Clear();
        
        // Get all children of the waypoints parent
        foreach (Transform child in waypointsParent)
        {
            _waypoints.Add(child);
        }
        
        _waypointCount = _waypoints.Count;
        
        // Sort waypoints by name if they are named numerically (e.g., "Waypoint1", "Waypoint2", etc.)
        _waypoints.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        
        Debug.Log($"Track path initialized with {_waypointCount} waypoints");
    }
    
    // Find the closest waypoint to a given position
    public Transform GetClosestWaypoint(Vector3 position)
    {
        if (_waypointCount == 0) return null;
        
        Transform closest = _waypoints[0];
        float minDistance = Vector3.Distance(position, closest.position);
        
        for (int i = 1; i < _waypointCount; i++)
        {
            float distance = Vector3.Distance(position, _waypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = _waypoints[i];
            }
        }
        
        return closest;
    }
    public int minWaypointsForLoop = 150;
     
    // Find the next waypoint after the current one
    public Transform GetNextWaypoint(Transform currentWaypoint)
    {
        if (_waypointCount <= 1) return currentWaypoint;

        int index = _waypoints.IndexOf(currentWaypoint);

        if (index == -1) // Current waypoint not found
        {
            return GetClosestWaypoint(currentWaypoint.position);
        }

        // Get next waypoint
        int nextIndex = index + 1;

        // If we've reached the end of the list
        if (nextIndex >= _waypointCount)
        {
            // Only loop if isLooped is true AND we have enough waypoints
            if (isLooped && _waypointCount >= minWaypointsForLoop)
            {
                return _waypoints[0]; // Return first waypoint to create loop
            }
            else
            {
                return _waypoints[index]; // Return current waypoint (no advancement)
            }
        }

        return _waypoints[nextIndex];
    }

    // Find the previous waypoint before the current one
    public Transform GetPreviousWaypoint(Transform currentWaypoint)
    {
        if (_waypointCount <= 1) return currentWaypoint;
        
        int index = _waypoints.IndexOf(currentWaypoint);
        
        if (index == -1) // Current waypoint not found
        {
            return GetClosestWaypoint(currentWaypoint.position);
        }
        
        // Get previous waypoint
        int prevIndex = (index - 1 + _waypointCount) % _waypointCount;
        
        // If not looped and we're at the first waypoint, return the first waypoint
        if (!isLooped && index == 0)
        {
            return _waypoints[0];
        }
        
        return _waypoints[prevIndex];
    }
    
    // Get track direction at a specific position
    public Vector3 GetTrackDirectionAt(Vector3 position)
    {
        if (_waypointCount <= 1) return Vector3.forward;
        
        Transform closestWaypoint = GetClosestWaypoint(position);
        Transform nextWaypoint = GetNextWaypoint(closestWaypoint);
        
        if (closestWaypoint == nextWaypoint) return Vector3.forward;
        
        Vector3 direction = (nextWaypoint.position - closestWaypoint.position).normalized;
        direction.y = 0; // Keep direction horizontal
        return direction.normalized;
    }
    
    // Get waypoint position for camera, which leads the target
    public Vector3 GetCameraTrackPosition(Vector3 ballPosition, float leadDistance)
    {
        if (_waypointCount <= 1) return ballPosition;
        
        // Find the closest waypoint
        Transform closestWaypoint = GetClosestWaypoint(ballPosition);
        int closestIndex = _waypoints.IndexOf(closestWaypoint);
        
        // Find the next waypoint
        Transform nextWaypoint = GetNextWaypoint(closestWaypoint);
        
        // Calculate vector from ball to next waypoint
        Vector3 toNextWaypoint = nextWaypoint.position - ballPosition;
        toNextWaypoint.y = 0; // Keep flat for position calculation
        
        // If we're closer to the next waypoint than our lead distance, 
        // we need to look at the waypoint after that
        if (toNextWaypoint.magnitude < leadDistance)
        {
            Transform wayAfterNext = GetNextWaypoint(nextWaypoint);
            
            // Blend between next and the one after that
            float blendFactor = 1f - (toNextWaypoint.magnitude / leadDistance);
            Vector3 direction = Vector3.Lerp(
                (nextWaypoint.position - closestWaypoint.position).normalized,
                (wayAfterNext.position - nextWaypoint.position).normalized,
                blendFactor
            );
            
            // Apply lead distance to get final position
            return ballPosition + direction * leadDistance;
        }
        
        // Otherwise use vector to next waypoint, normalized to lead distance
        return ballPosition + toNextWaypoint.normalized * leadDistance;
    }
    
    // Calculate camera look-at position based on track
    public Vector3 GetCameraLookAtPosition(Vector3 ballPosition, float lookAheadDistance)
    {
        Vector3 trackDirection = GetTrackDirectionAt(ballPosition);
        return ballPosition + trackDirection * lookAheadDistance;
    }
    
    // Check if a position is near the start line
    public bool IsNearStartLine(Vector3 position, float threshold = 3f)
    {
        if (startPoint == null) return false;
        return Vector3.Distance(position, startPoint.position) < threshold;
    }
    
    // Check if a position is near the finish line
    public bool IsNearFinishLine(Vector3 position, float threshold = 3f)
    {
        if (finishPoint == null) return false;
        return Vector3.Distance(position, finishPoint.position) < threshold;
    }
    
    void OnDrawGizmos()
    {
        if (!visualizeInEditor) return;
        
        if (waypointsParent != null && waypointsParent.childCount > 0)
        {
            // Draw path between waypoints
            Gizmos.color = pathColor;
            
            // Temporary list to draw the path
            List<Transform> points = new List<Transform>();
            foreach (Transform child in waypointsParent)
            {
                points.Add(child);
            }
            
            // Sort waypoints by name if they are named numerically (e.g., "Waypoint1", "Waypoint2", etc.)
            points.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 pointA = points[i].position;
                Vector3 pointB = points[i+1].position;
                
                // Adjust height if terrain exists
                if (_terrain != null)
                {
                    pointA.y = _terrain.SampleHeight(pointA) + pathHeight;
                    pointB.y = _terrain.SampleHeight(pointB) + pathHeight;
                }
                
                Gizmos.DrawLine(pointA, pointB);
            }
            
            // Close the loop if needed
            if (isLooped && points.Count > 1)
            {
                Vector3 firstPoint = points[0].position;
                Vector3 lastPoint = points[points.Count - 1].position;
                
                // Adjust height if terrain exists
                if (_terrain != null)
                {
                    firstPoint.y = _terrain.SampleHeight(firstPoint) + pathHeight;
                    lastPoint.y = _terrain.SampleHeight(lastPoint) + pathHeight;
                }
                
                Gizmos.DrawLine(lastPoint, firstPoint);
            }
            
            // Draw waypoint spheres
            Gizmos.color = waypointColor;
            foreach (Transform waypoint in points)
            {
                Vector3 position = waypoint.position;
                
                // Adjust height if terrain exists
                if (_terrain != null)
                {
                    position.y = _terrain.SampleHeight(position) + pathHeight;
                }
                
                Gizmos.DrawSphere(position, waypointRadius);
            }
        }
        
        // Draw start line
        if (startPoint != null)
        {
            Gizmos.color = startLineColor;
            Vector3 startForward = startPoint.forward;
            Vector3 startRight = startPoint.right;
            
            Vector3 startLeft = startPoint.position - startRight * (checkpointWidth / 2);
            Vector3 startRight1 = startPoint.position + startRight * (checkpointWidth / 2);
            
            // Adjust height if terrain exists
            if (_terrain != null)
            {
                startLeft.y = _terrain.SampleHeight(startLeft) + pathHeight;
                startRight1.y = _terrain.SampleHeight(startRight1) + pathHeight;
            }
            
            Gizmos.DrawLine(startLeft, startRight1);
        }
        
        // Draw finish line
        if (finishPoint != null && finishPoint != startPoint)
        {
            Gizmos.color = finishLineColor;
            Vector3 finishForward = finishPoint.forward;
            Vector3 finishRight = finishPoint.right;
            
            Vector3 finishLeft = finishPoint.position - finishRight * (checkpointWidth / 2);
            Vector3 finishRight1 = finishPoint.position + finishRight * (checkpointWidth / 2);
            
            // Adjust height if terrain exists
            if (_terrain != null)
            {
                finishLeft.y = _terrain.SampleHeight(finishLeft) + pathHeight;
                finishRight1.y = _terrain.SampleHeight(finishRight1) + pathHeight;
            }
            
            Gizmos.DrawLine(finishLeft, finishRight1);
        }
    }
    
    // Helper method to create waypoints
    [ContextMenu("Create Path Waypoints")]
    public void CreateWaypoints()
    {
        // Create parent if it doesn't exist
        if (waypointsParent == null)
        {
            GameObject parent = new GameObject("TrackWaypoints");
            waypointsParent = parent.transform;
            waypointsParent.parent = transform;
        }
        
        // Create 8 waypoints in a circle as a default
        const int numWaypoints = 8;
        const float radius = 20f;
        
        for (int i = 0; i < numWaypoints; i++)
        {
            float angle = i * (360f / numWaypoints) * Mathf.Deg2Rad;
            
            // Calculate position
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            
            Vector3 position = transform.position + new Vector3(x, 0, z);
            
            // Adjust height to terrain
            if (_terrain != null)
            {
                position.y = _terrain.SampleHeight(position) + 0.5f; // Slightly above terrain
            }
            
            // Create waypoint
            GameObject waypoint = new GameObject($"Waypoint{i+1}");
            waypoint.transform.position = position;
            waypoint.transform.parent = waypointsParent;
            
            // Point in direction of next waypoint
            float nextAngle = (i + 1) % numWaypoints * (360f / numWaypoints) * Mathf.Deg2Rad;
            float nextX = Mathf.Sin(nextAngle) * radius;
            float nextZ = Mathf.Cos(nextAngle) * radius;
            
            Vector3 nextPosition = transform.position + new Vector3(nextX, 0, nextZ);
            Vector3 direction = nextPosition - position;
            
            waypoint.transform.forward = direction.normalized;
        }
        
        // Automatically set the first waypoint as start and finish
        startPoint = waypointsParent.GetChild(0);
        finishPoint = waypointsParent.GetChild(0);
        
        // Refresh the waypoints list
        RefreshWaypointsList();
        
        Debug.Log($"Created {numWaypoints} waypoints in a circle");
    }
}