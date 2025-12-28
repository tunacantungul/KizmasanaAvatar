using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MinigameManager : MonoBehaviour
{
    // Simple Rock-Paper-Scissors minigame
    public enum Move { Rock, Paper, Scissors }

    [Header("UI Elements")]
    public Text statusText;
    public Button rockButton, paperButton, scissorsButton;

    private Move playerMove;
    private Move opponentMove;
    private PawnStateData playerPawnData;
    private PawnStateData opponentPawnData;


    void Start()
    {
        if (GameStateManager.Instance == null || GameStateManager.Instance.challenger == null || GameStateManager.Instance.defender == null)
        {
            // This should not happen if the flow is correct
            Debug.LogError("GameStateManager not found or incomplete! Returning to GameScene.");
            SceneManager.LoadScene("GameScene");
            return;
        }

        var challengerData = GameStateManager.Instance.challenger;
        var defenderData = GameStateManager.Instance.defender;

        // The player is always WaterTribe in a minigame
        playerPawnData = (Tile.PlayerType)(challengerData.pawnOwnerPlayerIndex + 1) == Tile.PlayerType.WaterTribe ? challengerData : defenderData;
        opponentPawnData = playerPawnData == challengerData ? defenderData : challengerData;
        
        var playerOwner = (Tile.PlayerType)(playerPawnData.pawnOwnerPlayerIndex + 1);
        var opponentOwner = (Tile.PlayerType)(opponentPawnData.pawnOwnerPlayerIndex + 1);

        statusText.text = $"SAVAŞ!\n{playerOwner} vs. {opponentOwner}\nHamleni seç!";

        rockButton.onClick.AddListener(() => PlayerChose(Move.Rock));
        paperButton.onClick.AddListener(() => PlayerChose(Move.Paper));
        scissorsButton.onClick.AddListener(() => PlayerChose(Move.Scissors));
    }

    void PlayerChose(Move move)
    {
        playerMove = move;
        opponentMove = (Move)Random.Range(0, 3); // AI makes a random choice

        rockButton.interactable = false;
        paperButton.interactable = false;
        scissorsButton.interactable = false;

        DetermineWinner();
    }

    void DetermineWinner()
    {
        string resultText = $"Senin seçimin: {playerMove}\nRakibin seçimi: {opponentMove}\n\n";

        if (playerMove == opponentMove)
        {
            // Tie
            resultText += "Berabere! Tekrar Oyna!";
            Invoke("ResetRound", 2f);
            statusText.text = resultText;
        }
        else if ((playerMove == Move.Rock && opponentMove == Move.Scissors) ||
                 (playerMove == Move.Paper && opponentMove == Move.Rock) ||
                 (playerMove == Move.Scissors && opponentMove == Move.Paper))
        {
            // Player wins
            resultText += "Kazandın!";
            GameStateManager.Instance.minigameWinner = (Tile.PlayerType)(playerPawnData.pawnOwnerPlayerIndex + 1);
            statusText.text = resultText;
            Invoke("EndMinigame", 2f);
        }
        else
        {
            // Opponent wins
            resultText += "Kaybettin!";
            GameStateManager.Instance.minigameWinner = (Tile.PlayerType)(opponentPawnData.pawnOwnerPlayerIndex + 1);
            statusText.text = resultText;
            Invoke("EndMinigame", 2f);
        }
    }

    void ResetRound()
    {
        rockButton.interactable = true;
        paperButton.interactable = true;
        scissorsButton.interactable = true;

        var playerOwner = (Tile.PlayerType)(playerPawnData.pawnOwnerPlayerIndex + 1);
        var opponentOwner = (Tile.PlayerType)(opponentPawnData.pawnOwnerPlayerIndex + 1);
        statusText.text = $"SAVAŞ!\n{playerOwner} vs. {opponentOwner}\nHamleni seç!";
    }

    void EndMinigame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
