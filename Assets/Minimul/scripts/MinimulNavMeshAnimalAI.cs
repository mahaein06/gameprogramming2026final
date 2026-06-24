using ithappy.Animals_FREE;
using UnityEngine;
using UnityEngine.AI;

public class MinimulNavMeshAnimalAI : MonoBehaviour
{
    private enum AnimalAIState
    {
        Wander,
        Chase,
        Attack
    }

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CreatureMover mover;
    [SerializeField] private MinimulMuzzleShooter shooter;
    [SerializeField] private Transform target;
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float wanderPause = 1f;
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private int damage = 10;

    [SerializeField] private AnimalAIState state = AnimalAIState.Wander;

    private const float PathRefreshInterval = 0.2f;
    private const float NavMeshSampleDistance = 4f;
    private const float DestinationTolerance = 0.5f;

    private Vector3 wanderDestination;
    private bool hasWanderDestination;
    private float waitUntil;
    private float nextPathRefreshTime;
    private float nextFireTime;

    public string CurrentState => state.ToString();

    private void Awake()
    {
        ResolveComponents();
        ConfigureAgentForPathOnly();
    }

    private void OnEnable()
    {
        ResolveComponents();
        ConfigureAgentForPathOnly();
        hasWanderDestination = false;
        waitUntil = 0f;
        nextPathRefreshTime = 0f;
        nextFireTime = 0f;
    }

    private void OnDisable()
    {
        StopMoving();
        if (CanUseAgent())
        {
            agent.ResetPath();
        }
    }

    private void Update()
    {
        ResolveComponents();
        ConfigureAgentForPathOnly();

        if (mover == null || !CanUseAgent())
        {
            StopMoving();
            return;
        }

        agent.nextPosition = transform.position;

        if (target != null)
        {
            float targetDistance = FlatDistance(transform.position, GetTargetPoint());
            if (targetDistance <= attackRange)
            {
                Attack();
                return;
            }

            if (targetDistance <= detectRange)
            {
                Chase();
                return;
            }
        }

        Wander();
    }

    public void BindTarget(Transform targetTransform)
    {
        target = targetTransform;
        hasWanderDestination = false;
        nextPathRefreshTime = 0f;
    }

    private void ResolveComponents()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (mover == null)
        {
            mover = GetComponent<CreatureMover>();
        }

        if (shooter == null)
        {
            shooter = GetComponent<MinimulMuzzleShooter>();
        }
    }

    private void ConfigureAgentForPathOnly()
    {
        if (agent == null) return;

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = Mathf.Max(agent.speed, 3.5f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, turnSpeed);
        agent.acceleration = Mathf.Max(agent.acceleration, 12f);
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 0.2f);
    }

    private void Wander()
    {
        state = AnimalAIState.Wander;

        if (ReachedDestination())
        {
            StopMoving();
            if (waitUntil <= 0f)
            {
                waitUntil = Time.time + Mathf.Max(0f, wanderPause);
            }

            if (Time.time < waitUntil)
            {
                return;
            }

            hasWanderDestination = false;
            waitUntil = 0f;
        }

        if (!hasWanderDestination)
        {
            TryPickWanderDestination();
        }

        MoveAlongPath(false);
    }

    private void Chase()
    {
        state = AnimalAIState.Chase;
        hasWanderDestination = false;
        waitUntil = 0f;

        if (Time.time >= nextPathRefreshTime)
        {
            agent.SetDestination(GetTargetPoint());
            nextPathRefreshTime = Time.time + PathRefreshInterval;
        }

        MoveAlongPath(true);
    }

    private void Attack()
    {
        state = AnimalAIState.Attack;
        hasWanderDestination = false;
        waitUntil = 0f;

        if (CanUseAgent())
        {
            agent.ResetPath();
        }

        StopMoving();
        FacePoint(GetTargetPoint());

        if (shooter != null && Time.time >= nextFireTime)
        {
            shooter.FireEnemy(damage);
            nextFireTime = Time.time + Mathf.Max(0.05f, fireRate);
        }
    }

    private bool TryPickWanderDestination()
    {
        Vector3 origin = transform.position;
        for (int i = 0; i < 24; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * Mathf.Max(0f, wanderRadius);
            Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas)) continue;

            wanderDestination = hit.position;
            hasWanderDestination = agent.SetDestination(wanderDestination);
            return hasWanderDestination;
        }

        return false;
    }

    private void MoveAlongPath(bool isRun)
    {
        if (!agent.hasPath || agent.pathPending)
        {
            StopMoving();
            return;
        }

        Vector3 direction = agent.steeringTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.04f)
        {
            StopMoving();
            return;
        }

        FaceDirection(direction);

        Vector3 localDirection = transform.InverseTransformDirection(direction.normalized);
        Vector2 axis = Vector2.ClampMagnitude(new Vector2(localDirection.x, localDirection.z), 1f);
        mover.SetInput(axis, transform.position + transform.forward, isRun, false);
        agent.nextPosition = transform.position;
    }

    private void StopMoving()
    {
        if (mover != null)
        {
            mover.SetInput(Vector2.zero, transform.position + transform.forward, false, false);
        }
    }

    private bool ReachedDestination()
    {
        if (!hasWanderDestination) return true;
        if (agent.pathPending) return false;

        return FlatDistance(transform.position, wanderDestination) <= DestinationTolerance
            || agent.remainingDistance <= DestinationTolerance;
    }

    private bool CanUseAgent()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        return agent.Warp(hit.position) && agent.isOnNavMesh;
    }

    private Vector3 GetTargetPoint()
    {
        if (target == null) return transform.position + transform.forward;
        if (target.TryGetComponent(out CharacterController controller))
        {
            return controller.transform.TransformPoint(controller.center);
        }

        return target.position;
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Vector3 currentEuler = transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(currentEuler.x, lookRotation.eulerAngles.y, currentEuler.z);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
