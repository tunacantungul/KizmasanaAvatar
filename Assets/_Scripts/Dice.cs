using UnityEngine;
using UnityEngine.UI;

public class Dice : MonoBehaviour
{
    public Button rollButton;
    private GameManager _gameManager;
    private UIManager _uiManager;

    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        _uiManager = FindObjectOfType<UIManager>();
        
        if (rollButton != null)
        {
            rollButton.onClick.AddListener(RollForButton);
        }
    }

    // This public void method is called by the UI button
    public void RollForButton()
    {
        int result = Random.Range(1, 7);
        
        if (_uiManager != null)
        {
            _uiManager.UpdateDiceResult("Zar: " + result);
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



