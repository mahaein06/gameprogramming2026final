using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneForestPainter
{
    private const string ScenePath = "Assets/Scenes/MainScene/GameScene.unity";

    private static readonly string[] TreePrefabPaths =
    {
        "Assets/Hand_Painted_Nature_Kit_LITE/Prefabs/Pine_Tree.prefab",
        "Assets/Hand_Painted_Nature_Kit_LITE/Prefabs/Larch_Tree.prefab",
        "Assets/Hand_Painted_Nature_Kit_LITE/Prefabs/Cedar_Tree_03.prefab"
    };

    private static readonly string[] AnimalNames = { "Deer", "Horse", "Pinguin", "Dog", "Tiger" };

    [MenuItem("Tools/GameScene/Replant Forest Trees")]
    public static void ReplantForestTreesMenu()
    {
        OpenGameSceneIfNeeded();
        ReplantForestTrees();
    }

    public static void ApplyDefaultForest()
    {
        OpenGameSceneIfNeeded();
        ReplantForestTrees();
        EditorApplication.Exit(0);
    }

    private static void OpenGameSceneIfNeeded()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    private static void ReplantForestTrees()
    {
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("GameSceneForestPainter: Terrain을 찾지 못했습니다.");
            return;
        }

        GameSceneForestSettings settings = GetOrCreateSettings();
        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Replant Forest Trees");

        int[] prototypeIndexes = EnsureTreePrototypes(terrain.terrainData);
        List<TreeInstance> preservedTrees = PreserveNonGeneratedTrees(terrain.terrainData, prototypeIndexes);
        List<Vector3> occupied = BuildBlockedPositions(settings);
        Bounds houseBounds = BuildHouseBounds(settings.houseAvoidRadius);

        Random.InitState(settings.seed);
        List<Vector2> clusterCenters = BuildClusterCenters(terrain, settings, houseBounds, occupied);
        List<TreeInstance> generated = new List<TreeInstance>();
        int attempts = Mathf.Max(settings.targetTreeCount * 80, 1200);

        for (int i = 0; i < attempts && generated.Count < settings.targetTreeCount; i++)
        {
            Vector3 world = PickCandidate(terrain, settings, clusterCenters);
            if (!IsInsideTerrain(terrain, settings, world))
                continue;

            if (houseBounds.Contains(world))
                continue;

            if (IsTooClose(world, occupied, settings.minTreeSpacing))
                continue;

            float heightScale = Random.Range(settings.minHeightScale, settings.maxHeightScale);
            if (IsNearAnyAnimal(world, occupied, settings.nearAnimalRadius))
                heightScale = Mathf.Min(heightScale, settings.nearAnimalMaxHeightScale);

            int prototypeIndex = prototypeIndexes[generated.Count % prototypeIndexes.Length];
            generated.Add(CreateTreeInstance(terrain, world, prototypeIndex, settings, heightScale));
            occupied.Add(world);
        }

        preservedTrees.AddRange(generated);
        terrain.terrainData.treeInstances = preservedTrees.ToArray();
        terrain.Flush();

        EditorUtility.SetDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log($"GameSceneForestPainter: {generated.Count} trees planted. Preserved {preservedTrees.Count - generated.Count} existing non-managed trees.");
    }

    private static GameSceneForestSettings GetOrCreateSettings()
    {
        GameObject settingsObject = GameObject.Find("ForestPlantingSettings");
        if (settingsObject == null)
        {
            settingsObject = new GameObject("ForestPlantingSettings");
        }

        GameSceneForestSettings settings = settingsObject.GetComponent<GameSceneForestSettings>();
        if (settings == null)
        {
            settings = settingsObject.AddComponent<GameSceneForestSettings>();
        }

        settings.seed = 20260624;
        settings.targetTreeCount = 130;
        settings.clusterCount = 7;
        settings.minTreeSpacing = 8f;
        settings.terrainEdgeMargin = 5f;
        settings.houseAvoidRadius = 11f;
        settings.animalStartAvoidRadius = 6f;
        settings.minHeightScale = 0.55f;
        settings.maxHeightScale = 0.9f;
        settings.minWidthScale = 0.55f;
        settings.maxWidthScale = 0.9f;
        settings.nearAnimalRadius = 22f;
        settings.nearAnimalMaxHeightScale = 0.7f;

        EditorUtility.SetDirty(settingsObject);
        return settings;
    }

    private static int[] EnsureTreePrototypes(TerrainData data)
    {
        List<TreePrototype> prototypes = new List<TreePrototype>(data.treePrototypes);
        int[] indexes = new int[TreePrefabPaths.Length];

        for (int i = 0; i < TreePrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPaths[i]);
            if (prefab == null)
            {
                Debug.LogError($"GameSceneForestPainter: tree prefab missing: {TreePrefabPaths[i]}");
                indexes[i] = 0;
                continue;
            }

            int existing = prototypes.FindIndex(p => p.prefab == prefab);
            if (existing < 0)
            {
                prototypes.Add(new TreePrototype { prefab = prefab, bendFactor = 0.2f });
                existing = prototypes.Count - 1;
            }

            indexes[i] = existing;
        }

        data.treePrototypes = prototypes.ToArray();
        return indexes;
    }

    private static List<TreeInstance> PreserveNonGeneratedTrees(TerrainData data, int[] generatedPrototypeIndexes)
    {
        HashSet<int> generatedIndexes = new HashSet<int>(generatedPrototypeIndexes);
        List<TreeInstance> preserved = new List<TreeInstance>();
        foreach (TreeInstance tree in data.treeInstances)
        {
            if (!generatedIndexes.Contains(tree.prototypeIndex))
                preserved.Add(tree);
        }

        return preserved;
    }

    private static List<Vector3> BuildBlockedPositions(GameSceneForestSettings settings)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (string animalName in AnimalNames)
        {
            GameObject animal = GameObject.Find(animalName);
            if (animal != null)
                positions.Add(animal.transform.position);
        }

        GameObject house = GameObject.Find("HouseFull") ?? GameObject.Find("House");
        if (house != null)
            positions.Add(house.transform.position);

        return positions;
    }

    private static Bounds BuildHouseBounds(float padding)
    {
        GameObject house = GameObject.Find("HouseFull") ?? GameObject.Find("House");
        if (house == null)
            return new Bounds(new Vector3(58.8f, 0f, 59.494f), new Vector3(padding * 2f, 100f, padding * 2f));

        Renderer[] renderers = house.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(house.transform.position, Vector3.zero);
        bool initialized = false;
        foreach (Renderer renderer in renderers)
        {
            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!initialized)
            bounds = new Bounds(house.transform.position, Vector3.zero);

        bounds.Expand(new Vector3(padding * 2f, 100f, padding * 2f));
        return bounds;
    }

    private static List<Vector2> BuildClusterCenters(Terrain terrain, GameSceneForestSettings settings, Bounds houseBounds, List<Vector3> blocked)
    {
        List<Vector2> centers = new List<Vector2>();
        int attempts = Mathf.Max(settings.clusterCount * 80, 300);
        for (int i = 0; i < attempts && centers.Count < settings.clusterCount; i++)
        {
            Vector3 candidate = PickUniformTerrainPoint(terrain, settings);
            if (houseBounds.Contains(candidate) || IsTooClose(candidate, blocked, settings.animalStartAvoidRadius))
                continue;

            centers.Add(new Vector2(candidate.x, candidate.z));
        }

        if (centers.Count == 0)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            centers.Add(new Vector2(terrainPosition.x + size.x * 0.5f, terrainPosition.z + size.z * 0.5f));
        }

        return centers;
    }

    private static Vector3 PickCandidate(Terrain terrain, GameSceneForestSettings settings, List<Vector2> clusterCenters)
    {
        if (clusterCenters.Count > 0 && Random.value < 0.72f)
        {
            Vector2 center = clusterCenters[Random.Range(0, clusterCenters.Count)];
            float radius = Random.Range(7f, 24f);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 candidate = new Vector3(center.x + Mathf.Cos(angle) * radius, 0f, center.y + Mathf.Sin(angle) * radius);
            candidate.y = TerrainHeightAt(terrain, candidate);
            return candidate;
        }

        return PickUniformTerrainPoint(terrain, settings);
    }

    private static Vector3 PickUniformTerrainPoint(Terrain terrain, GameSceneForestSettings settings)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float x = Random.Range(terrainPosition.x + settings.terrainEdgeMargin, terrainPosition.x + size.x - settings.terrainEdgeMargin);
        float z = Random.Range(terrainPosition.z + settings.terrainEdgeMargin, terrainPosition.z + size.z - settings.terrainEdgeMargin);
        return new Vector3(x, TerrainHeightAt(terrain, new Vector3(x, 0f, z)), z);
    }

    private static TreeInstance CreateTreeInstance(Terrain terrain, Vector3 world, int prototypeIndex, GameSceneForestSettings settings, float heightScale)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        Vector3 normalized = new Vector3(
            Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + size.x, world.x),
            Mathf.InverseLerp(terrainPosition.y, terrainPosition.y + size.y, world.y - terrainPosition.y),
            Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + size.z, world.z));

        return new TreeInstance
        {
            position = normalized,
            prototypeIndex = prototypeIndex,
            widthScale = heightScale,
            heightScale = heightScale,
            color = Color.white,
            lightmapColor = Color.white,
            rotation = Random.Range(0f, Mathf.PI * 2f)
        };
    }

    private static bool IsInsideTerrain(Terrain terrain, GameSceneForestSettings settings, Vector3 world)
    {
        Vector3 pos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return world.x >= pos.x + settings.terrainEdgeMargin &&
               world.x <= pos.x + size.x - settings.terrainEdgeMargin &&
               world.z >= pos.z + settings.terrainEdgeMargin &&
               world.z <= pos.z + size.z - settings.terrainEdgeMargin;
    }

    private static float TerrainHeightAt(Terrain terrain, Vector3 world)
    {
        return terrain.SampleHeight(world) + terrain.transform.position.y;
    }

    private static bool IsTooClose(Vector3 point, List<Vector3> occupied, float minDistance)
    {
        float minSqr = minDistance * minDistance;
        foreach (Vector3 other in occupied)
        {
            Vector2 a = new Vector2(point.x, point.z);
            Vector2 b = new Vector2(other.x, other.z);
            if ((a - b).sqrMagnitude < minSqr)
                return true;
        }

        return false;
    }

    private static bool IsNearAnyAnimal(Vector3 point, List<Vector3> occupied, float radius)
    {
        float radiusSqr = radius * radius;
        for (int i = 0; i < Mathf.Min(AnimalNames.Length, occupied.Count); i++)
        {
            Vector2 a = new Vector2(point.x, point.z);
            Vector2 b = new Vector2(occupied[i].x, occupied[i].z);
            if ((a - b).sqrMagnitude < radiusSqr)
                return true;
        }

        return false;
    }
}



