using UnityEngine;

public class EnemyTerrainRoamer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.4f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float pointTolerance = 0.35f;
    [SerializeField] private float moveAngleThreshold = 20f;
    [SerializeField] private Vector2 waitTimeRange = new Vector2(0.8f, 2.2f);
    [SerializeField] private bool lockYPosition = true;
    [SerializeField] private bool lockXRotation = true;
    [SerializeField] private bool lockZRotation = true;
    [SerializeField] private string verticalParameter = "Vert";
    [SerializeField] private string stateParameter = "State";
    [SerializeField] private float animationDampTime = 0.12f;

    private Vector3 origin;
    private Vector3 target;
    private float fixedY;
    private float fixedXRotation;
    private float fixedZRotation;
    private float waitUntil;
    private bool initialized;
    private bool isRoaming;
    private Animator animator;

    public bool IsRoaming => enabled && isRoaming;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        CaptureStartPose();
        PickNewTarget();
    }

    private void OnDisable()
    {
        SetWalkAnimation(false);
    }

    private void Update()
    {
        if (!initialized)
        {
            CaptureStartPose();
        }

        if (Time.time < waitUntil)
        {
            isRoaming = false;
            SetWalkAnimation(false);
            ApplyLocks();
            return;
        }

        Vector3 current = transform.position;
        Vector3 flatCurrent = new Vector3(current.x, fixedY, current.z);
        Vector3 flatTarget = new Vector3(target.x, fixedY, target.z);
        Vector3 toTarget = flatTarget - flatCurrent;

        if (toTarget.sqrMagnitude <= pointTolerance * pointTolerance)
        {
            isRoaming = false;
            SetWalkAnimation(false);
            PickNewTarget();
            ApplyLocks();
            return;
        }

        RotateToward(toTarget);

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float angleToTarget = Vector3.Angle(flatForward, toTarget.normalized);
        bool canMoveForward = angleToTarget <= moveAngleThreshold;

        if (canMoveForward)
        {
            Vector3 step = flatForward * moveSpeed * Time.deltaTime;
            if (step.sqrMagnitude > toTarget.sqrMagnitude)
            {
                step = toTarget;
            }

            Vector3 next = flatCurrent + step;
            if (lockYPosition)
            {
                next.y = fixedY;
            }

            transform.position = next;
        }

        isRoaming = true;
        SetWalkAnimation(true);
        ApplyLocks();
    }

    public void CaptureStartPose()
    {
        origin = transform.position;
        fixedY = transform.position.y;
        fixedXRotation = transform.eulerAngles.x;
        fixedZRotation = transform.eulerAngles.z;
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
        else
        {
            isRoaming = false;
            SetWalkAnimation(false);
        }
    }

    private void RotateToward(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 targetEuler = targetRotation.eulerAngles;
        targetEuler.x = lockXRotation ? fixedXRotation : currentEuler.x;
        targetEuler.z = lockZRotation ? fixedZRotation : currentEuler.z;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(targetEuler), rotateSpeed * Time.deltaTime);
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

        Vector3 euler = transform.eulerAngles;
        bool changed = false;

        if (lockXRotation && Mathf.Abs(Mathf.DeltaAngle(euler.x, fixedXRotation)) > 0.001f)
        {
            euler.x = fixedXRotation;
            changed = true;
        }

        if (lockZRotation && Mathf.Abs(Mathf.DeltaAngle(euler.z, fixedZRotation)) > 0.001f)
        {
            euler.z = fixedZRotation;
            changed = true;
        }

        if (changed)
        {
            transform.eulerAngles = euler;
        }
    }

    private void SetWalkAnimation(bool moving)
    {
        if (animator == null || !animator.enabled || !animator.gameObject.activeInHierarchy) return;

        float vertical = moving ? 1f : 0f;
        animator.SetFloat(verticalParameter, vertical, animationDampTime, Time.deltaTime);
        animator.SetFloat(stateParameter, 0f, animationDampTime, Time.deltaTime);
    }
}
