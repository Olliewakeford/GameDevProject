using UnityEngine;

public class TrackFollowingCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float distance = 5.0f;
    public float height = 2.0f;
    public float lookAheadDistance = 2.0f;
    
    [Header("Follow Settings")]
    public float positionSmoothTime = 0.1f;
    public float rotationSmoothTime = 0.05f;
    
    [Header("Track Path Settings")]
    public TrackPathManager trackPath;
    public float trackInfluence = 0.8f; // How much the track direction influences camera
    public float waypointLookAheadDistance = 10f; // How far ahead to look on the track path
    
    private Vector3 _positionVelocity = Vector3.zero;
    private Rigidbody _targetRigidbody;
    private BallController _ballController;
    
    void Start()
    {
        // Find target if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                _targetRigidbody = player.GetComponent<Rigidbody>();
                _ballController = player.GetComponent<BallController>();
                Debug.Log("Camera found player target automatically");
            }
            else
            {
                Debug.LogError("No target assigned to camera and no Player tag found");
            }
        }
        else
        {
            _targetRigidbody = target.GetComponent<Rigidbody>();
            _ballController = target.GetComponent<BallController>();
        }
        
        // Find track path if not assigned
        if (trackPath == null)
        {
            trackPath = FindObjectOfType<TrackPathManager>();
            if (trackPath == null)
            {
                Debug.LogWarning("No TrackPathManager found. Camera will use fallback behavior.");
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        if (trackPath != null)
        {
            ApplyTrackBasedCamera();
        }
        else
        {
            ApplyFallbackCamera();
        }
    }
    
    void ApplyTrackBasedCamera()
    {
        // Get the track direction at the player's position
        Vector3 trackDirection = trackPath.GetTrackDirectionAt(target.position);
    
        // IMPORTANT: REVERSE the direction to place camera BEHIND the ball
        Vector3 cameraDirection = -trackDirection; // This is the key fix
    
        // Get position behind the ball based on track
        Vector3 targetPosition = target.position + cameraDirection * distance;
        targetPosition.y = target.position.y + height;
    
        // Get the look-at position (ahead on the track)
        Vector3 lookAtPosition = target.position + trackDirection * lookAheadDistance;
    
        // Apply velocity influence if available
        if (_targetRigidbody != null && _targetRigidbody.velocity.magnitude > 0.5f)
        {
            // Get the velocity-based position (behind the ball)
            Vector3 velocityDir = -_targetRigidbody.velocity.normalized;
            velocityDir.y = 0;
            velocityDir.Normalize();
        
            Vector3 velocityBasedPosition = target.position + velocityDir * distance;
            velocityBasedPosition.y = target.position.y + height;
        
            // Blend between track-based and velocity-based positions
            targetPosition = Vector3.Lerp(velocityBasedPosition, targetPosition, trackInfluence);
        }
    
        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref _positionVelocity, 
            positionSmoothTime);
    
        // Calculate and apply rotation
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSmoothTime * 30f * Time.deltaTime));
    }
    
    void ApplyFallbackCamera()
    {
        // Fallback method when no track path is available
        
        // Get direction based on velocity or forward direction
        Vector3 followDirection;
        
        if (_targetRigidbody != null && _targetRigidbody.velocity.magnitude > 0.5f)
        {
            followDirection = -_targetRigidbody.velocity.normalized;
            followDirection.y = 0;
            followDirection.Normalize();
        }
        else
        {
            followDirection = -target.forward;
            followDirection.y = 0;
            followDirection.Normalize();
        }
        
        // Calculate position
        Vector3 targetPosition = target.position + followDirection * distance;
        targetPosition.y = target.position.y + height;
        
        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref _positionVelocity, 
            positionSmoothTime);
        
        // Look at target with slight lead
        Vector3 lookAtPosition = target.position + target.forward * lookAheadDistance;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSmoothTime * 30f * Time.deltaTime));
    }
}