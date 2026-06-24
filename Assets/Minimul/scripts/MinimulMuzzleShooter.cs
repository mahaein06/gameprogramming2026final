using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MinimulMuzzleShooter : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float bulletSpeed = 30f;
    [SerializeField] private float bulletLifeTime = 5f;
    [SerializeField] private float muzzleOffset = 0.1f;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private bool fireOnInput;
    [SerializeField] private bool consumePlayerAmmo;
    [SerializeField] private PlayerStatus playerStatus;

    private Collider[] ownerColliders;
    private float nextFireTime;

    private void Awake()
    {
        ownerColliders = GetComponentsInChildren<Collider>(true);
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void Update()
    {
        if (!fireOnInput || Time.time < nextFireTime || !WasLeftClickPressed()) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking()) return;

        if (FirePlayerInput())
        {
            nextFireTime = Time.time + Mathf.Max(0.05f, fireRate);
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        fireOnInput = enabled;
    }

    public void BindPlayerStatus(PlayerStatus status)
    {
        playerStatus = status;
    }

    public bool Fire()
    {
        if (!CanSpawnBullet(false)) return false;

        SpawnBullet(false, 0);
        return true;
    }

    public bool FireEnemy(int damage)
    {
        if (!CanSpawnBullet(true)) return false;

        SpawnBullet(true, damage);
        return true;
    }

    private bool FirePlayerInput()
    {
        if (!CanSpawnBullet(false)) return false;
        if (consumePlayerAmmo && !TryConsumePlayerAmmo()) return false;

        SpawnBullet(false, 0);
        return true;
    }

    private bool TryConsumePlayerAmmo()
    {
        if (playerStatus == null)
        {
            Debug.LogWarning("MinimulMuzzleShooter: Player Status is not assigned.", this);
            return false;
        }

        return playerStatus.UseAmmo();
    }

    private bool CanSpawnBullet(bool enemyBulletRequired)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("MinimulMuzzleShooter: Bullet Prefab is not assigned.", this);
            return false;
        }

        if (muzzle == null)
        {
            Debug.LogWarning("MinimulMuzzleShooter: Muzzle is not assigned.", this);
            return false;
        }

        if (bulletPrefab.GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning("MinimulMuzzleShooter: Bullet Prefab needs a Rigidbody.", bulletPrefab);
            return false;
        }

        return true;
    }

    private void SpawnBullet(bool enemyBulletRequired, int damage)
    {
        Vector3 direction = muzzle.forward.sqrMagnitude > 0.0001f ? muzzle.forward.normalized : transform.forward;
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position + direction * muzzleOffset, muzzle.rotation);
        SetTagSafely(bullet, "Bullet");

        Rigidbody bulletRigidbody = bullet.GetComponent<Rigidbody>();
        bulletRigidbody.isKinematic = false;
        bulletRigidbody.useGravity = false;
        bulletRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bulletRigidbody.linearVelocity = direction * bulletSpeed;

        if (enemyBulletRequired)
        {
            EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
            if (enemyBullet == null)
            {
                enemyBullet = bullet.AddComponent<EnemyBullet>();
            }

            enemyBullet.damage = damage;
        }

        IgnoreOwnerCollisions(bullet.GetComponent<Collider>());
        Destroy(bullet, bulletLifeTime);
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
            Debug.LogWarning($"Tag '{tagName}' is missing. Add it in Project Settings > Tags and Layers.", target);
        }
    }

    private static bool WasLeftClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
