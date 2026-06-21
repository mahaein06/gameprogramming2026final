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

    private const string FirstLine = "- ?몃Ŋ鍮?";
    private const string AfterChoiceLine = "- ?類ㅻ뎁, ?怨뺚봺???믩객????용┛ 獄쏅뗄嫄??블? ??苡뀐쭕??뵠??";
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
        AutoAssignMissingReferences();
        KeepManagerOutsideDialoguePanel();
        PrepareWorldPrompt();
        CreateMissingChoiceUI();

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
            if (currentAnimal != null && promptCanInteract && fPrompt != null && fPrompt.activeInHierarchy && WasInteractPressed())
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
        promptCanInteract = false;
        SetActiveSafe(fPrompt, false);
    }

    public void StartDialogue(AnimalDialogue animal)
    {
        if (animal == null) return;

        currentAnimal = animal;
        isTalking = true;
        waitingForChoice = true;
        canCloseWithClick = false;
        promptCanInteract = false;

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

    private void AutoAssignMissingReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = FindAnyObjectByType<PlayerStatus>();
        }

        if (dialoguePanel == null)
        {
            GameObject foundDialoguePanel = GameObject.Find("DialoguePanel");
            if (foundDialoguePanel != null)
            {
                dialoguePanel = foundDialoguePanel;
            }
        }

        if (fPrompt == null)
        {
            GameObject foundPrompt = GameObject.Find("FPrompText");
            if (foundPrompt == null)
            {
                foundPrompt = GameObject.Find("FPromptText");
            }

            if (foundPrompt != null)
            {
                fPrompt = foundPrompt;
            }
        }

        if (dialoguePanel == null) return;

        if (portraitImage == null)
        {
            Transform portrait = dialoguePanel.transform.Find("PortraitImage");
            if (portrait != null)
            {
                portraitImage = portrait.GetComponent<Image>();
            }
        }

        if (nameText == null)
        {
            Transform name = dialoguePanel.transform.Find("NameText");
            if (name != null)
            {
                nameText = name.GetComponent<TMP_Text>();
            }
        }

        if (dialogueText == null)
        {
            Transform text = dialoguePanel.transform.Find("DialogueText");
            if (text != null)
            {
                dialogueText = text.GetComponent<TMP_Text>();
            }
        }
    }

    private void CreateMissingChoiceUI()
    {
        if (dialoguePanel == null) return;

        if (choicePanel == null)
        {
            Transform existingChoicePanel = dialoguePanel.transform.Find("ChoicePanel");
            if (existingChoicePanel != null)
            {
                choicePanel = existingChoicePanel.gameObject;
            }
            else
            {
                choicePanel = new GameObject("ChoicePanel", typeof(RectTransform));
                choicePanel.transform.SetParent(dialoguePanel.transform, false);
                RectTransform rect = choicePanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 20f);
                rect.sizeDelta = new Vector2(420f, 60f);
            }
        }

        if (healButton == null)
        {
            healButton = GetOrCreateChoiceButton("HealButton", "筌ｋ????겸뫗???띾┛", new Vector2(-110f, 0f));
        }

        if (reloadButton == null)
        {
            reloadButton = GetOrCreateChoiceButton("ReloadButton", "?μ빘釉??關???띾┛", new Vector2(110f, 0f));
        }
    }

    private Button GetOrCreateChoiceButton(string objectName, string label, Vector2 anchoredPosition)
    {
        Transform existing = choicePanel.transform.Find(objectName);
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
            {
                return existingButton;
            }
        }

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(choicePanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(200f, 44f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text buttonText = textObject.GetComponent<TMP_Text>();
        buttonText.text = label;
        buttonText.fontSize = 20f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.black;

        return buttonObject.GetComponent<Button>();
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