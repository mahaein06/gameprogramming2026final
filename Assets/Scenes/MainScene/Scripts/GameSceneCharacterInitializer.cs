using ithappy.Animals_FREE;
using UnityEngine;
using UnityEngine.AI;
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

        GameSceneNavMeshBaker.EnsureInScene();
        DisableDuplicateAnimalRoots();

        var selected = CharacterSelectionState.SelectedAnimal;
        var selectedRoot = FindAnimalRoot(selected);
        var selectedCamera = ConfigureCameras(selected, selectedRoot);
        int enemyCount = 0;

        foreach (SelectableAnimal animal in System.Enum.GetValues(typeof(SelectableAnimal)))
        {
            var root = FindAnimalRoot(animal);
            if (root == null) continue;

            bool isPlayer = animal == selected;
            SetTagSafely(root, isPlayer ? PlayerTag : EnemyTag);
            ConfigureHorseFrontLegStabilizer(root, animal);
            ConfigureInput(root, selectedCamera, isPlayer);
            PlayerStatus status = ConfigurePlayerStatus(root, isPlayer);
            ConfigureShooter(root, status, selectedCamera, isPlayer);
            ConfigureDialogueManager(status, isPlayer);
            if (ConfigureEnemyStatus(root, isPlayer))
            {
                enemyCount++;
            }
            ConfigureEnemyAI(root, selectedRoot, isPlayer);
        }

        EnemyKillTracker.InitializeForScene(enemyCount);

        if (selectedCamera != null && selectedRoot != null)
        {
            selectedCamera.BindPlayer(selectedRoot.transform);
            selectedCamera.ReinitializeFromCurrentTransform();
        }
    }

    private static void DisableDuplicateAnimalRoots()
    {
        foreach (SelectableAnimal animal in System.Enum.GetValues(typeof(SelectableAnimal)))
        {
            GameObject[] roots = FindAnimalRoots(animal);
            if (roots.Length <= 1) continue;

            GameObject keep = roots[0];
            foreach (GameObject root in roots)
            {
                if (root.name == GetSceneObjectName(animal))
                {
                    keep = root;
                    break;
                }
            }

            foreach (GameObject root in roots)
            {
                if (root != null && root != keep)
                {
                    root.SetActive(false);
                    Debug.LogWarning($"Disabled duplicate {animal} object: {root.name}", root);
                }
            }
        }
    }

    private static GameObject[] FindAnimalRoots(SelectableAnimal animal)
    {
        string primaryName = GetSceneObjectName(animal);
        System.Collections.Generic.List<GameObject> roots = new System.Collections.Generic.List<GameObject>();

        foreach (var mover in FindObjectsByType<CreatureMover>(FindObjectsInactive.Include))
        {
            if (IsAnimalRootName(mover.name, primaryName) || (animal == SelectableAnimal.Penguin && IsAnimalRootName(mover.name, "Penguin")))
            {
                if (!roots.Contains(mover.gameObject))
                {
                    roots.Add(mover.gameObject);
                }
            }
        }

        GameObject exact = GameObject.Find(primaryName);
        if (exact != null && exact.GetComponent<CreatureMover>() != null && !roots.Contains(exact))
        {
            roots.Insert(0, exact);
        }

        return roots.ToArray();
    }

    private static bool IsAnimalRootName(string objectName, string primaryName)
    {
        return objectName == primaryName
            || objectName.StartsWith(primaryName + " ")
            || objectName.StartsWith(primaryName + "_")
            || objectName.StartsWith(primaryName + "(");
    }
    private static ThirdPersonCamera ConfigureCameras(SelectableAnimal selected, GameObject selectedRoot)
    {
        var cameras = FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        ThirdPersonCamera selectedCamera = FindNamedCamera(cameras, selected) ?? FindNearestCamera(cameras, selectedRoot);

        foreach (var cameraRig in cameras)
        {
            bool active = cameraRig == selectedCamera;
            cameraRig.gameObject.SetActive(active);

            var unityCamera = cameraRig.GetComponent<Camera>();
            if (unityCamera != null)
            {
                unityCamera.enabled = active;
                if (active)
                {
                    unityCamera.tag = "MainCamera";
                }
            }

            var audioListener = cameraRig.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = active;
            }
        }

        return selectedCamera;
    }

    private static ThirdPersonCamera FindNamedCamera(ThirdPersonCamera[] cameras, SelectableAnimal animal)
    {
        string[] names = GetCameraNames(animal);
        foreach (string cameraName in names)
        {
            foreach (var cameraRig in cameras)
            {
                if (cameraRig.name == cameraName)
                {
                    return cameraRig;
                }
            }
        }

        return null;
    }

    private static string[] GetCameraNames(SelectableAnimal animal)
    {
        switch (animal)
        {
            case SelectableAnimal.Deer:
                return new[] { "DeerCam" };
            case SelectableAnimal.Horse:
                return new[] { "HorseCam" };
            case SelectableAnimal.Penguin:
                return new[] { "PenguinCam", "PinguinCam" };
            case SelectableAnimal.Dog:
                return new[] { "DogCam" };
            case SelectableAnimal.Tiger:
                return new[] { "TigerCam" };
            default:
                return new[] { "DogCam" };
        }
    }

    private static ThirdPersonCamera FindNearestCamera(ThirdPersonCamera[] cameras, GameObject selectedRoot)
    {
        if (selectedRoot == null || cameras.Length == 0) return cameras.Length > 0 ? cameras[0] : null;

        ThirdPersonCamera best = null;
        float bestDistance = float.MaxValue;
        Vector3 rootPosition = selectedRoot.transform.position;

        foreach (var cameraRig in cameras)
        {
            float distance = (cameraRig.transform.position - rootPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = cameraRig;
            }
        }

        return best;
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


    private static void ConfigureHorseFrontLegStabilizer(GameObject root, SelectableAnimal animal)
    {
        if (animal != SelectableAnimal.Horse || root == null) return;

        if (root.GetComponent<HorseFrontLegStabilizer>() == null)
        {
            root.AddComponent<HorseFrontLegStabilizer>();
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

    private static void ConfigureShooter(GameObject root, PlayerStatus status, ThirdPersonCamera selectedCamera, bool isPlayer)
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
            Camera aimCamera = selectedCamera != null ? selectedCamera.GetComponent<Camera>() : Camera.main;
            shooter.ConfigureForPlayer(status ?? root.GetComponent<PlayerStatus>() ?? root.AddComponent<PlayerStatus>(), aimCamera);
        }
    }

    private static PlayerStatus ConfigurePlayerStatus(GameObject root, bool isPlayer)
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

        return status;
    }

    private static void ConfigureDialogueManager(PlayerStatus status, bool isPlayer)
    {
        if (!isPlayer || status == null) return;

        DialogueManager dialogueManager = DialogueManager.Instance ?? FindAnyObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.BindPlayerStatus(status);
        }
    }

    private static bool ConfigureEnemyStatus(GameObject root, bool isPlayer)
    {
        var enemyStatus = root.GetComponent<EnemyStatus>();

        if (isPlayer)
        {
            if (enemyStatus != null)
            {
                enemyStatus.enabled = false;
                enemyStatus.SetEnemyHPBarVisible(false);
            }

            return false;
        }

        if (enemyStatus == null)
        {
            enemyStatus = root.AddComponent<EnemyStatus>();
        }

        enemyStatus.enabled = true;
        enemyStatus.EnsureEditableHPBar();
        enemyStatus.ResetEnemy();
        return true;
    }
    private static void ConfigureEnemyAI(GameObject root, GameObject playerRoot, bool isPlayer)
    {
        var ai = root.GetComponent<EnemyAI>();
        var agent = root.GetComponent<NavMeshAgent>();

        if (isPlayer)
        {
            if (ai != null)
            {
                ai.enabled = false;
            }

            if (agent != null)
            {
                agent.enabled = false;
            }

            return;
        }

        if (agent == null)
        {
            agent = root.AddComponent<NavMeshAgent>();
        }

        agent.enabled = true;
        agent.speed = Mathf.Max(agent.speed, 3.5f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);
        agent.acceleration = Mathf.Max(agent.acceleration, 12f);
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 2.5f);

        if (ai == null)
        {
            ai = root.AddComponent<EnemyAI>();
        }

        ai.enabled = true;
        if (playerRoot != null)
        {
            ai.BindPlayer(playerRoot.transform);
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










