using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex; // Set this in the Inspector or via script

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LapMessageUI.DebugToCanvas($"Checkpoint {checkpointIndex} triggered by {other.name}");
            CheckpointManager.Instance.PlayerPassedCheckpoint(checkpointIndex);

            // If this is the last checkpoint
            // if (checkpointIndex == CheckpointManager.Instance.totalCheckpoints - 1)
            // {
            //     LapTimer.Instance.StopTimer();
            //     FindObjectOfType<LapMessageUI>().ShowMessage("Lap Finished!\nTime: " +
            //         FindObjectOfType<LapTimerUI>().FormatTime(LapTimer.Instance.GetElapsedTime()), 10f);
            //     FindObjectOfType<RestartButtonHandler>().ShowButton();
            // }
        }
    }
}