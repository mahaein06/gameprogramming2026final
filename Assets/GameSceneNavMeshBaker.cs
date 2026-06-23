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

    private NavMeshModifierVolume houseBlocker;

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
        if (surface != null)
        {
            surface.BuildNavMesh();
        }
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

        surface.collectObjects = CollectObjects.Volume;
        surface.center = bakeVolumeCenter;
        surface.size = bakeVolumeSize;
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



