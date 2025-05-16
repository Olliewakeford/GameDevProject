using UnityEngine;

public class StartFinishLine : MonoBehaviour
{
    [Header("References")]
    public TrackPathManager trackPathManager;
    public Transform startPoint;
    public Transform finishPoint;
    
    [Header("Visual Settings")]
    public float width = 10f;
    public float height = 3f;
    public Material startMaterial;
    public Material finishMaterial;
    
    private GameObject _startLineObject;
    private GameObject _finishLineObject;
    
    void Start()
    {
        // Find track path manager if not assigned
        if (trackPathManager == null)
        {
            trackPathManager = FindObjectOfType<TrackPathManager>();
        }
        
        // Get start/finish points from track path if available
        if (trackPathManager != null)
        {
            if (startPoint == null && trackPathManager.startPoint != null)
            {
                startPoint = trackPathManager.startPoint;
            }
            
            if (finishPoint == null && trackPathManager.finishPoint != null)
            {
                finishPoint = trackPathManager.finishPoint;
            }
        }
        
        // Create visual representations
        CreateStartLine();
        CreateFinishLine();
    }
    
    void CreateStartLine()
    {
        if (startPoint == null) return;
        
        // Create visual representation
        _startLineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _startLineObject.name = "StartLine";
        _startLineObject.transform.parent = transform;
        
        // Calculate position perpendicular to track direction
        _startLineObject.transform.position = startPoint.position;
        _startLineObject.transform.rotation = startPoint.rotation;
        // _startLineObject.transform.Rotate(0, 90, 0); // Rotate to be perpendicular to direction
        
        // Set scale
        _startLineObject.transform.localScale = new Vector3(width, height, 0.1f);
        
        // Set material
        if (startMaterial != null)
        {
            _startLineObject.GetComponent<Renderer>().material = startMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            _startLineObject.GetComponent<Renderer>().material = mat;
        }
        
        // Configure as trigger
        _startLineObject.GetComponent<BoxCollider>().isTrigger = true;
        
        // Add trigger component
        StartFinishTrigger trigger = _startLineObject.AddComponent<StartFinishTrigger>();
        trigger.isStartLine = true;
        trigger.isFinishLine = (startPoint == finishPoint); // If start/finish are the same
    }
    
    void CreateFinishLine()
    {
        if (finishPoint == null || finishPoint == startPoint) return; // Skip if same as start
        
        // Create visual representation
        _finishLineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _finishLineObject.name = "FinishLine";
        _finishLineObject.transform.parent = transform;
        
        // Calculate position perpendicular to track direction
        _finishLineObject.transform.position = finishPoint.position;
        _finishLineObject.transform.rotation = finishPoint.rotation;
        // _finishLineObject.transform.Rotate(0, 0, 0); // Rotate to be perpendicular to direction
        
        // Set scale
        _finishLineObject.transform.localScale = new Vector3(width, height, 0.1f);
        
        // Set material
        if (finishMaterial != null)
        {
            _finishLineObject.GetComponent<Renderer>().material = finishMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            _finishLineObject.GetComponent<Renderer>().material = mat;
        }
        
        // Configure as trigger
        _finishLineObject.GetComponent<BoxCollider>().isTrigger = true;
        
        // Add trigger component
        StartFinishTrigger trigger = _finishLineObject.AddComponent<StartFinishTrigger>();
        trigger.isStartLine = false;
        trigger.isFinishLine = true;
    }
}

// Trigger component for detecting line crossing
public class StartFinishTrigger : MonoBehaviour
{
    public bool isStartLine = false;
    public bool isFinishLine = false;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isStartLine)
            {
                Debug.Log("Player crossed start line!");
                CheckpointManager.Instance.ResetCheckpoints();
                LapTimer.Instance.StartTimer();
                FindObjectOfType<LapMessageUI>().ShowMessage("Lap Started!");
            }
            
            if (isFinishLine)
            {
                Debug.Log("Player crossed finish line!");
                if (CheckpointManager.Instance.AllCheckpointsPassed())
                {
                    Debug.Log("Lap completed!");
                    LapTimer.Instance.StopTimer();
                    FindObjectOfType<LapMessageUI>().ShowMessage("Lap Finished!\nTime: " +
                        FindObjectOfType<LapTimerUI>().FormatTime(LapTimer.Instance.GetElapsedTime()), 10f); // Show for 10 seconds
                }
                else
                {
                    Debug.Log("You must pass all checkpoints before finishing!");
                    FindObjectOfType<LapMessageUI>().ShowMessage("Pass all checkpoints first!");
                }
            }
        }
    }
}