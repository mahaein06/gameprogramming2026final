using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("Ranges")]
    [SerializeField] private float detectRange = 14f;
    [SerializeField] private float attackRange = 7f;

    [Header("Attack")]
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private int damage = 10;
    [SerializeField] private int startAmmo = 10;
    [SerializeField] private int reloadAmmo = 5;
    [SerializeField] private float reloadDelay = 30f;
    [SerializeField] private float bulletSpeed = 24f;
    [SerializeField] private float bulletLifeTime = 5f;
    [SerializeField] private float muzzleOffset = 0.25f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolPointTolerance = 1.2f;
    [SerializeField] private float randomPatrolRadius = 8f;

    [Header("Runtime")]
    [SerializeField] private EnemyState state = EnemyState.Patrol;

    private const string PlayerTag = "Player";
    private const string BulletTag = "Bullet";

    private static readonly string[] WeaponNameHints =
    {
        "RPGDONE",
        "SSG_Guns",
        "Gun",
        "Rifle",
        "Weapon",
        "Pistol"
    };

    private NavMeshAgent agent;
    private Transform player;
    private Collider[] ownerColliders;
    private int patrolIndex;
    private int currentAmmo;
    private float nextFireTime;
    private bool reloadScheduled;
    private Vector3 randomPatrolTarget;
    private bool hasRandomPatrolTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        ownerColliders = GetComponentsInChildren<Collider>(true);
        currentAmmo = Mathf.Max(0, startAmmo);
        EnsureBulletPrefab();
        EnsureFirePoint();
    }

    private void OnEnable()
    {
        currentAmmo = Mathf.Max(0, startAmmo);
        reloadScheduled = false;
        nextFireTime = 0f;
        hasRandomPatrolTarget = false;
        FindPlayerIfNeeded();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReloadPartial));
        reloadScheduled = false;
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        if (player == null)
        {
            SetState(EnemyState.Patrol);
            Patrol();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            SetState(EnemyState.Attack);
            Attack();
        }
        else if (distanceToPlayer <= detectRange)
        {
            SetState(EnemyState.Chase);
            Chase();
        }
        else
        {
            SetState(EnemyState.Patrol);
            Patrol();
        }
    }

    public void BindPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    private void SetState(EnemyState nextState)
    {
        state = nextState;
    }

    private void Patrol()
    {
        if (!CanUseAgent()) return;

        agent.isStopped = false;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform point = patrolPoints[Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1)];
            if (point == null)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                return;
            }

            agent.SetDestination(point.position);
            if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }

            return;
        }

        if (!hasRandomPatrolTarget || (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance))
        {
            hasRandomPatrolTarget = TryFindRandomPatrolTarget(out randomPatrolTarget);
        }

        if (hasRandomPatrolTarget)
        {
            agent.SetDestination(randomPatrolTarget);
        }
    }

    private void Chase()
    {
        if (!CanUseAgent() || player == null) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        StopAgent();
        FacePlayer();
        TryFire();
    }

    private void TryFire()
    {
        if (player == null || Time.time < nextFireTime) return;

        if (currentAmmo <= 0)
        {
            ScheduleReloadIfNeeded();
            return;
        }

        EnsureBulletPrefab();
        EnsureFirePoint();
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 fireDirection = GetFireDirection();
        Vector3 spawnPosition = firePoint.position + fireDirection * muzzleOffset;
        Quaternion spawnRotation = Quaternion.LookRotation(fireDirection, Vector3.up);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, spawnRotation);
        SetTagSafely(bullet, BulletTag);

        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet == null)
        {
            enemyBullet = bullet.AddComponent<EnemyBullet>();
        }
        enemyBullet.damage = damage;

        Rigidbody bulletRigidbody = bullet.GetComponent<Rigidbody>();
        if (bulletRigidbody == null)
        {
            bulletRigidbody = bullet.AddComponent<Rigidbody>();
        }

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider == null)
        {
            bulletCollider = bullet.AddComponent<SphereCollider>();
        }

        IgnoreOwnerCollisions(bulletCollider);
        bulletRigidbody.isKinematic = false;
        bulletRigidbody.useGravity = false;
        bulletRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bulletRigidbody.linearVelocity = fireDirection * bulletSpeed;

        currentAmmo--;
        nextFireTime = Time.time + Mathf.Max(0.05f, fireRate);
        if (currentAmmo <= 0)
        {
            ScheduleReloadIfNeeded();
        }

        Destroy(bullet, bulletLifeTime);
    }

    private void ScheduleReloadIfNeeded()
    {
        if (reloadScheduled) return;

        reloadScheduled = true;
        Invoke(nameof(ReloadPartial), reloadDelay);
    }

    private void ReloadPartial()
    {
        currentAmmo = Mathf.Max(0, reloadAmmo);
        reloadScheduled = false;
    }

    private Vector3 GetFireDirection()
    {
        Vector3 target = player != null ? player.position + Vector3.up * 1.1f : transform.position + transform.forward;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 540f * Time.deltaTime);
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void StopAgent()
    {
        if (!CanUseAgent()) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private bool TryFindRandomPatrolTarget(out Vector3 target)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomPatrolRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, randomPatrolRadius, NavMesh.AllAreas))
            {
                target = hit.position;
                return true;
            }
        }

        target = transform.position;
        return false;
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null && player.gameObject.activeInHierarchy) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
        player = playerObject != null ? playerObject.transform : null;
    }

    private void EnsureBulletPrefab()
    {
        if (bulletPrefab != null) return;

#if UNITY_EDITOR
        bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Marpa Studio/Built-In/Prefab/Bullet2.prefab");
        if (bulletPrefab == null)
        {
            bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Marpa Studio/Built-In/Prefab/Bullet.prefab");
        }
#endif
    }

    private void EnsureFirePoint()
    {
        if (firePoint != null) return;

        firePoint = FindWeaponCandidate();
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private Transform FindWeaponCandidate()
    {
        Transform best = null;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform) continue;
            if (!HasWeaponName(child.name)) continue;
            if (child.GetComponentInChildren<Renderer>(true) == null) continue;

            if (best == null || child.childCount > best.childCount)
            {
                best = child;
            }
        }

        return best;
    }

    private static bool HasWeaponName(string objectName)
    {
        foreach (string hint in WeaponNameHints)
        {
            if (objectName.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void IgnoreOwnerCollisions(Collider bulletCollider)
    {
        if (ownerColliders == null || bulletCollider == null) return;

        foreach (Collider ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(bulletCollider, ownerCollider);
            }
        }
    }

    private static void SetTagSafely(GameObject target, string tagName)
    {
        try
        {
            target.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{tagName}' is missing. Add it in Project Settings > Tags and Layers.");
        }
    }
}

