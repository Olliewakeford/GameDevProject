using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform player;
    public Transform startPoint;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RestartGame()
    {
        // Reset player position and velocity
        player.position = startPoint.position;
        player.rotation = startPoint.rotation;
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset timer and checkpoints
        LapTimer.Instance.ResetTimer();
        CheckpointManager.Instance.ResetCheckpoints();

        // Reset UI as needed
        FindObjectOfType<LapMessageUI>().ShowMessage("Lap Restarted!");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
