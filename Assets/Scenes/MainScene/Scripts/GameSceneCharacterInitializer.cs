using ithappy.Animals_FREE;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneCharacterInitializer : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string PlayerTag = "Player";
    private const string EnemyTag = "Enemy";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyIfGameScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyIfGameScene(scene);
    }

    private static void ApplyIfGameScene(Scene scene)
    {
        if (scene.name != GameSceneName) return;

        var selected = CharacterSelectionState.SelectedAnimal;
        var camera = FindAnyObjectByType<ThirdPersonCamera>();
        var selectedRoot = FindAnimalRoot(selected);

        foreach (SelectableAnimal animal in System.Enum.GetValues(typeof(SelectableAnimal)))
        {
            var root = FindAnimalRoot(animal);
            if (root == null) continue;

            bool isPlayer = animal == selected;
            SetTagSafely(root, isPlayer ? PlayerTag : EnemyTag);
            ConfigureInput(root, camera, isPlayer);
            ConfigureShooter(root, isPlayer);
            ConfigurePlayerStatus(root, isPlayer);
        }

        if (camera != null && selectedRoot != null)
        {
            camera.BindPlayer(selectedRoot.transform);
            camera.ReinitializeFromCurrentTransform();
        }
    }

    private static GameObject FindAnimalRoot(SelectableAnimal animal)
    {
        string primaryName = GetSceneObjectName(animal);
        var root = GameObject.Find(primaryName);
        if (root != null) return root;

        if (animal == SelectableAnimal.Penguin)
        {
            root = GameObject.Find("Penguin");
            if (root != null) return root;
        }

        foreach (var mover in FindObjectsByType<CreatureMover>(FindObjectsInactive.Include))
        {
            if (mover.name == primaryName || mover.name.StartsWith(primaryName + " "))
            {
                return mover.gameObject;
            }
        }

        return null;
    }

    private static string GetSceneObjectName(SelectableAnimal animal)
    {
        switch (animal)
        {
            case SelectableAnimal.Deer:
                return "Deer";
            case SelectableAnimal.Horse:
                return "Horse";
            case SelectableAnimal.Penguin:
                return "Pinguin";
            case SelectableAnimal.Dog:
                return "Dog";
            case SelectableAnimal.Tiger:
                return "Tiger";
            default:
                return "Dog";
        }
    }

    private static void ConfigureInput(GameObject root, ThirdPersonCamera camera, bool isPlayer)
    {
        var input = root.GetComponent<MovePlayerInput>();
        if (input == null && isPlayer)
        {
            input = root.AddComponent<MovePlayerInput>();
        }

        if (input == null) return;

        input.enabled = isPlayer;
        if (isPlayer)
        {
            input.BindMover(root.GetComponent<CreatureMover>());
            input.BindCamera(camera);
        }
    }

    private static void ConfigureShooter(GameObject root, bool isPlayer)
    {
        var shooter = root.GetComponent<DogRpgShooter>();
        if (shooter == null && isPlayer)
        {
            shooter = root.AddComponent<DogRpgShooter>();
        }

        if (shooter == null) return;

        shooter.enabled = isPlayer;
        if (isPlayer)
        {
            shooter.ConfigureForPlayer(root.GetComponent<PlayerStatus>() ?? root.AddComponent<PlayerStatus>());
        }
    }

    private static void ConfigurePlayerStatus(GameObject root, bool isPlayer)
    {
        var status = root.GetComponent<PlayerStatus>();
        if (status == null && isPlayer)
        {
            status = root.AddComponent<PlayerStatus>();
        }

        if (status != null)
        {
            status.enabled = isPlayer;
        }
    }

    private static void SetTagSafely(GameObject root, string tagName)
    {
        try
        {
            root.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{tagName}' is missing. Add it in Project Settings > Tags and Layers.");
        }
    }
}

