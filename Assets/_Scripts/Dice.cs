using UnityEngine;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Dice : MonoBehaviour
{
    public Button rollButton;
    public TextMeshProUGUI resultText;
    private GameManager _gameManager;

    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        if (rollButton != null)
        {
            // Point the listener to a void method
            rollButton.onClick.AddListener(RollForButton);
        }
    }

    // This public void method is called by the UI button
    public void RollForButton()
    {
        int result = Random.Range(1, 7);
        
        if (resultText != null)
        {
            resultText.text = "Zar: " + result;
        }

        if (_gameManager != null)
        {
            _gameManager.OnDiceRolled(result);
        }
        else
        {
            Debug.LogError("Dice script cannot find the GameManager!");
        }
    }
}


