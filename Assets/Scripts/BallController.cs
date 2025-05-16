using UnityEngine;
using System.Collections.Generic;

public class BallController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce = 15f;
    public float maxSpeed = 20f;
    public float brakingForce = 5f;
    public float dragOnTrack = 0.5f;
    public float dragOffTrack = 5f;
    public float airDrag = 0.02f; // Reduced from 0.1f for less slowdown in air
    
    [Header("Gravity & Air Control")]
    public float additionalGravity = 15f; // Additional gravity force
    public float airControlMultiplier = 0.7f; // How much control in air (0-1)
    public float groundedVelocityRetention = 0.95f; // How much velocity to retain when landing
    
    [Header("References")]
    public Terrain terrain;
    public Texture2D trackMask;
    public Camera mainCamera;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public Color onTrackColor = Color.green;
    public Color offTrackColor = Color.red;
    public Color inAirColor = Color.blue;
    
    private Rigidbody _rb;
    private Transform _transform;
    private bool _isOnTrack = false;
    private bool _isGrounded = false;
    private bool _wasGroundedLastFrame = false;
    private Vector3 _lastFramePosition;
    private float _currentSpeed;
    private Material _originalMaterial;
    private Renderer _renderer;
    private Vector3 _velocityBeforeAirborne;
    
    public TrackGridData trackGridData;
    
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _transform = transform;
        _renderer = GetComponent<Renderer>();
        
        if (_renderer != null && showDebugInfo)
        {
            _originalMaterial = _renderer.material;
        }
        
        // Find references if not set
        if (terrain == null)
            terrain = FindObjectOfType<Terrain>();
            
        if (trackMask == null)
        {
            // Try to load from Resources folder
            trackMask = Resources.Load<Texture2D>("TrackMask");
            
            if (trackMask == null)
                Debug.LogWarning("Track mask not found! Please assign it in the inspector.");
        }
        
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        _lastFramePosition = transform.position;
    }
    
    void Update()
    {
        // Calculate current speed (for UI or effects)
        _currentSpeed = Vector3.Distance(_lastFramePosition, transform.position) / Time.deltaTime;
        _lastFramePosition = transform.position;
        
        // Update visual feedback if enabled
        if (showDebugInfo && _renderer != null)
        {
            if (!_isGrounded)
                _renderer.material.color = inAirColor;
            else if (_isOnTrack)
                _renderer.material.color = onTrackColor;
            else
                _renderer.material.color = offTrackColor;
        }
    }
    
    void FixedUpdate()
    {
        // Store previous grounded state for landing detection
        _wasGroundedLastFrame = _isGrounded;
        
        // Check ground contact and track status
        CheckGroundContact();
        CheckIfOnTrack();
        
        // Apply movement forces
        ApplyCameraRelativeMovementForce();
        
        // Apply additional gravity when in air
        if (!_isGrounded)
        {
            _rb.AddForce(Vector3.down * additionalGravity, ForceMode.Acceleration);
        }
        else if (!_wasGroundedLastFrame)
        {
            // Just landed - handle landing physics
            HandleLanding();
        }
        
        // Keep track of velocity for when we go airborne
        if (_isGrounded)
        {
            _velocityBeforeAirborne = _rb.velocity;
        }
        
        // Limit max speed
        LimitMaxSpeed();
        
        // Apply appropriate drag
        ApplyDrag();
    }
    
    void CheckGroundContact()
    {
        // Use a slightly more generous raycast for ground detection
        float checkDistance = GetComponent<Collider>().bounds.extents.y + 0.2f;
        
        _isGrounded = Physics.Raycast(
            _transform.position, Vector3.down, 
            checkDistance);
            
        // Debug visualization
        if (showDebugInfo)
        {
            Debug.DrawRay(_transform.position, Vector3.down * checkDistance, 
                _isGrounded ? Color.green : Color.red);
        }
    }
    
    void CheckIfOnTrack()
    {
        if (!_isGrounded || trackGridData == null || terrain == null) {
            _isOnTrack = false;
            return;
        }

        Vector3 terrainLocalPos = _transform.position - terrain.transform.position;
        TerrainData terrainData = terrain.terrainData;

        // Convert to normalized position (0-1)
        float normX = terrainLocalPos.x / terrainData.size.x;
        float normZ = terrainLocalPos.z / terrainData.size.z;

        // Convert to grid coordinates
        int gridX = Mathf.RoundToInt(normX * (trackGridData.resolution - 1));
        int gridZ = Mathf.RoundToInt(normZ * (trackGridData.resolution - 1));

        _isOnTrack = trackGridData.IsOnTrack(gridX, gridZ);
    }
    
    void ApplyCameraRelativeMovementForce()
    {
        // Get input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        // Skip if no input
        if (Mathf.Abs(horizontalInput) < 0.1f && Mathf.Abs(verticalInput) < 0.1f)
        {
            // Apply braking when no input but still moving
            if (_rb.velocity.magnitude > 0.5f && _isGrounded)
            {
                _rb.AddForce(-_rb.velocity.normalized * brakingForce, ForceMode.Acceleration);
            }
            return;
        }
        
        // Get camera forward and right vectors (projected onto horizontal plane)
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        cameraForward.y = 0;
        cameraRight.y = 0;
        
        // Normalize to ensure consistent force magnitudes
        if (cameraForward.magnitude > 0.1f) cameraForward.Normalize();
        if (cameraRight.magnitude > 0.1f) cameraRight.Normalize();
        
        // Calculate desired movement direction in camera space
        Vector3 moveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
        
        // Apply force - more force when changing direction
        float forceMagnitude = moveForce;
        
        // If trying to go against current velocity, apply more force to help change direction
        if (_rb.velocity.magnitude > 1f && Vector3.Dot(_rb.velocity.normalized, moveDirection) < 0)
        {
            forceMagnitude *= 1.5f;
        }
        
        // Reduce control in air if set
        if (!_isGrounded)
        {
            forceMagnitude *= airControlMultiplier;
        }
        
        _rb.AddForce(moveDirection * forceMagnitude, ForceMode.Acceleration);
        
        // Debug visualization
        if (showDebugInfo)
        {
            Debug.DrawRay(transform.position, moveDirection * 2f, Color.green);
            Debug.DrawRay(transform.position, _rb.velocity * 0.5f, Color.yellow);
        }
    }
    
    void HandleLanding()
    {
        // Preserve some of the horizontal velocity when landing
        Vector3 horizontalVelocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        
        // If landing velocity is very small, try to recover some of the velocity from before we were airborne
        if (horizontalVelocity.magnitude < 2f && _velocityBeforeAirborne.magnitude > 2f)
        {
            Vector3 recoveredVelocity = new Vector3(
                _velocityBeforeAirborne.x * groundedVelocityRetention,
                _rb.velocity.y,
                _velocityBeforeAirborne.z * groundedVelocityRetention
            );
            
            _rb.velocity = recoveredVelocity;
        }
    }
    
    void LimitMaxSpeed()
    {
        // Only limit XZ speed (horizontal)
        Vector3 horizontalVelocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            _rb.velocity = new Vector3(
                horizontalVelocity.x, 
                _rb.velocity.y, 
                horizontalVelocity.z);
        }
    }
    
    void ApplyDrag()
    {
        if (!_isGrounded)
        {
            _rb.drag = airDrag;
        }
        else if (_isOnTrack)
        {
            _rb.drag = dragOnTrack;
        }
        else
        {
            _rb.drag = dragOffTrack;
        }
    }
    
    void OnDestroy()
    {
        // Restore original material if we changed it
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.material = _originalMaterial;
        }
    }
    
    // Public getters
    public float GetCurrentSpeed()
    {
        return _currentSpeed;
    }
    
    public bool IsOnTrack()
    {
        return _isOnTrack;
    }
    
    public bool IsGrounded()
    {
        return _isGrounded;
    }
}