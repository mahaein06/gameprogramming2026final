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

    private const string FirstLine = "- 뭐야?";
    private const string AfterChoiceLine = "- 형씨, 우리도 먹고 살기 바쁘다고. 이번만이야.";
    private const float WorldPromptScale = 0.01f;

    private AnimalDialogue currentAnimal;
    private bool isTalking;
    private bool waitingForChoice;
    private bool canCloseWithClick;
    private RectTransform fPromptRect;
    private Canvas fPromptWorldCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DialogueManager: Multiple instances found. Keeping the newest one.", this);
        }

        Instance = this;
        PrepareWorldPrompt();

        SetActiveSafe(fPrompt, false);
        SetActiveSafe(dialoguePanel, false);
        SetActiveSafe(choicePanel, false);

        if (healButton != null)
        {
            healButton.onClick.RemoveListener(OnHealSelected);
            healButton.onClick.AddListener(OnHealSelected);
        }
        else
        {
            Debug.LogWarning("DialogueManager: Heal Button is not assigned.", this);
        }

        if (reloadButton != null)
        {
            reloadButton.onClick.RemoveListener(OnReloadSelected);
            reloadButton.onClick.AddListener(OnReloadSelected);
        }
        else
        {
            Debug.LogWarning("DialogueManager: Reload Button is not assigned.", this);
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
            if (currentAnimal != null && WasInteractPressed())
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
            UpdatePromptWorldPose();
        }
    }

    public void HidePrompt(AnimalDialogue animal)
    {
        if (currentAnimal != animal) return;

        currentAnimal = null;
        SetActiveSafe(fPrompt, false);
    }

    public void StartDialogue(AnimalDialogue animal)
    {
        if (animal == null) return;

        currentAnimal = animal;
        isTalking = true;
        waitingForChoice = true;
        canCloseWithClick = false;

        SetActiveSafe(fPrompt, false);
        SetActiveSafe(dialoguePanel, true);
        SetActiveSafe(choicePanel, true);

        if (portraitImage != null)
        {
            portraitImage.sprite = animal.portrait;
            portraitImage.enabled = animal.portrait != null;
        }
        else
        {
            Debug.LogWarning("DialogueManager: Portrait Image is not assigned.", this);
        }

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(animal.animalName) ? "Chicken" : animal.animalName;
        }
        else
        {
            Debug.LogWarning("DialogueManager: Name Text is not assigned.", this);
        }

        SetDialogueText(FirstLine);
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
            return;
        }

        Vector3 promptPosition = currentAnimal.GetPromptWorldPosition();
        Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(promptPosition);
        bool isVisible = viewportPosition.z > 0f;
        SetActiveSafe(fPrompt, isVisible);
        if (!isVisible) return;

        fPrompt.transform.position = promptPosition;
        fPrompt.transform.rotation = cameraToUse.transform.rotation;
        fPrompt.transform.localScale = Vector3.one * WorldPromptScale;
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    private static bool WasLeftClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
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
