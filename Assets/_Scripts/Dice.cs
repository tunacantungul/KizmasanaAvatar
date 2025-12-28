using UnityEngine;
using UnityEngine.UI;

public class Dice : MonoBehaviour
{
    public Button rollButton;
    private GameManager _gameManager;
    private UIManager _uiManager;

    void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _uiManager = FindFirstObjectByType<UIManager>();
        
        if (rollButton == null)
        {
            Debug.LogError("CRITICAL ERROR: Roll Button, Dice script'ine atanmamış! Lütfen Unity Inspector'dan atamayı yapın.");
            return;
        }

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



