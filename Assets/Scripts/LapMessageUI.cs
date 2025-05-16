using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LapMessageUI : MonoBehaviour
{
    public Text messageText; // Assign in Inspector
    public static LapMessageUI Instance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        StopAllCoroutines();
        StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messageText.text = message;
        messageText.enabled = true;
        yield return new WaitForSeconds(duration);
        messageText.enabled = false;
    }

    public void ShowDebug(string debugMessage, float duration = 3f)
    {
        ShowMessage(debugMessage, duration);
    }

    public static void DebugToCanvas(string msg, float duration = 3f)
    {
        if (Instance != null)
            Instance.ShowDebug(msg, duration);
    }
}
