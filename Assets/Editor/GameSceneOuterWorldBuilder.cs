using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneOuterWorldBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene/GameScene.unity";
    private const string OuterTerrainDataPath = "Assets/Scenes/MainScene/GameScene_OuterTerrain.asset";
    private const string FogMaterialPath = "Assets/Materials/BoundaryFog.mat";

    private static readonly string[] TreePrefabPaths =
    {
        "Assets/Hand_Painted_Nature_Kit_LITE/Prefabs/Pine_Tree.prefab",
        "Assets/Hand_Painted_Nature_Kit_LITE/Prefabs/Larch_Tree.prefab",
        "Assets/Hand_Painted_Nature_Kit_LITE/Prefabs/Cedar_Tree_03.prefab"
    };

    public static void BuildOuterWorld()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Terrain innerTerrain = FindInnerTerrain();
        if (innerTerrain == null || innerTerrain.terrainData == null)
        {
            Debug.LogError("GameSceneOuterWorldBuilder: inner Terrain not found.");
            EditorApplication.Exit(1);
            return;
        }

        Terrain outerTerrain = CreateOrUpdateOuterTerrain(innerTerrain);
        PlantOuterTrees(innerTerrain, outerTerrain);
        CreateBoundaryFog(innerTerrain);
        CreateBoundaryWalls(innerTerrain);
        ConfigureRenderFog();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("GameSceneOuterWorldBuilder: outer terrain, trees, fog, and boundary walls updated.");
        EditorApplication.Exit(0);
    }

    private static Terrain FindInnerTerrain()
    {
        foreach (Terrain terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include))
        {
            if (terrain.name == "Terrain")
                return terrain;
        }

        return Object.FindAnyObjectByType<Terrain>();
    }

    private static Terrain CreateOrUpdateOuterTerrain(Terrain innerTerrain)
    {
        TerrainData innerData = innerTerrain.terrainData;
        Vector3 innerPos = innerTerrain.transform.position;
        Vector3 innerSize = innerData.size;
        Vector3 innerCenter = innerPos + new Vector3(innerSize.x * 0.5f, 0f, innerSize.z * 0.5f);
        Vector3 outerSize = new Vector3(innerSize.x * 4f, innerSize.y, innerSize.z * 4f);
        Vector3 outerPos = new Vector3(innerCenter.x - outerSize.x * 0.5f, innerPos.y - 0.08f, innerCenter.z - outerSize.z * 0.5f);

        TerrainData outerData = AssetDatabase.LoadAssetAtPath<TerrainData>(OuterTerrainDataPath);
        if (outerData == null)
        {
            outerData = new TerrainData();
            outerData.heightmapResolution = Mathf.Max(33, innerData.heightmapResolution);
            outerData.alphamapResolution = innerData.alphamapResolution;
            outerData.baseMapResolution = innerData.baseMapResolution;
            AssetDatabase.CreateAsset(outerData, OuterTerrainDataPath);
        }

        outerData.size = outerSize;
        outerData.terrainLayers = innerData.terrainLayers;
        outerData.detailPrototypes = innerData.detailPrototypes;
        outerData.treePrototypes = BuildTreePrototypes(innerData.treePrototypes);

        float[,] heights = new float[outerData.heightmapResolution, outerData.heightmapResolution];
        outerData.SetHeights(0, 0, heights);

        GameObject outerObject = GameObject.Find("OuterTerrain");
        if (outerObject == null)
        {
            outerObject = Terrain.CreateTerrainGameObject(outerData);
            outerObject.name = "OuterTerrain";
        }

        Terrain outerTerrain = outerObject.GetComponent<Terrain>();
        TerrainCollider collider = outerObject.GetComponent<TerrainCollider>();
        outerTerrain.terrainData = outerData;
        if (collider != null)
            collider.terrainData = outerData;

        outerTerrain.drawInstanced = innerTerrain.drawInstanced;
        outerTerrain.treeDistance = Mathf.Max(innerTerrain.treeDistance, 600f);
        outerTerrain.detailObjectDistance = innerTerrain.detailObjectDistance;
        outerTerrain.transform.position = outerPos;
        outerTerrain.groupingID = innerTerrain.groupingID;
        outerTerrain.allowAutoConnect = true;
        outerObject.isStatic = true;

        return outerTerrain;
    }

    private static TreePrototype[] BuildTreePrototypes(TreePrototype[] existing)
    {
        List<TreePrototype> prototypes = new List<TreePrototype>(existing ?? new TreePrototype[0]);
        foreach (string path in TreePrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            bool hasPrefab = false;
            foreach (TreePrototype prototype in prototypes)
            {
                if (prototype.prefab == prefab)
                {
                    hasPrefab = true;
                    break;
                }
            }

            if (!hasPrefab)
                prototypes.Add(new TreePrototype { prefab = prefab, bendFactor = 0.2f });
        }

        return prototypes.ToArray();
    }

    private static void PlantOuterTrees(Terrain innerTerrain, Terrain outerTerrain)
    {
        TerrainData outerData = outerTerrain.terrainData;
        Vector3 innerPos = innerTerrain.transform.position;
        Vector3 innerSize = innerTerrain.terrainData.size;
        Rect innerRect = new Rect(innerPos.x - 4f, innerPos.z - 4f, innerSize.x + 8f, innerSize.z + 8f);
        Vector3 outerPos = outerTerrain.transform.position;
        Vector3 outerSize = outerData.size;

        List<int> treePrototypeIndexes = new List<int>();
        for (int i = 0; i < outerData.treePrototypes.Length; i++)
        {
            GameObject prefab = outerData.treePrototypes[i].prefab;
            if (prefab == null)
                continue;

            string name = prefab.name.ToLowerInvariant();
            if (name.Contains("tree") || name.Contains("pine") || name.Contains("larch") || name.Contains("cedar"))
                treePrototypeIndexes.Add(i);
        }

        if (treePrototypeIndexes.Count == 0)
        {
            Debug.LogWarning("GameSceneOuterWorldBuilder: no tree prototypes found for outer terrain.");
            return;
        }

        List<TreeInstance> trees = new List<TreeInstance>();
        List<Vector2> occupied = new List<Vector2>();
        Random.InitState(20260624 + 99);
        int targetCount = 420;
        float minSpacing = 10f;
        int attempts = targetCount * 90;

        for (int i = 0; i < attempts && trees.Count < targetCount; i++)
        {
            float x = Random.Range(outerPos.x + 10f, outerPos.x + outerSize.x - 10f);
            float z = Random.Range(outerPos.z + 10f, outerPos.z + outerSize.z - 10f);
            if (innerRect.Contains(new Vector2(x, z)))
                continue;

            Vector2 point = new Vector2(x, z);
            if (IsTooClose(point, occupied, minSpacing))
                continue;

            float normalizedX = Mathf.InverseLerp(outerPos.x, outerPos.x + outerSize.x, x);
            float normalizedZ = Mathf.InverseLerp(outerPos.z, outerPos.z + outerSize.z, z);
            float heightScale = Random.Range(0.55f, 0.9f);
            int prototype = treePrototypeIndexes[trees.Count % treePrototypeIndexes.Count];
            trees.Add(new TreeInstance
            {
                position = new Vector3(normalizedX, 0f, normalizedZ),
                prototypeIndex = prototype,
                widthScale = heightScale,
                heightScale = heightScale,
                color = Color.white,
                lightmapColor = Color.white,
                rotation = Random.Range(0f, Mathf.PI * 2f)
            });
            occupied.Add(point);
        }

        outerData.treeInstances = trees.ToArray();
        outerTerrain.Flush();
        EditorUtility.SetDirty(outerData);
        Debug.Log($"GameSceneOuterWorldBuilder: planted {trees.Count} outer terrain trees.");
    }

    private static bool IsTooClose(Vector2 point, List<Vector2> occupied, float minSpacing)
    {
        float sqr = minSpacing * minSpacing;
        foreach (Vector2 other in occupied)
        {
            if ((point - other).sqrMagnitude < sqr)
                return true;
        }

        return false;
    }

    private static void CreateBoundaryFog(Terrain innerTerrain)
    {
        GameObject root = RecreateRoot("BoundaryFog");
        Material fogMaterial = GetOrCreateFogMaterial();
        Vector3 pos = innerTerrain.transform.position;
        Vector3 size = innerTerrain.terrainData.size;
        float centerX = pos.x + size.x * 0.5f;
        float centerZ = pos.z + size.z * 0.5f;
        float y = pos.y + 4.2f;
        float height = 8f;
        float thickness = 7f;

        CreateFogBand(root.transform, "NorthFog", new Vector3(centerX, y, pos.z + size.z), new Vector3(size.x + thickness * 2f, height, thickness), fogMaterial);
        CreateFogBand(root.transform, "SouthFog", new Vector3(centerX, y, pos.z), new Vector3(size.x + thickness * 2f, height, thickness), fogMaterial);
        CreateFogBand(root.transform, "EastFog", new Vector3(pos.x + size.x, y, centerZ), new Vector3(thickness, height, size.z + thickness * 2f), fogMaterial);
        CreateFogBand(root.transform, "WestFog", new Vector3(pos.x, y, centerZ), new Vector3(thickness, height, size.z + thickness * 2f), fogMaterial);
    }

    private static void CreateFogBand(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = name;
        band.transform.SetParent(parent, false);
        band.transform.position = position;
        band.transform.localScale = scale;
        Collider collider = band.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = band.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        band.isStatic = true;
    }

    private static void CreateBoundaryWalls(Terrain innerTerrain)
    {
        GameObject root = RecreateRoot("TerrainBoundaryWalls");
        Vector3 pos = innerTerrain.transform.position;
        Vector3 size = innerTerrain.terrainData.size;
        float centerX = pos.x + size.x * 0.5f;
        float centerZ = pos.z + size.z * 0.5f;
        float wallHeight = 24f;
        float wallThickness = 1f;
        float y = pos.y + wallHeight * 0.5f;

        CreateWall(root.transform, "NorthWall", new Vector3(centerX, y, pos.z + size.z), new Vector3(size.x, wallHeight, wallThickness));
        CreateWall(root.transform, "SouthWall", new Vector3(centerX, y, pos.z), new Vector3(size.x, wallHeight, wallThickness));
        CreateWall(root.transform, "EastWall", new Vector3(pos.x + size.x, y, centerZ), new Vector3(wallThickness, wallHeight, size.z));
        CreateWall(root.transform, "WestWall", new Vector3(pos.x, y, centerZ), new Vector3(wallThickness, wallHeight, size.z));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        wall.isStatic = true;
    }

    private static GameObject RecreateRoot(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null)
            Object.DestroyImmediate(old);

        return new GameObject(name);
    }

    private static Material GetOrCreateFogMaterial()
    {
        Directory.CreateDirectory("Assets/Materials");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, FogMaterialPath);
        }

        material.name = "BoundaryFog";
        material.SetColor("_BaseColor", new Color(0.72f, 0.78f, 0.82f, 0.38f));
        material.SetColor("_Color", new Color(0.72f, 0.78f, 0.82f, 0.38f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureRenderFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.74f, 0.78f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
    }
}

