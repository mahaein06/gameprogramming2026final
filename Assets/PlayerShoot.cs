using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerShoot : MonoBehaviour
{
    public PlayerStatus playerStatus;
    public GameObject bulletPrefab;
    public Transform firePoint;

    private void Awake()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void Update()
    {
        if (!WasLeftClickPressed()) return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
        {
            return;
        }

        Fire();
    }

    private void Fire()
    {
        if (playerStatus == null)
        {
            Debug.LogWarning("PlayerShoot: Player Status is not assigned.", this);
            return;
        }

        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerShoot: Bullet Prefab is not assigned.", this);
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("PlayerShoot: Fire Point is not assigned.", this);
            return;
        }

        if (!playerStatus.UseAmmo()) return;

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
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
