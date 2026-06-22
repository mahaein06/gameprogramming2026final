using TMPro;
using UnityEngine;
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
    }

    private void UpdateUI()
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = $"{killedEnemies}/{totalEnemies}";
        }
    }
}

