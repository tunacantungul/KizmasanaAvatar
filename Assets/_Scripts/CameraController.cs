using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // The target to frame, assign the _GameManager which has the Board script.
    public Transform target; 
    
    // Padding around the board
    public float padding = 2f; 

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Ensure the camera is orthographic for a 2D board game view
        cam.orthographic = true;

        // A small delay to ensure the board is fully generated before framing it.
        Invoke(nameof(FrameBoard), 0.2f);
    }

    void FrameBoard()
    {
        if (target == null)
        {
            // Try to find the GameManager if not assigned
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                target = gameManager.transform;
            }
            else
            {
                Debug.LogError("CameraController: Target not assigned and GameManager not found!");
                return;
            }
        }

        // Calculate the bounds of all children of the target (all the tiles)
        Bounds bounds = new Bounds(target.position, Vector3.zero);
        var renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // --- FIXES ARE HERE ---

        // 1. Set rotation for a perfect top-down view
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 2. Calculate the required orthographic size
        float boardSize = Mathf.Max(bounds.size.x, bounds.size.z);
        float requiredSize = (boardSize / 2f) + padding;
        cam.orthographicSize = requiredSize;

        // 3. Position the camera above the center of the board
        // Using a high 'y' value is safe for an orthographic camera
        Vector3 targetPosition = new Vector3(bounds.center.x, 100f, bounds.center.z);
        transform.position = targetPosition;
    }
}
