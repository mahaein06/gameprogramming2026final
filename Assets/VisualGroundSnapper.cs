using UnityEngine;

public class VisualGroundSnapper : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private float raycastHeight = 20f;
    [SerializeField] private float raycastDistance = 80f;
    [SerializeField] private bool snapEveryLateUpdate = true;

    private void Awake()
    {
        ResolveVisualRoot();
    }

    private void Start()
    {
        SnapToGround();
    }

    private void LateUpdate()
    {
        if (snapEveryLateUpdate)
        {
            SnapToGround();
        }
    }

    public void Configure(Transform targetVisualRoot = null, float offset = 0.02f)
    {
        visualRoot = targetVisualRoot != null ? targetVisualRoot : FindVisualRoot();
        groundOffset = offset;
        SnapToGround();
    }

    public void SnapToGround()
    {
        ResolveVisualRoot();
        if (visualRoot == null) return;
        if (!TryGetVisualBounds(out Bounds visualBounds)) return;
        if (!TryGetGroundY(out float groundY)) return;

        float deltaY = groundY + groundOffset - visualBounds.min.y;
        if (Mathf.Abs(deltaY) < 0.001f) return;

        visualRoot.position += Vector3.up * deltaY;
    }

    private void ResolveVisualRoot()
    {
        if (visualRoot == null || !visualRoot.IsChildOf(transform))
        {
            visualRoot = FindVisualRoot();
        }
    }

    private Transform FindVisualRoot()
    {
        foreach (Transform child in transform)
        {
            if (ShouldIgnoreTransform(child)) continue;
            if (child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                return child;
            }
        }

        SkinnedMeshRenderer renderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer == null) return null;

        Transform current = renderer.transform;
        while (current.parent != null && current.parent != transform)
        {
            current = current.parent;
        }

        return current == transform ? renderer.transform : current;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (SkinnedMeshRenderer renderer in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (ShouldIgnoreRenderer(renderer)) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private bool TryGetGroundY(out float groundY)
    {
        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, raycastHeight + raycastDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;

            groundY = hit.point.y;
            return true;
        }

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            Vector3 position = transform.position;
            groundY = terrain.SampleHeight(position) + terrain.transform.position.y;
            return true;
        }

        groundY = transform.position.y;
        return true;
    }

    private static bool ShouldIgnoreTransform(Transform candidate)
    {
        string lowerName = candidate.name.ToLowerInvariant();
        return lowerName.Contains("cam")
            || lowerName.Contains("camera")
            || lowerName.Contains("hpbar")
            || lowerName.Contains("enemyhp")
            || lowerName.Contains("gun")
            || lowerName.Contains("weapon");
    }

    private static bool ShouldIgnoreRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled) return true;
        if (renderer.GetComponentInParent<Canvas>() != null) return true;

        string lowerName = renderer.name.ToLowerInvariant();
        return lowerName.Contains("hpbar")
            || lowerName.Contains("enemyhp")
            || lowerName.Contains("gun")
            || lowerName.Contains("weapon");
    }
}
