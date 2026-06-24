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
    private const string FogTexturePath = "Assets/Materials/BoundaryFogNoise.png";

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
        Vector3 outerSize = new Vector3(innerSize.x * 2f, innerSize.y, innerSize.z * 2f);
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
        outerTerrain.treeDistance = Mathf.Max(innerTerrain.treeDistance, 450f);
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
        int targetCount = 240;
        float minSpacing = 7f;
        int attempts = targetCount * 100;

        for (int i = 0; i < attempts && trees.Count < targetCount; i++)
        {
            float x = Random.Range(outerPos.x + 8f, outerPos.x + outerSize.x - 8f);
            float z = Random.Range(outerPos.z + 8f, outerPos.z + outerSize.z - 8f);
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
        float minX = pos.x;
        float maxX = pos.x + size.x;
        float minZ = pos.z;
        float maxZ = pos.z + size.z;
        float yBase = pos.y + 3.2f;

        Random.InitState(20260624 + 404);
        CreateFogSide(root.transform, fogMaterial, "North", minX, maxX, maxZ - 3f, maxZ + 11f, yBase, 34, 0f);
        CreateFogSide(root.transform, fogMaterial, "South", minX, maxX, minZ - 11f, minZ + 3f, yBase, 34, 180f);
        CreateFogSide(root.transform, fogMaterial, "East", maxX - 3f, maxX + 11f, minZ, maxZ, yBase, 34, -90f);
        CreateFogSide(root.transform, fogMaterial, "West", minX - 11f, minX + 3f, minZ, maxZ, yBase, 34, 90f);
    }

    private static void CreateFogSide(Transform parent, Material material, string side, float minX, float maxX, float minZ, float maxZ, float yBase, int count, float baseYaw)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            float y = yBase + Random.Range(-0.8f, 3.4f);
            float width = Random.Range(8f, 20f);
            float height = Random.Range(3.5f, 9f);
            float yaw = baseYaw + Random.Range(-24f, 24f);

            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Quad);
            puff.name = side + "FogPuff_" + i.ToString("00");
            puff.transform.SetParent(parent, false);
            puff.transform.position = new Vector3(x, y, z);
            puff.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), yaw, Random.Range(-8f, 8f));
            puff.transform.localScale = new Vector3(width, height, 1f);
            Collider collider = puff.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            MeshRenderer renderer = puff.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            puff.isStatic = true;
        }
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
        Texture2D fogTexture = GetOrCreateFogTexture();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, FogMaterialPath);
        }

        Color fogColor = new Color(0.72f, 0.78f, 0.82f, 0.34f);
        material.name = "BoundaryFog";
        material.SetTexture("_BaseMap", fogTexture);
        material.SetTexture("_MainTex", fogTexture);
        material.SetColor("_BaseColor", fogColor);
        material.SetColor("_Color", fogColor);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D GetOrCreateFogTexture()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FogTexturePath);
        if (texture != null)
            return texture;

        const int size = 256;
        Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float radial = 1f - Mathf.Clamp01(Vector2.Distance(p, center) / (size * 0.48f));
                float n1 = Mathf.PerlinNoise(x * 0.025f, y * 0.025f);
                float n2 = Mathf.PerlinNoise(30f + x * 0.055f, 80f + y * 0.055f);
                float alpha = Mathf.Clamp01(Mathf.Pow(radial, 0.7f) * (0.35f + n1 * 0.45f + n2 * 0.2f));
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                generated.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        generated.Apply();
        File.WriteAllBytes(FogTexturePath, generated.EncodeToPNG());
        Object.DestroyImmediate(generated);
        AssetDatabase.ImportAsset(FogTexturePath);
        TextureImporter importer = AssetImporter.GetAtPath(FogTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(FogTexturePath);
    }

    private static void ConfigureRenderFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.74f, 0.78f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;
    }
}


