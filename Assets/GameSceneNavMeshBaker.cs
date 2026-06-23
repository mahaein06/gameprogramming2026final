using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class GameSceneNavMeshBaker : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField] private NavMeshSurface surface;
    [SerializeField] private Vector3 bakeVolumeCenter = Vector3.zero;
    [SerializeField] private Vector3 bakeVolumeSize = new Vector3(200f, 40f, 260f);
    [SerializeField] private bool autoFitBakeVolume = true;
    [SerializeField] private float bakeVolumePadding = 20f;
    [SerializeField] private bool buildOnStart = false;

    [Header("House Exclusion")]
    [SerializeField] private Transform house;
    [SerializeField] private Vector3 houseExclusionCenter = Vector3.zero;
    [SerializeField] private Vector3 houseExclusionSize = new Vector3(12f, 8f, 12f);

    private const int RuntimeNavMeshLayer = 30;
    private const string RuntimeFloorName = "Runtime_NavMesh_Floor";

    private NavMeshModifierVolume houseBlocker;
    private BoxCollider runtimeFloorCollider;

    private void Awake()
    {
        EnsureSetup();
    }

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void OnValidate()
    {
        EnsureSetup();
    }

    [ContextMenu("Build GameScene NavMesh")]
    public void BuildNavMesh()
    {
        EnsureSetup();
        if (surface == null) return;

        SetRuntimeFloorEnabled(true);
        try
        {
            surface.BuildNavMesh();
        }
        finally
        {
            SetRuntimeFloorEnabled(false);
        }
    }
    public static GameSceneNavMeshBaker EnsureRuntimeNavMesh()
    {
        GameSceneNavMeshBaker baker = FindAnyObjectByType<GameSceneNavMeshBaker>(FindObjectsInactive.Include);
        if (baker == null)
        {
            GameObject bakerObject = new GameObject("GameSceneNavMeshBaker");
            baker = bakerObject.AddComponent<GameSceneNavMeshBaker>();
        }

        baker.EnsureSetup();
        if (!baker.HasUsableNavMesh())
        {
            baker.BuildNavMesh();
        }

        return baker;
    }

    public static GameSceneNavMeshBaker EnsureInScene()
    {
        GameSceneNavMeshBaker baker = FindAnyObjectByType<GameSceneNavMeshBaker>(FindObjectsInactive.Include);
        if (Application.isPlaying)
        {
            return baker;
        }

        if (baker == null)
        {
            GameObject bakerObject = new GameObject("GameSceneNavMeshBaker");
            baker = bakerObject.AddComponent<GameSceneNavMeshBaker>();
        }

        baker.EnsureSetup();
        if (baker.buildOnStart)
        {
            baker.BuildNavMesh();
        }

        return baker;
    }

    private bool HasUsableNavMesh()
    {
        string[] sampleObjectNames = { "Deer", "Horse", "Pinguin", "Penguin", "Dog", "Tiger" };
        foreach (string objectName in sampleObjectNames)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null && NavMesh.SamplePosition(target.transform.position, out _, 12f, NavMesh.AllAreas))
            {
                return true;
            }
        }

        foreach (TerrainCollider terrainCollider in FindObjectsByType<TerrainCollider>(FindObjectsInactive.Exclude))
        {
            float sampleDistance = Mathf.Max(terrainCollider.bounds.extents.x, terrainCollider.bounds.extents.z);
            if (NavMesh.SamplePosition(terrainCollider.bounds.center, out _, sampleDistance, NavMesh.AllAreas))
            {
                return true;
            }
        }

        return false;
    }
    private void EnsureSetup()
    {
        if (surface == null)
        {
            surface = GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = gameObject.AddComponent<NavMeshSurface>();
            }
        }

        UpdateBakeVolumeFromScene();
        EnsureRuntimeFloor();

        surface.collectObjects = CollectObjects.Volume;
        surface.center = bakeVolumeCenter;
        surface.size = bakeVolumeSize;
        surface.layerMask = 1 << RuntimeNavMeshLayer;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;

        if (house == null)
        {
            GameObject houseObject = GameObject.Find("House");
            if (houseObject != null)
            {
                house = houseObject.transform;
            }
        }

        EnsureHouseBlocker();
    }

    private void UpdateBakeVolumeFromScene()
    {
        if (!autoFitBakeVolume) return;

        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (TerrainCollider terrainCollider in FindObjectsByType<TerrainCollider>(FindObjectsInactive.Exclude))
        {
            EncapsulateBounds(ref bounds, ref hasBounds, terrainCollider.bounds);
        }

        string[] importantNames = { "Deer", "Horse", "Pinguin", "Penguin", "Dog", "Tiger", "House", "HouseFull" };
        foreach (string objectName in importantNames)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null) continue;

            EncapsulatePoint(ref bounds, ref hasBounds, target.transform.position);
        }

        if (!hasBounds) return;

        bounds.Expand(new Vector3(bakeVolumePadding, 0f, bakeVolumePadding));
        bakeVolumeCenter = transform.InverseTransformPoint(bounds.center);
        bakeVolumeSize = new Vector3(
            Mathf.Max(bounds.size.x, 80f),
            Mathf.Max(bounds.size.y + 20f, 40f),
            Mathf.Max(bounds.size.z, 80f));
    }

    private static void EncapsulateBounds(ref Bounds bounds, ref bool hasBounds, Bounds value)
    {
        if (!hasBounds)
        {
            bounds = value;
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(value);
    }

    private static void EncapsulatePoint(ref Bounds bounds, ref bool hasBounds, Vector3 point)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.one);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }
    private void EnsureRuntimeFloor()
    {
        Bounds terrainBounds;
        if (!TryGetTerrainBounds(out terrainBounds))
        {
            terrainBounds = new Bounds(transform.TransformPoint(bakeVolumeCenter), bakeVolumeSize);
        }

        Transform floorTransform = transform.Find(RuntimeFloorName);
        if (floorTransform == null)
        {
            GameObject floorObject = new GameObject(RuntimeFloorName);
            floorObject.transform.SetParent(transform, false);
            floorTransform = floorObject.transform;
        }

        floorTransform.gameObject.layer = RuntimeNavMeshLayer;
        floorTransform.position = terrainBounds.center;
        floorTransform.rotation = Quaternion.identity;
        floorTransform.localScale = Vector3.one;
        floorTransform.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

        runtimeFloorCollider = floorTransform.GetComponent<BoxCollider>();
        if (runtimeFloorCollider == null)
        {
            runtimeFloorCollider = floorTransform.gameObject.AddComponent<BoxCollider>();
        }

        runtimeFloorCollider.isTrigger = false;
        runtimeFloorCollider.enabled = false;
        runtimeFloorCollider.center = Vector3.zero;
        runtimeFloorCollider.size = new Vector3(
            Mathf.Max(terrainBounds.size.x, 1f),
            Mathf.Max(terrainBounds.size.y + 0.25f, 0.25f),
            Mathf.Max(terrainBounds.size.z, 1f));
    }

    private static bool TryGetTerrainBounds(out Bounds terrainBounds)
    {
        bool hasBounds = false;
        terrainBounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (TerrainCollider terrainCollider in FindObjectsByType<TerrainCollider>(FindObjectsInactive.Exclude))
        {
            EncapsulateBounds(ref terrainBounds, ref hasBounds, terrainCollider.bounds);
        }

        return hasBounds;
    }
    private void SetRuntimeFloorEnabled(bool enabled)
    {
        if (runtimeFloorCollider != null)
        {
            runtimeFloorCollider.enabled = enabled;
        }
    }
    private void EnsureHouseBlocker()
    {
        if (house == null) return;

        Transform blockerTransform = house.Find("House_NavMesh_NotWalkable_Area");
        if (blockerTransform == null)
        {
            GameObject blockerObject = new GameObject("House_NavMesh_NotWalkable_Area");
            blockerObject.transform.SetParent(house, false);
            blockerTransform = blockerObject.transform;
        }

        blockerTransform.localPosition = houseExclusionCenter;
        blockerTransform.localRotation = Quaternion.identity;
        blockerTransform.localScale = Vector3.one;
        blockerTransform.gameObject.layer = RuntimeNavMeshLayer;

        houseBlocker = blockerTransform.GetComponent<NavMeshModifierVolume>();
        if (houseBlocker == null)
        {
            houseBlocker = blockerTransform.gameObject.AddComponent<NavMeshModifierVolume>();
        }

        houseBlocker.area = 1;
        houseBlocker.center = Vector3.zero;
        houseBlocker.size = houseExclusionSize;
    }
}









