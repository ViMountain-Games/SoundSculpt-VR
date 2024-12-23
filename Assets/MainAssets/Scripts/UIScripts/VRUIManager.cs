using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // For UI components
using System.Collections.Generic;

public class VRUIManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The target of the UI in the world.")]
    public Transform uiTarget;

    [Tooltip("The GameObject of the UI (e.g., Canvas) used for positioning.")]
    public GameObject uiCanvas;

    [Tooltip("The GameObject to activate/deactivate (e.g., the menu window).")]
    public GameObject menuWindow;

    [Header("Input Settings")]
    [Tooltip("Input Action to toggle the UI.")]
    public InputActionProperty toggleUIInput;

    [Tooltip("Enable or disable the use of input to toggle the UI.")]
    public bool enableUIWithInput = true; // Default set to true

    [Header("Movement Settings")]
    [Tooltip("Speed at which the UI follows the target.")]
    public float followSpeed = 10f;

    [Tooltip("Rotation speed of the UI towards the player.")]
    public float rotationSpeed = 10f;

    private bool isUIActive = false;
    private Transform playerTransform;

    private void Start()
    {
        // Find the player via the "Player" tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player not found! Ensure the player has the tag 'Player'.");
        }

        // Deactivate the menu window initially
        if (menuWindow != null)
        {
            menuWindow.SetActive(false);
        }

        // Associate the input action
        if (toggleUIInput != null)
        {
            toggleUIInput.action.performed += HandleToggleUIInput;
        }
    }

    private void OnDestroy()
    {
        // Remove the listener for input when the script is destroyed
        if (toggleUIInput != null)
        {
            toggleUIInput.action.performed -= HandleToggleUIInput;
        }
    }

    private void Update()
    {
        if (uiCanvas != null && uiTarget != null && playerTransform != null)
        {
            // Smoothly position the UI towards the target
            Vector3 targetPosition = uiTarget.position;
            uiCanvas.transform.position = Vector3.Lerp(uiCanvas.transform.position, targetPosition, followSpeed * Time.deltaTime);

            // Calculate the direction towards the player
            Vector3 directionToPlayer = (playerTransform.position - uiCanvas.transform.position).normalized;

            // Rotate the canvas towards the player with a 180-degree inversion
            Quaternion targetRotation = Quaternion.LookRotation(-directionToPlayer, Vector3.up);
            uiCanvas.transform.rotation = Quaternion.Slerp(uiCanvas.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleToggleUIInput(InputAction.CallbackContext context)
    {
        if (enableUIWithInput)
        {
            ToggleUI();
        }
    }

    private void ToggleUI()
    {
        if (menuWindow != null)
        {
            isUIActive = !isUIActive;

            if (isUIActive)
            {
                // Activate the menu window
                menuWindow.SetActive(true);
            }
            else
            {
                // Deactivate the menu window
                menuWindow.SetActive(false);
            }
        }
    }
}
