using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public Button restartButton;
    public Button exitButton;
    public Transform playerBall; // Assign your player ball here
    public Vector3 startPosition; // Set this to your starting position
    public Vector3 startVelocity = Vector3.zero; // Optional: set to zero to stop the ball
    public LapTimer lapTimer; // assign in Inspector
    public CheckpointManager checkpointManager; // assign in Inspector

    // Start is called before the first frame update
    void Start()
    {
        restartButton.onClick.AddListener(RestartAtStart);
        exitButton.onClick.AddListener(ExitToHome);
    }

    void RestartAtStart()
    {
        // Move ball and reset velocity
        playerBall.position = startPosition;
        Rigidbody rb = playerBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = startVelocity;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset timer and checkpoints
        if (lapTimer != null) lapTimer.ResetTimer();
        if (checkpointManager != null) checkpointManager.ResetCheckpoints();
    }

    void ExitToHome()
    {
        SceneManager.LoadScene("HomePage");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
