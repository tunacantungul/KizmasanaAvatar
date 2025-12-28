// In file: Assets/CP/Scripts/KarateGame/KarateGameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class KarateGameManager : MonoBehaviour
{
    [Header("Character Sprites")]
    public Image playerCharacterDisplay;
    public Image npcCharacterDisplay;
    public Sprite korraSprite;
    public Sprite fireNpcSprite;
    public Sprite waterNpcSprite;
    public Sprite earthNpcSprite;

    [Header("Projectile & Effect Prefabs")]
    public RectTransform playerProjectileSpawn;
    public RectTransform npcProjectileSpawn;
    public Image fireProjectile;
    public Image waterProjectile;
    public Image earthProjectile;
    public Image playerAirShield;
    public Image npcAirShield;

    [Header("Health & Round UI")]
    public Text playerLivesText;
    public Text npcLivesText;
    public Text roundResultText;

    [Header("Card Display")]
    public List<CardController> playerCardControllers;
    public CardController npcCardDisplay;

    [Header("Game Over UI")]
    public GameObject gameOverScreen;
    public Text gameOverText;

    [Header("Game State")]
    public int startingLives = 5;
    public float projectileAnimationTime = 1.0f;
    public float resultDisplayTime = 1.5f;

    // --- PRIVATE GAME STATE ---
    private int playerLives;
    private int npcLives;
    private List<Element> npcHand = new List<Element>();
    private Element npcElement;
    private bool isRoundInProgress = false;

    void Start()
    {
        SetupNpc();
        SetupCharacters();
        InitializeGame();
        CheckForMissingLinks(); // Keep this for debugging
    }

    public void PlayerSelectsCard(Element playerChoice, CardController card)
    {
        if (isRoundInProgress) return;
        StartCoroutine(PlayRoundCoroutine(playerChoice, card));
    }

    private IEnumerator PlayRoundCoroutine(Element playerChoice, CardController card)
    {
        isRoundInProgress = true;

        Element npcChoice = GetNpcChoice();
        if (npcCardDisplay != null)
        {
            npcCardDisplay.gameObject.SetActive(true);
            npcCardDisplay.SetElement(npcChoice);
        }

        // Handle Air card shields first
        if (playerChoice == Element.Air) { StartCoroutine(AnimateAirShield(playerAirShield)); }
        if (npcChoice == Element.Air) { StartCoroutine(AnimateAirShield(npcAirShield)); }

        string result = DetermineWinner(playerChoice, npcChoice);
        string resultMessage = "";
        Image projectileToUse = null;
        RectTransform startSpawn = null;
        RectTransform endSpawn = null;
        Quaternion projectileRotation = Quaternion.identity; // Default: no rotation

        switch (result)
        {
            case "player":
                npcLives--;
                resultMessage = $"You win! {playerChoice} beats {npcChoice}.";
                projectileToUse = GetProjectileForElement(playerChoice);
                startSpawn = playerProjectileSpawn;
                endSpawn = npcProjectileSpawn;
                // Player's projectile faces "forward" (no rotation)
                projectileRotation = Quaternion.identity; 
                break;
            case "npc":
                playerLives--;
                resultMessage = $"You lose. {npcChoice} beats {playerChoice}.";
                projectileToUse = GetProjectileForElement(npcChoice);
                startSpawn = npcProjectileSpawn;
                endSpawn = playerProjectileSpawn;
                // NPC's projectile rotated 180 degrees
                projectileRotation = Quaternion.Euler(0, 0, 180); 
                break;
            case "draw":
                resultMessage = "It's a draw.";
                break;
        }

        if (roundResultText != null) roundResultText.text = resultMessage;
        UpdateUI();

        // Animate projectile if it was a winning attack
        if (projectileToUse != null && startSpawn != null && endSpawn != null)
        {
            yield return StartCoroutine(AnimateProjectile(projectileToUse, startSpawn.position, endSpawn.position, projectileRotation));
        }
        else // Otherwise, just wait to show the result
        {
            yield return new WaitForSeconds(resultDisplayTime);
        }

        if (playerLives <= 0 || npcLives <= 0)
        {
            EndGame();
        }
        else
        {
            DrawNewCardForPlayer(card);
            DrawNewCardForNpc();
            EndRound();
        }
    }

    private void InitializeGame()
    {
        playerLives = startingLives;
        npcLives = startingLives;

        foreach (var card in playerCardControllers) { DrawNewCardForPlayer(card); }
        DrawNewCardForNpc(true);

        UpdateUI();

        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        EndRound(); // Cleans up UI for the start
    }
    
    private void EndRound()
    {
        if (roundResultText != null) roundResultText.text = "";
        if (npcCardDisplay != null) npcCardDisplay.gameObject.SetActive(false);
        if (playerAirShield != null) playerAirShield.gameObject.SetActive(false);
        if (npcAirShield != null) npcAirShield.gameObject.SetActive(false);
        isRoundInProgress = false;
    }

    private void SetupNpc()
    {
        if (GameData.IsInitialized)
        {
            npcElement = GameData.NpcElement;
        }
        else // For testing purposes
        {
            int randomElementIndex = Random.Range(0, 3); // 0=Fire, 1=Water, 2=Earth
            npcElement = (Element)randomElementIndex;
        }
    }
    
    private void SetupCharacters()
    {
        if (playerCharacterDisplay != null) playerCharacterDisplay.sprite = korraSprite;
        if (npcCharacterDisplay != null)
        {
            switch (npcElement)
            {
                case Element.Fire: npcCharacterDisplay.sprite = fireNpcSprite; break;
                case Element.Water: npcCharacterDisplay.sprite = waterNpcSprite; break;
                case Element.Earth: npcCharacterDisplay.sprite = earthNpcSprite; break;
            }
        }
    }

    private string DetermineWinner(Element player, Element npc)
    {
        if (player == Element.Air || npc == Element.Air) return "draw";
        if (player == npc) return "draw";
        if ((player == Element.Water && npc == Element.Fire) || (player == Element.Fire && npc == Element.Earth) || (player == Element.Earth && npc == Element.Water)) return "player";
        return "npc";
    }

    private void DrawNewCardForPlayer(CardController card)
    {
        card.SetElement((Element)Random.Range(0, 4));
    }
    
    private void DrawNewCardForNpc(bool isInitialDeal = false)
    {
        if (isInitialDeal)
        {
            npcHand.Clear();
            for (int i = 0; i < 4; i++) { npcHand.Add(GetNpcCardElement()); }
        }
        else
        {
            int indexToReplace = Random.Range(0, npcHand.Count);
            npcHand[indexToReplace] = GetNpcCardElement();
        }
    }

    private Element GetNpcCardElement()
    {
        // NPCs have their own element plus a chance to draw a defensive Air card.
        if (Random.Range(0f, 1f) < 0.25f) // 25% chance for an Air card
        {
            return Element.Air;
        }
        return npcElement;
    }

    private Element GetNpcChoice() => npcHand[Random.Range(0, npcHand.Count)];
    
    private Image GetProjectileForElement(Element element)
    {
        switch (element)
        {
            case Element.Fire: return fireProjectile;
            case Element.Water: return waterProjectile;
            case Element.Earth: return earthProjectile;
            default: return null;
        }
    }

    private IEnumerator AnimateProjectile(Image projectile, Vector3 startPos, Vector3 endPos, Quaternion startRotation)
    {
        projectile.gameObject.SetActive(true);
        projectile.transform.position = startPos;
        projectile.transform.rotation = startRotation; // Apply initial rotation
        float timeElapsed = 0;
        while (timeElapsed < projectileAnimationTime)
        {
            projectile.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / projectileAnimationTime);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        projectile.gameObject.SetActive(false);
        projectile.transform.rotation = Quaternion.identity; // Reset rotation for next use
    }
    
    private IEnumerator AnimateAirShield(Image shield)
    {
        if (shield == null) yield break;
        shield.gameObject.SetActive(true);
        yield return new WaitForSeconds(resultDisplayTime);
        shield.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (playerLivesText != null) playerLivesText.text = $"Lives: {playerLives}";
        if (npcLivesText != null) npcLivesText.text = $"Lives: {npcLives}";
    }

    private void EndGame()
    {
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        if (gameOverText != null)
        {
             if (playerLives <= 0) { gameOverText.text = "You Lose!"; } else { gameOverText.text = "You Win!"; }
        }
    }
    
    private void CheckForMissingLinks()
    {
        if (playerCharacterDisplay == null) Debug.LogWarning("KarateGameManager: 'playerCharacterDisplay' is not linked.");
        if (npcCharacterDisplay == null) Debug.LogWarning("KarateGameManager: 'npcCharacterDisplay' is not linked.");
        if (playerProjectileSpawn == null || npcProjectileSpawn == null) Debug.LogWarning("KarateGameManager: Projectile spawn points are not linked.");
        if (fireProjectile == null || waterProjectile == null || earthProjectile == null) Debug.LogWarning("KarateGameManager: One or more projectile images are not linked.");
        if (playerLivesText == null || npcLivesText == null || roundResultText == null) Debug.LogWarning("KarateGameManager: One or more UI Text elements are not linked.");
    }
}
