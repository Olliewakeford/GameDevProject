using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public int totalCheckpoints = 0; // Set this to the number of checkpoints in your track
    private int nextCheckpointIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayerPassedCheckpoint(int checkpointIndex)
    {
        LapMessageUI.DebugToCanvas($"Trying to pass checkpoint {checkpointIndex}, next expected: {nextCheckpointIndex} (total: {totalCheckpoints})");
        if (checkpointIndex == nextCheckpointIndex)
        {
            Debug.Log("Checkpoint " + checkpointIndex + " passed!");
            nextCheckpointIndex++;
            LapMessageUI.DebugToCanvas($"Checkpoint {checkpointIndex} passed!");
            if (nextCheckpointIndex >= totalCheckpoints)
            {
                Debug.Log("All checkpoints passed! You can finish the lap.");
                LapMessageUI.DebugToCanvas("All checkpoints passed! You can finish the lap.");
            }
        }
        else
        {
            Debug.Log("Wrong checkpoint! You must pass them in order.");
            LapMessageUI.DebugToCanvas($"Wrong checkpoint! Hit {checkpointIndex}, expected {nextCheckpointIndex}");
        }
    }

    public void ResetCheckpoints()
    {
        nextCheckpointIndex = 0;
    }

    public bool AllCheckpointsPassed()
    {
        return nextCheckpointIndex >= totalCheckpoints;
    }
}