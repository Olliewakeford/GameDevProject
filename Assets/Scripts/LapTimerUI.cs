using UnityEngine;
using UnityEngine.UI;

public class LapTimerUI : MonoBehaviour
{
    public Text timerText; // Assign in Inspector

    void Update()
    {
        if (LapTimer.Instance != null)
        {
            float time = LapTimer.Instance.GetElapsedTime();
            timerText.text = FormatTime(time);
        }
    }

    public string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        int milliseconds = Mathf.FloorToInt((time * 1000F) % 1000F);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }
}