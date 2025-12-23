using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI diceResultText;
    public TextMeshProUGUI scoreText;
    public Toggle autoPlayToggle;

    private GameManager _gameManager;

    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        if (autoPlayToggle != null && _gameManager != null)
        {
            autoPlayToggle.onValueChanged.AddListener((isActive) => {
                _gameManager.SetAutoPlay(isActive);
            });
        }
    }

    public void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void UpdateDiceResult(string message)
    {
        if (diceResultText != null)
        {
            diceResultText.text = message;
        }
    }

    public void UpdateScore(string scoreMessage)
    {
        if (scoreText != null)
        {
            scoreText.text = scoreMessage;
        }
    }
}

