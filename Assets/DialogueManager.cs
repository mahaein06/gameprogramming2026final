using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject fPrompt;
    public GameObject dialoguePanel;
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public GameObject choicePanel;
    public Button healButton;
    public Button reloadButton;

    [Header("Player")]
    public PlayerStatus playerStatus;

    private const string FirstLine = "\u002D \uBB50\uC57C?";
    private const string AfterChoiceLine = "\u002D \uD615\uC528, \uC6B0\uB9AC\uB3C4 \uBA39\uACE0 \uC0B4\uAE30 \uBC14\uC058\uB2E4\uACE0. \uC774\uBC88\uB9CC\uC774\uC57C.";
    private const float WorldPromptScale = 0.005f;

    private AnimalDialogue currentAnimal;
    private bool isTalking;
    private bool waitingForChoice;
    private bool canCloseWithClick;
    private bool promptCanInteract;
    private RectTransform fPromptRect;
    private Canvas fPromptWorldCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DialogueManager: Multiple instances found. Keeping the newest one.", this);
        }

        Instance = this;
        AssignOnlyMissingNonUiReferences();
        KeepManagerOutsideDialoguePanel();
        PrepareWorldPrompt();

        SetActiveSafe(fPrompt, false);
        SetActiveSafe(dialoguePanel, false);
        SetActiveSafe(choicePanel, false);

        if (healButton != null)
        {
            healButton.onClick.RemoveListener(OnHealSelected);
            healButton.onClick.AddListener(OnHealSelected);
        }

        if (reloadButton != null)
        {
            reloadButton.onClick.RemoveListener(OnReloadSelected);
            reloadButton.onClick.AddListener(OnReloadSelected);
        }
    }

    private void LateUpdate()
    {
        UpdatePromptWorldPose();
    }

    private void Update()
    {
        if (!isTalking)
        {
            if (currentAnimal != null && promptCanInteract && WasInteractPressed())
            {
                StartDialogue(currentAnimal);
            }

            return;
        }

        if (waitingForChoice) return;

        if (canCloseWithClick && WasLeftClickPressed())
        {
            EndDialogue();
        }
    }

    public bool IsTalking()
    {
        return isTalking;
    }

    public void ShowPrompt(AnimalDialogue animal)
    {
        if (animal == null) return;

        currentAnimal = animal;

        if (!isTalking)
        {
            SetActiveSafe(fPrompt, true);
            promptCanInteract = fPrompt != null;
            UpdatePromptWorldPose();
        }
    }

    public void HidePrompt(AnimalDialogue animal)
    {
        if (currentAnimal != animal) return;

        currentAnimal = null;
        promptCanInteract = false;
        SetActiveSafe(fPrompt, false);
    }

    public void StartDialogue(AnimalDialogue animal)
    {
        if (animal == null) return;

        currentAnimal = animal;
        isTalking = true;
        promptCanInteract = false;

        bool hasChoices = choicePanel != null && healButton != null && reloadButton != null;
        waitingForChoice = hasChoices;
        canCloseWithClick = !hasChoices;

        SetActiveSafe(fPrompt, false);
        SetActiveSafe(dialoguePanel, true);
        SetActiveSafe(choicePanel, hasChoices);

        if (portraitImage != null)
        {
            portraitImage.sprite = animal.portrait;
            portraitImage.enabled = animal.portrait != null;
        }

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(animal.animalName) ? "Chicken" : animal.animalName;
        }

        SetDialogueText(FirstLine);

        if (!hasChoices)
        {
            Debug.LogWarning("DialogueManager: ChoicePanel/HealButton/ReloadButton are not assigned. Existing DialoguePanel will show without choices.", this);
        }
    }

    public void EndDialogue()
    {
        isTalking = false;
        waitingForChoice = false;
        canCloseWithClick = false;

        SetActiveSafe(dialoguePanel, false);
        SetActiveSafe(choicePanel, false);

        if (currentAnimal != null)
        {
            SetActiveSafe(fPrompt, true);
            promptCanInteract = fPrompt != null;
            UpdatePromptWorldPose();
        }
    }

    private void OnHealSelected()
    {
        if (playerStatus != null)
        {
            playerStatus.HealFull();
        }
        else
        {
            Debug.LogWarning("DialogueManager: Player Status is not assigned.", this);
        }

        ContinueAfterChoice();
    }

    private void OnReloadSelected()
    {
        if (playerStatus != null)
        {
            playerStatus.ReloadFull();
        }
        else
        {
            Debug.LogWarning("DialogueManager: Player Status is not assigned.", this);
        }

        ContinueAfterChoice();
    }

    private void ContinueAfterChoice()
    {
        waitingForChoice = false;
        canCloseWithClick = true;
        SetActiveSafe(choicePanel, false);
        SetDialogueText(AfterChoiceLine);
    }

    private void PrepareWorldPrompt()
    {
        if (fPrompt == null) return;

        if (fPrompt.transform.parent != null)
        {
            fPrompt.transform.SetParent(null, false);
        }

        fPromptRect = fPrompt.GetComponent<RectTransform>();
        if (fPromptRect == null)
        {
            fPromptRect = fPrompt.AddComponent<RectTransform>();
        }

        fPromptWorldCanvas = fPrompt.GetComponent<Canvas>();
        if (fPromptWorldCanvas == null)
        {
            fPromptWorldCanvas = fPrompt.AddComponent<Canvas>();
        }

        fPromptWorldCanvas.renderMode = RenderMode.WorldSpace;
        fPromptWorldCanvas.overrideSorting = true;
        fPromptWorldCanvas.sortingOrder = 500;

        fPromptRect.localScale = Vector3.one * WorldPromptScale;
        fPromptRect.pivot = new Vector2(0.5f, 0.5f);
        fPromptRect.anchorMin = new Vector2(0.5f, 0.5f);
        fPromptRect.anchorMax = new Vector2(0.5f, 0.5f);
        if (fPromptRect.sizeDelta == Vector2.zero)
        {
            fPromptRect.sizeDelta = new Vector2(200f, 50f);
        }
    }

    private void UpdatePromptWorldPose()
    {
        if (fPrompt == null || currentAnimal == null || isTalking) return;

        if (fPromptRect == null || fPromptWorldCanvas == null)
        {
            PrepareWorldPrompt();
        }

        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
        {
            SetActiveSafe(fPrompt, false);
            promptCanInteract = false;
            return;
        }

        Vector3 promptPosition = currentAnimal.GetPromptWorldPosition();
        Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(promptPosition);
        bool isVisible = viewportPosition.z > 0f;
        SetActiveSafe(fPrompt, isVisible);
        promptCanInteract = isVisible;
        if (!isVisible) return;

        fPrompt.transform.position = promptPosition;
        fPrompt.transform.rotation = GetUprightBillboardRotation(cameraToUse, promptPosition);
        fPrompt.transform.localScale = Vector3.one * WorldPromptScale;
    }

    private static Quaternion GetUprightBillboardRotation(Camera cameraToUse, Vector3 promptPosition)
    {
        Vector3 directionToCamera = promptPosition - cameraToUse.transform.position;
        if (directionToCamera.sqrMagnitude < 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(directionToCamera, Vector3.up);
    }

    private void KeepManagerOutsideDialoguePanel()
    {
        if (dialoguePanel != null && transform.IsChildOf(dialoguePanel.transform))
        {
            transform.SetParent(null, true);
        }
    }

    private void AssignOnlyMissingNonUiReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = FindAnyObjectByType<PlayerStatus>();
        }
    }

    private static T FindDeepChildComponent<T>(Transform parent, string childName) where T : Component
    {
        Transform child = FindDeepChild(parent, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindDeepChild(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = pressed || Input.GetKeyDown(KeyCode.F);
#endif
        return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F);
#else
        return false;
#endif
    }

    private static bool WasLeftClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = pressed || Input.GetMouseButtonDown(0);
#endif
        return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void SetDialogueText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
        else
        {
            Debug.LogWarning("DialogueManager: Dialogue Text is not assigned.", this);
        }
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}