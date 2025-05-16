using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomePageUI : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        SceneManager.LoadScene("RollPeak");
    }
}
