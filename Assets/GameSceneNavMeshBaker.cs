using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class GameSceneNavMeshBaker : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField] private NavMeshSurface surface;
    [SerializeField] private Vector3 bakeVolumeCenter = Vector3.zero;
    [SerializeField] private Vector3 bakeVolumeSize = new Vector3(80f, 20f, 80f);
    [SerializeField] private bool buildOnStart = true;

    [Header("House Exclusion")]
    [SerializeField] private Transform house;
    [SerializeField] private Vector3 houseExclusionCenter = Vector3.zero;
    [SerializeField] private Vector3 houseExclusionSize = new Vector3(12f, 8f, 12f);

    private NavMeshModifierVolume houseBlocker;

    private void Awake()
    {
        EnsureSetup();
        if (Application.isPlaying && buildOnStart)
        {
            BuildNavMesh();
        }
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
        if (baker == null)
        {
            GameObject bakerObject = new GameObject("GameSceneNavMeshBaker");
            baker = bakerObject.AddComponent<GameSceneNavMeshBaker>();
        }

        baker.EnsureSetup();
        if (Application.isPlaying && baker.buildOnStart)
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
