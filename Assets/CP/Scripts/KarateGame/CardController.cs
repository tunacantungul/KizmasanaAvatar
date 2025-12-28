// In file: Assets/CP/Scripts/KarateGame/CardController.cs
using UnityEngine;
using UnityEngine.EventSystems; // Required for IPointerClickHandler
using UnityEngine.UI;           // Required for Image

// This component should be attached to a UI Image GameObject.
[RequireComponent(typeof(Image))] 
public class CardController : MonoBehaviour, IPointerClickHandler
{
    // --- PUBLIC FIELDS (Assign these in the Unity Inspector) ---
    [Header("Card Sprites")]
    public Sprite fireSprite;
    public Sprite waterSprite;
    public Sprite earthSprite;
    public Sprite airSprite;

    // --- PRIVATE FIELDS ---
    private Element currentElement;
    private KarateGameManager gameManager;
    
    // This script now works with a UI Image
    private Image cardImage;

    void Awake()
    {
        // Find the game manager in the scene. 
        // A more robust method for complex scenes might be a singleton pattern.
        gameManager = FindFirstObjectByType<KarateGameManager>();

        // Get the Image component attached to this GameObject
        cardImage = GetComponent<Image>();
    }

    /// <summary>
    /// Sets the element for this card and updates its visual representation.
    /// </summary>
    public void SetElement(Element newElement)
    {
        currentElement = newElement;
        
        // You would expand this to change sprites or materials based on the element
        gameObject.name = $"Card_{newElement}"; 
        UpdateCardVisual();
    }

    /// <summary>
    /// Called by Unity when the GameObject this script is on is clicked.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Tell the game manager that this card was selected
        if (gameManager != null)
        {
            gameManager.PlayerSelectsCard(currentElement, this);
        }
    }

    /// <summary>
    /// Updates the card's sprite based on its element.
    /// </summary>
    private void UpdateCardVisual()
    {
        // Reset color to white to ensure the sprite's original colors are shown
        if (cardImage != null)
        {
            cardImage.color = Color.white; 
        }

        Sprite selectedSprite = null;
        switch (currentElement)
        {
            case Element.Fire:
                selectedSprite = fireSprite;
                break;
            case Element.Water:
                selectedSprite = waterSprite;
                break;
            case Element.Earth:
                selectedSprite = earthSprite;
                break;
            case Element.Air:
                selectedSprite = airSprite;
                break;
        }

        if (cardImage != null && selectedSprite != null)
        {
            cardImage.sprite = selectedSprite;
        }
        else if (cardImage != null) // Fallback if no sprite is assigned for an element, set a default color
        {
            Debug.LogWarning($"No sprite assigned for {currentElement} in {gameObject.name}. Falling back to color.");
            switch (currentElement) // Re-add color fallback for visual distinction if no sprite
            {
                case Element.Fire: cardImage.color = new Color(1.0f, 0.27f, 0.0f); break;
                case Element.Water: cardImage.color = new Color(0.11f, 0.56f, 1.0f); break;
                case Element.Earth: cardImage.color = new Color(0.54f, 0.27f, 0.07f); break;
                case Element.Air: cardImage.color = new Color(0.67f, 0.84f, 0.9f); break;
                default: cardImage.color = Color.magenta; break; // Error color
            }
        }
    }
}
