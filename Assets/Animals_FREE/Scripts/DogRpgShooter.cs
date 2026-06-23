using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ithappy.Animals_FREE
{
    public class DogRpgShooter : MonoBehaviour
    {
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private Transform m_FireTransform;
        [SerializeField] private string m_FireObjectName = "RPGDONE";
        [SerializeField] private float m_FirePower = 30f;
        [SerializeField] private float m_MuzzleOffset = 0.12f;
        [SerializeField] private float m_BulletLifeTime = 5f;
        [SerializeField] private PlayerStatus m_PlayerStatus;
        [SerializeField] private Camera m_AimCamera;
        [SerializeField] private bool m_MatchFireTransformToBulletAngle = true;
        public Transform firePosition;

        private Collider[] m_OwnerColliders;
        private Quaternion m_FireRotationOffset = Quaternion.identity;
        private bool m_HasFireRotationOffset;

        private static readonly string[] WeaponNameHints =
        {
            "deergun",
            "horsegun",
            "penguingun",
            "tigergun",
            "doggun",
            "RPGDONE",
            "SSG_Guns",
            "Gun",
            "Rifle",
            "Weapon",
            "Pistol"
        };

        private void Awake()
        {
            CacheOwnerColliders();
            EnsurePlayerStatus();
            EnsureBulletPrefab();
            EnsureFireTransform();
            CacheFireRotationOffset();
        }

        private void LateUpdate()
        {
            AlignFireTransformToBulletAngle();
        }

        public void ConfigureForPlayer(PlayerStatus playerStatus, Camera aimCamera = null)
        {
            m_PlayerStatus = playerStatus;
            m_AimCamera = aimCamera != null ? aimCamera : Camera.main;
            CacheOwnerColliders();
            EnsureBulletPrefab();
            EnsureFireTransform();
            ResetFireRotationOffset();
        }

        private static GameObject LoadBulletPrefab(bool useBullet2)
        {
#if UNITY_EDITOR
            string path = useBullet2
                ? "Assets/Marpa Studio/Built-In/Prefab/Bullet2.prefab"
                : "Assets/Marpa Studio/Built-In/Prefab/Bullet.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        private void Update()
        {
            if (!WasFirePressed()) return;

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
            {
                return;
            }

            Fire();
        }

        private static bool WasFirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool pressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            pressed = pressed || Input.GetKeyDown(KeyCode.Space);
#endif
            return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private void CacheOwnerColliders()
        {
            m_OwnerColliders = GetComponentsInChildren<Collider>(true);
        }

        private void EnsurePlayerStatus()
        {
            if (m_PlayerStatus != null) return;

            m_PlayerStatus = GetComponent<PlayerStatus>();
            if (m_PlayerStatus != null) return;

            m_PlayerStatus = FindAnyObjectByType<PlayerStatus>();
            if (m_PlayerStatus != null) return;

            m_PlayerStatus = gameObject.AddComponent<PlayerStatus>();
        }

        private void EnsureBulletPrefab()
        {
            bool useBullet2 = ShouldUseBullet2Prefab();
            GameObject desiredPrefab = LoadBulletPrefab(useBullet2);

            if (desiredPrefab != null)
            {
                m_BulletPrefab = desiredPrefab;
                return;
            }

            if (m_BulletPrefab == null)
            {
                Debug.LogWarning($"Bullet prefab was not found for {name}. Check Assets/Marpa Studio/Built-In/Prefab.");
            }
        }

        private bool ShouldUseBullet2Prefab()
        {
            return !string.Equals(gameObject.name, "Dog", System.StringComparison.OrdinalIgnoreCase)
                && !gameObject.name.StartsWith("Dog ", System.StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureFireTransform()
        {
            if (firePosition != null)
            {
                m_FireTransform = firePosition;
                return;
            }

            if (m_FireTransform != null && m_FireTransform.IsChildOf(transform)) return;

            m_FireTransform = FindNamedChild(m_FireObjectName);
            if (m_FireTransform != null) return;

            m_FireTransform = FindWeaponCandidate();
            if (m_FireTransform != null) return;

            m_FireTransform = FindNearestSceneWeaponCandidate();
            if (m_FireTransform != null) return;

            m_FireTransform = transform;
        }

        private Transform FindNamedChild(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return null;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == targetName || child.name.StartsWith(targetName + " "))
                {
                    return child;
                }
            }

            return null;
        }

        private Transform FindWeaponCandidate()
        {
            Transform best = null;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform) continue;
                if (!HasWeaponName(child.name)) continue;
                if (child.GetComponentInChildren<Renderer>(true) == null) continue;

                if (best == null || IsBetterWeaponCandidate(child, best))
                {
                    best = child;
                }
            }

            return best;
        }

        private Transform FindNearestSceneWeaponCandidate()
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            Vector3 ownerPosition = transform.position;

            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (candidate == transform || candidate.IsChildOf(transform)) continue;
                if (!HasWeaponName(candidate.name)) continue;
                if (candidate.GetComponentInChildren<Renderer>(true) == null) continue;

                float distance = (candidate.position - ownerPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
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

        private static bool IsBetterWeaponCandidate(Transform candidate, Transform currentBest)
        {
            bool candidateIsTrigger = candidate.name.IndexOf("Trigger", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool currentIsTrigger = currentBest.name.IndexOf("Trigger", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (candidateIsTrigger != currentIsTrigger)
            {
                return !candidateIsTrigger;
            }

            return candidate.childCount > currentBest.childCount;
        }

        private void AlignFireTransformToBulletAngle()
        {
            if (!m_MatchFireTransformToBulletAngle) return;

            EnsureFireTransform();
            CacheFireRotationOffset();

            if (m_FireTransform == null || !m_HasFireRotationOffset) return;

            Vector3 fireDirection = GetFireDirection();
            if (fireDirection.sqrMagnitude < 0.0001f) return;

            Quaternion bulletAngle = Quaternion.LookRotation(fireDirection, Vector3.up);
            m_FireTransform.rotation = bulletAngle * m_FireRotationOffset;
        }

        private void CacheFireRotationOffset()
        {
            if (m_HasFireRotationOffset || m_FireTransform == null) return;

            Vector3 fireDirection = GetFireDirection();
            if (fireDirection.sqrMagnitude < 0.0001f)
            {
                fireDirection = transform.forward;
            }

            Quaternion bulletAngle = Quaternion.LookRotation(fireDirection, Vector3.up);
            m_FireRotationOffset = Quaternion.Inverse(bulletAngle) * m_FireTransform.rotation;
            m_HasFireRotationOffset = true;
        }

        private void ResetFireRotationOffset()
        {
            m_HasFireRotationOffset = false;
            CacheFireRotationOffset();
        }

        private void Fire()
        {
            EnsureBulletPrefab();
            EnsureFireTransform();

            if (m_BulletPrefab == null || m_FireTransform == null) return;

            EnsurePlayerStatus();
            if (m_PlayerStatus == null || !m_PlayerStatus.UseAmmo())
            {
                return;
            }

            Vector3 fireDirection = GetFireDirection();
            Vector3 muzzlePosition = GetMuzzlePosition(fireDirection);
            Quaternion fireRotation = Quaternion.LookRotation(fireDirection, Vector3.up);
            var bullet = Instantiate(m_BulletPrefab, muzzlePosition, fireRotation);
            SetTagSafely(bullet, "Bullet");

            if (!bullet.TryGetComponent<Rigidbody>(out var bulletRigidbody))
            {
                bulletRigidbody = bullet.AddComponent<Rigidbody>();
            }

            if (!bullet.TryGetComponent<Collider>(out var bulletCollider))
            {
                bulletCollider = bullet.AddComponent<SphereCollider>();
            }

            IgnoreOwnerCollisions(bulletCollider);

            if (!bullet.TryGetComponent<BulletCollisionDestroyer>(out _))
            {
                bullet.AddComponent<BulletCollisionDestroyer>();
            }

            bulletRigidbody.isKinematic = false;
            bulletRigidbody.useGravity = false;
            bulletRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bulletRigidbody.linearVelocity = fireDirection * m_FirePower;
            Destroy(bullet, m_BulletLifeTime);
        }

        private void IgnoreOwnerCollisions(Collider bulletCollider)
        {
            if (m_OwnerColliders == null || bulletCollider == null) return;

            foreach (Collider ownerCollider in m_OwnerColliders)
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

        private Vector3 GetFireDirection()
        {
            Camera aimCamera = m_AimCamera != null && m_AimCamera.isActiveAndEnabled ? m_AimCamera : Camera.main;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
            }

            return direction.normalized;
        }

        private Vector3 GetMuzzlePosition(Vector3 fireDirection)
        {
            if (TryGetFireBounds(out Bounds bounds))
            {
                float distanceToFront = GetProjectedExtent(bounds.extents, fireDirection);
                return bounds.center + fireDirection * (distanceToFront + m_MuzzleOffset);
            }

            return m_FireTransform.position + fireDirection * m_MuzzleOffset;
        }

        private bool TryGetFireBounds(out Bounds bounds)
        {
            Renderer[] renderers = m_FireTransform.GetComponentsInChildren<Renderer>(true);
            bounds = default;

            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
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

        private static float GetProjectedExtent(Vector3 extents, Vector3 direction)
        {
            Vector3 absDirection = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            return Vector3.Dot(extents, absDirection);
        }
    }

    public class BulletCollisionDestroyer : MonoBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
        }
    }
}


