using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemyKillTracker : MonoBehaviour
{
    private const string WinSceneName = "WinScene";

    [SerializeField] private TMP_Text enemyCountText;
    [SerializeField] private int totalEnemies = 4;

    private static EnemyKillTracker instance;
    private int killedEnemies;

    public static EnemyKillTracker Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<EnemyKillTracker>();
            }

            if (instance == null)
            {
                GameObject trackerObject = new GameObject("EnemyKillTracker");
                instance = trackerObject.AddComponent<EnemyKillTracker>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BindEnemyCountTextIfNeeded();
        UpdateUI();
    }

    public static void InitializeForScene(int enemyCount)
    {
        Instance.ResetForScene(enemyCount);
    }

    public static void NotifyEnemyKilled(EnemyStatus enemy)
    {
        Instance.RegisterKill();
    }

    private void ResetForScene(int enemyCount)
    {
        killedEnemies = 0;
        totalEnemies = Mathf.Max(0, enemyCount);
        BindEnemyCountTextIfNeeded();
        UpdateUI();
    }

    private void RegisterKill()
    {
        killedEnemies = Mathf.Clamp(killedEnemies + 1, 0, totalEnemies);
        UpdateUI();

        if (totalEnemies > 0 && killedEnemies >= totalEnemies)
        {
            SceneManager.LoadScene(WinSceneName);
        }
    }

    private void BindEnemyCountTextIfNeeded()
    {
        if (enemyCountText != null) return;

        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text.name == "EnemyCountText" || text.name == "EnemyCount" || text.text.Contains("Enemy Count"))
            {
                enemyCountText = text;
                break;
            }
        }

        if (enemyCountText == null)
        {
            enemyCountText = CreateEnemyCountText();
        }
    }

    private TMP_Text CreateEnemyCountText()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject textObject = new GameObject("EnemyCountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(40f, -92f);
        rect.sizeDelta = new Vector2(160f, 32f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = 20f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private void UpdateUI()
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = $"{killedEnemies}/{totalEnemies}";
        }
    }
}


