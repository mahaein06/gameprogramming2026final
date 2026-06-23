using UnityEngine;

public class EnemyTerrainRoamer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.4f;
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float pointTolerance = 0.35f;
    [SerializeField] private Vector2 waitTimeRange = new Vector2(0.8f, 2.2f);
    [SerializeField] private bool lockYPosition = true;
    [SerializeField] private bool lockYRotation = true;

    private Vector3 origin;
    private Vector3 target;
    private float fixedY;
    private float fixedYRotation;
    private float waitUntil;
    private bool initialized;

    private void OnEnable()
    {
        CaptureStartPose();
        PickNewTarget();
    }

    private void Update()
    {
        if (!initialized)
        {
            CaptureStartPose();
        }

        if (Time.time < waitUntil)
        {
            ApplyLocks();
            return;
        }

        Vector3 current = transform.position;
        Vector3 flatCurrent = new Vector3(current.x, fixedY, current.z);
        Vector3 flatTarget = new Vector3(target.x, fixedY, target.z);

        if ((flatTarget - flatCurrent).sqrMagnitude <= pointTolerance * pointTolerance)
        {
            PickNewTarget();
            ApplyLocks();
            return;
        }

        Vector3 next = Vector3.MoveTowards(flatCurrent, flatTarget, moveSpeed * Time.deltaTime);
        if (lockYPosition)
        {
            next.y = fixedY;
        }

        transform.position = next;
        ApplyLocks();
    }

    public void CaptureStartPose()
    {
        origin = transform.position;
        fixedY = transform.position.y;
        fixedYRotation = transform.eulerAngles.y;
        initialized = true;
    }

    public void SetActiveRoaming(bool active)
    {
        enabled = active;
        if (active)
        {
            CaptureStartPose();
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        Vector2 random = Random.insideUnitCircle * Mathf.Max(0.1f, roamRadius);
        target = origin + new Vector3(random.x, 0f, random.y);
        ClampTargetToTerrain();
        target.y = fixedY;
        waitUntil = Time.time + Random.Range(Mathf.Min(waitTimeRange.x, waitTimeRange.y), Mathf.Max(waitTimeRange.x, waitTimeRange.y));
    }

    private void ClampTargetToTerrain()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null) return;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        target.x = Mathf.Clamp(target.x, terrainPosition.x, terrainPosition.x + terrainSize.x);
        target.z = Mathf.Clamp(target.z, terrainPosition.z, terrainPosition.z + terrainSize.z);
    }

    private void ApplyLocks()
    {
        if (lockYPosition)
        {
            Vector3 position = transform.position;
            if (!Mathf.Approximately(position.y, fixedY))
            {
                position.y = fixedY;
                transform.position = position;
            }
        }

        if (lockYRotation)
        {
            Vector3 euler = transform.eulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(euler.y, fixedYRotation)) > 0.001f)
            {
                euler.y = fixedYRotation;
                transform.eulerAngles = euler;
            }
        }
    }
}
