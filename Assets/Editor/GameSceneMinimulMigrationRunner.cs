#if UNITY_EDITOR
using System;
using ithappy.Animals_FREE;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSceneMinimulMigrationRunner
{
    private const string ScenePath = "Assets/Scenes/MainScene/GameScene.unity";

    [MenuItem("Tools/Minimul/Migrate GameScene")]
    public static void RunFromMenu()
    {
        Run();
    }

    private sealed class AnimalDef
    {
        public readonly SelectableAnimal Animal;
        public readonly string SceneName;
        public readonly string[] ExistingNames;
        public readonly string PrefabPath;
        public readonly string EnemyHpBarName;
        public readonly float CameraYawOffset;
        public readonly float AgentRadius;

        public AnimalDef(
            SelectableAnimal animal,
            string sceneName,
            string[] existingNames,
            string prefabPath,
            string enemyHpBarName,
            float cameraYawOffset,
            float agentRadius)
        {
            Animal = animal;
            SceneName = sceneName;
            ExistingNames = existingNames;
            PrefabPath = prefabPath;
            EnemyHpBarName = enemyHpBarName;
            CameraYawOffset = cameraYawOffset;
            AgentRadius = agentRadius;
        }
    }

    private static readonly AnimalDef[] Animals =
    {
        new AnimalDef(SelectableAnimal.Deer, "Deer", new[] { "Deer" }, "Assets/Minimul/prefebs/Deer.prefab", "Deer_EnemyHPBar", -90f, 0.33f),
        new AnimalDef(SelectableAnimal.Horse, "Horse", new[] { "Horse" }, "Assets/Minimul/prefebs/Horse.prefab", "Horse_EnemyHPBar", 60f, 0.36f),
        new AnimalDef(SelectableAnimal.Penguin, "Penguin", new[] { "Penguin", "Pinguin" }, "Assets/Minimul/prefebs/Pinguin.prefab", "Pinguin_EnemyHPBar", 0f, 0.26f),
        new AnimalDef(SelectableAnimal.Dog, "Dog", new[] { "Dog" }, "Assets/Minimul/prefebs/Dog.prefab", "Dog_EnemyHPBar", 180f, 0.29f),
        new AnimalDef(SelectableAnimal.Tiger, "Tiger", new[] { "Tiger" }, "Assets/Minimul/prefebs/Tiger.prefab", "Tiger_EnemyHPBar", 90f, 0.55f),
    };

    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            throw new InvalidOperationException($"Could not open scene: {ScenePath}");
        }

        Slider playerHpSlider = FindComponentOnSceneObject<Slider>("HPBar");
        TMP_Text playerAmmoText = FindComponentOnSceneObject<TMP_Text>("AmmoText");

        DisableOldAnimalCameras();

        GameObject[] roots = new GameObject[Animals.Length];
        for (int i = 0; i < Animals.Length; i++)
        {
            roots[i] = ReplaceAnimal(scene, Animals[i], playerHpSlider, playerAmmoText);
        }

        ThirdPersonCamera playerCamera = EnsureMainCamera(scene, roots[3]);
        MinimulAnimalControl control = EnsureControlObject(scene);
        ConfigureControl(control, playerCamera, playerHpSlider, playerAmmoText, roots);
        EnsureGameSceneNavMeshBaker();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("GameScene Minimul migration complete.");
    }

    private static GameObject ReplaceAnimal(Scene scene, AnimalDef def, Slider playerHpSlider, TMP_Text playerAmmoText)
    {
        GameObject oldRoot = null;
        foreach (string name in def.ExistingNames)
        {
            oldRoot ??= FindSceneObject(name);
        }

        Vector3 position = oldRoot != null ? oldRoot.transform.position : Vector3.zero;
        Quaternion rotation = oldRoot != null ? oldRoot.transform.rotation : Quaternion.identity;
        Vector3 scale = oldRoot != null ? oldRoot.transform.localScale : Vector3.one;

        DetachGameSceneUiRoots(oldRoot);
        foreach (string name in def.ExistingNames)
        {
            GameObject duplicate = FindSceneObject(name);
            if (duplicate != null)
            {
                UnityEngine.Object.DestroyImmediate(duplicate);
            }
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Missing Minimul prefab: {def.PrefabPath}");
        }

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        root.name = def.SceneName;
        root.transform.position = position;
        root.transform.rotation = rotation;
        root.transform.localScale = scale;
        SetTagSafely(root, "Enemy");

        CameraArmAim armAim = Require<CameraArmAim>(root, def.SceneName);
        CreatureMover mover = Require<CreatureMover>(root, def.SceneName);
        MovePlayerInput input = Require<MovePlayerInput>(root, def.SceneName);
        MinimulMuzzleShooter shooter = Require<MinimulMuzzleShooter>(root, def.SceneName);
        NavMeshAgent agent = EnsureComponent<NavMeshAgent>(root);
        MinimulNavMeshAnimalAI ai = EnsureComponent<MinimulNavMeshAnimalAI>(root);
        PlayerStatus playerStatus = EnsureComponent<PlayerStatus>(root);
        EnemyStatus enemyStatus = EnsureComponent<EnemyStatus>(root);

        ConfigureAgent(agent, def.AgentRadius);
        ConfigureShooter(shooter, playerStatus);
        ConfigureNavAi(ai, agent, mover, shooter);
        ConfigurePlayerStatus(playerStatus, playerHpSlider, playerAmmoText);
        ConfigureEnemyStatus(enemyStatus, def.EnemyHpBarName);

        armAim.enabled = true;
        mover.enabled = true;
        input.enabled = false;
        shooter.SetInputEnabled(false);
        ai.enabled = false;
        agent.enabled = false;
        playerStatus.enabled = false;
        enemyStatus.enabled = false;
        enemyStatus.SetEnemyHPBarVisible(false);

        return root;
    }

    private static ThirdPersonCamera EnsureMainCamera(Scene scene, GameObject defaultPlayer)
    {
        GameObject cameraObject = FindSceneObject("Main Camera");
        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
        }

        cameraObject.SetActive(true);
        SetTagSafely(cameraObject, "MainCamera");

        Camera camera = EnsureComponent<Camera>(cameraObject);
        camera.enabled = true;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;

        AudioListener listener = EnsureComponent<AudioListener>(cameraObject);
        listener.enabled = true;
        EnsureUniversalAdditionalCameraData(cameraObject);

        ThirdPersonCamera thirdPersonCamera = EnsureComponent<ThirdPersonCamera>(cameraObject);
        if (defaultPlayer != null)
        {
            cameraObject.transform.position = defaultPlayer.transform.position + new Vector3(0f, 2.5f, -8f);
            cameraObject.transform.rotation = Quaternion.LookRotation(defaultPlayer.transform.position + Vector3.up - cameraObject.transform.position, Vector3.up);
        }

        SerializedObject serializedCamera = new SerializedObject(thirdPersonCamera);
        serializedCamera.FindProperty("m_Player").objectReferenceValue = defaultPlayer != null ? defaultPlayer.transform : null;
        serializedCamera.FindProperty("m_SensitivityX").floatValue = 0.01f;
        serializedCamera.FindProperty("m_SensitivityY").floatValue = 0.01f;
        serializedCamera.FindProperty("m_Zoom").floatValue = 0.288f;
        serializedCamera.FindProperty("m_SensetivityZoom").floatValue = 0.1f;
        serializedCamera.FindProperty("m_MinAngle").floatValue = -20.4f;
        serializedCamera.FindProperty("m_MaxAngle").floatValue = 70f;
        serializedCamera.FindProperty("m_Offset").floatValue = 1.368f;
        serializedCamera.FindProperty("m_CameraSpeed").floatValue = 60f;
        serializedCamera.FindProperty("m_UseSceneCameraPose").boolValue = true;
        serializedCamera.ApplyModifiedPropertiesWithoutUndo();

        return thirdPersonCamera;
    }

    private static MinimulAnimalControl EnsureControlObject(Scene scene)
    {
        MinimulAnimalControl existing = UnityEngine.Object.FindAnyObjectByType<MinimulAnimalControl>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        GameObject controlObject = new GameObject("MinimulAnimalControl");
        SceneManager.MoveGameObjectToScene(controlObject, scene);
        return controlObject.AddComponent<MinimulAnimalControl>();
    }

    private static void ConfigureControl(
        MinimulAnimalControl control,
        ThirdPersonCamera playerCamera,
        Slider playerHpSlider,
        TMP_Text playerAmmoText,
        GameObject[] roots)
    {
        SerializedObject serializedControl = new SerializedObject(control);
        serializedControl.FindProperty("playerCamera").objectReferenceValue = playerCamera;
        serializedControl.FindProperty("playerHpSlider").objectReferenceValue = playerHpSlider;
        serializedControl.FindProperty("playerAmmoText").objectReferenceValue = playerAmmoText;
        serializedControl.FindProperty("useCharacterSelectionState").boolValue = true;
        serializedControl.FindProperty("selectedAnimal").enumValueIndex = (int)SelectableAnimal.Dog;

        SerializedProperty animals = serializedControl.FindProperty("animals");
        animals.arraySize = Animals.Length;
        for (int i = 0; i < Animals.Length; i++)
        {
            GameObject root = roots[i];
            SerializedProperty slot = animals.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("animal").enumValueIndex = (int)Animals[i].Animal;
            slot.FindPropertyRelative("root").objectReferenceValue = root;
            slot.FindPropertyRelative("armAim").objectReferenceValue = root.GetComponent<CameraArmAim>();
            slot.FindPropertyRelative("mover").objectReferenceValue = root.GetComponent<CreatureMover>();
            slot.FindPropertyRelative("input").objectReferenceValue = root.GetComponent<MovePlayerInput>();
            slot.FindPropertyRelative("shooter").objectReferenceValue = root.GetComponent<MinimulMuzzleShooter>();
            slot.FindPropertyRelative("ai").objectReferenceValue = root.GetComponent<MinimulNavMeshAnimalAI>();
            slot.FindPropertyRelative("navMeshAgent").objectReferenceValue = root.GetComponent<NavMeshAgent>();
            slot.FindPropertyRelative("playerStatus").objectReferenceValue = root.GetComponent<PlayerStatus>();
            slot.FindPropertyRelative("enemyStatus").objectReferenceValue = root.GetComponent<EnemyStatus>();
            slot.FindPropertyRelative("enemyHpSlider").objectReferenceValue = FindComponentOnSceneObject<Slider>(Animals[i].EnemyHpBarName);
            slot.FindPropertyRelative("enemyHpBarRoot").objectReferenceValue = FindSceneObject(Animals[i].EnemyHpBarName);
            slot.FindPropertyRelative("cameraYawOffset").floatValue = Animals[i].CameraYawOffset;
        }

        serializedControl.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(control);
    }

    private static void ConfigureAgent(NavMeshAgent agent, float radius)
    {
        agent.radius = radius;
        agent.speed = 3.5f;
        agent.acceleration = 12f;
        agent.angularSpeed = 540f;
        agent.stoppingDistance = 0.2f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private static void ConfigureShooter(MinimulMuzzleShooter shooter, PlayerStatus playerStatus)
    {
        SerializedObject serializedShooter = new SerializedObject(shooter);
        serializedShooter.FindProperty("fireOnInput").boolValue = false;
        serializedShooter.FindProperty("consumePlayerAmmo").boolValue = true;
        serializedShooter.FindProperty("playerStatus").objectReferenceValue = playerStatus;
        serializedShooter.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureNavAi(MinimulNavMeshAnimalAI ai, NavMeshAgent agent, CreatureMover mover, MinimulMuzzleShooter shooter)
    {
        SerializedObject serializedAi = new SerializedObject(ai);
        serializedAi.FindProperty("agent").objectReferenceValue = agent;
        serializedAi.FindProperty("mover").objectReferenceValue = mover;
        serializedAi.FindProperty("shooter").objectReferenceValue = shooter;
        serializedAi.FindProperty("target").objectReferenceValue = null;
        serializedAi.FindProperty("wanderRadius").floatValue = 8f;
        serializedAi.FindProperty("wanderPause").floatValue = 1f;
        serializedAi.FindProperty("detectRange").floatValue = 12f;
        serializedAi.FindProperty("attackRange").floatValue = 7f;
        serializedAi.FindProperty("turnSpeed").floatValue = 540f;
        serializedAi.FindProperty("fireRate").floatValue = 1f;
        serializedAi.FindProperty("damage").intValue = 10;
        serializedAi.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePlayerStatus(PlayerStatus status, Slider hpSlider, TMP_Text ammoText)
    {
        status.hpSlider = hpSlider;
        status.ammoText = ammoText;
        EditorUtility.SetDirty(status);
    }

    private static void ConfigureEnemyStatus(EnemyStatus status, string hpBarName)
    {
        SerializedObject serializedStatus = new SerializedObject(status);
        serializedStatus.FindProperty("hpSlider").objectReferenceValue = FindComponentOnSceneObject<Slider>(hpBarName);
        serializedStatus.FindProperty("hpBarRoot").objectReferenceValue = FindSceneObject(hpBarName);
        serializedStatus.FindProperty("useManualHPBarPosition").boolValue = true;
        serializedStatus.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DisableOldAnimalCameras()
    {
        string[] names = { "DogCam", "DeerCam", "HorseCam", "PenguinCam", "PinguinCam", "TigerCam" };
        foreach (string name in names)
        {
            GameObject cameraObject = FindSceneObject(name);
            if (cameraObject == null) continue;

            foreach (Camera camera in cameraObject.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }

            foreach (AudioListener listener in cameraObject.GetComponentsInChildren<AudioListener>(true))
            {
                listener.enabled = false;
            }

            foreach (ThirdPersonCamera thirdPersonCamera in cameraObject.GetComponentsInChildren<ThirdPersonCamera>(true))
            {
                thirdPersonCamera.enabled = false;
            }

            SetTagSafely(cameraObject, "Untagged");
            cameraObject.SetActive(false);
        }
    }

    private static void DetachGameSceneUiRoots(GameObject oldRoot)
    {
        if (oldRoot == null) return;

        string[] names =
        {
            "HPBar",
            "AmmoText",
            "EnemyCountText",
            "Dog_EnemyHPBar",
            "Deer_EnemyHPBar",
            "Horse_EnemyHPBar",
            "Pinguin_EnemyHPBar",
            "Tiger_EnemyHPBar",
        };

        foreach (string name in names)
        {
            GameObject target = FindSceneObject(name);
            if (target != null && target.transform.IsChildOf(oldRoot.transform))
            {
                target.transform.SetParent(null, true);
            }
        }
    }

    private static void EnsureGameSceneNavMeshBaker()
    {
        GameSceneNavMeshBaker baker = GameSceneNavMeshBaker.EnsureInScene();
        if (baker != null)
        {
            EditorUtility.SetDirty(baker);
            NavMeshSurface surface = baker.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                EditorUtility.SetDirty(surface);
            }
        }
    }

    private static T Require<T>(GameObject root, string ownerName) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException($"{ownerName} Minimul prefab is missing {typeof(T).Name}.");
        }

        return component;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void EnsureUniversalAdditionalCameraData(GameObject cameraObject)
    {
        Type type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (type != null && cameraObject.GetComponent(type) == null)
        {
            cameraObject.AddComponent(type);
        }
    }

    private static T FindComponentOnSceneObject<T>(string objectName) where T : Component
    {
        GameObject target = FindSceneObject(objectName);
        return target != null ? target.GetComponent<T>() ?? target.GetComponentInChildren<T>(true) : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name != objectName) continue;
            if (!gameObject.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(gameObject)) continue;
            return gameObject;
        }

        return null;
    }

    private static void SetTagSafely(GameObject target, string tagName)
    {
        try
        {
            target.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{tagName}' is missing. Add it in Project Settings > Tags and Layers.", target);
        }
    }
}
#endif
