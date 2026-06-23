using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitleButton : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";

    private void OnMouseDown()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachIfNeeded(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachIfNeeded(scene);
    }

    private static void AttachIfNeeded(Scene scene)
    {
        if (scene.name != "WinScene" && scene.name != "LoseScene") return;

        GameObject square = FindBackToTitleSquare();
        if (square == null) return;

        if (square.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = square.AddComponent<BoxCollider2D>();
            SpriteRenderer spriteRenderer = square.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
            }
        }

        if (square.GetComponent<BackToTitleButton>() == null)
        {
            square.AddComponent<BackToTitleButton>();
        }
    }

    private static GameObject FindBackToTitleSquare()
    {
        GameObject backToTitle = GameObject.Find("BackToTitle");
        if (backToTitle == null) return null;

        Transform square = backToTitle.transform.Find("Square");
        if (square == null)
        {
            square = backToTitle.transform.Find("Square (1)");
        }

        if (square != null) return square.gameObject;

        foreach (Transform child in backToTitle.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("Square"))
            {
                return child.gameObject;
            }
        }

        return backToTitle;
    }
}
