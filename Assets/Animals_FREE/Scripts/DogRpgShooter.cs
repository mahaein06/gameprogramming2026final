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
        [SerializeField] private bool m_MatchFireTransformToCamera = true;
        [SerializeField, Range(-70f, 0f)] private float m_MinAimPitch = -45f;
        [SerializeField, Range(0f, 70f)] private float m_MaxAimPitch = 45f;
        public Transform firePosition;
        private Collider m_OwnerCollider;
        private Quaternion m_StartFireLocalRotation = Quaternion.identity;
        private bool m_HasStartFireLocalRotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSceneDogShooter()
        {
            foreach (var mover in FindObjectsByType<CreatureMover>())
            {
                var dog = mover.gameObject;
                if (dog.transform.Find("Dog_001_rig") == null) continue;

                if (!dog.TryGetComponent<DogRpgShooter>(out _))
                {
                    dog.AddComponent<DogRpgShooter>();
                }
            }
        }

        private void Awake()
        {
            m_OwnerCollider = GetComponent<Collider>();

            if (m_PlayerStatus == null)
            {
                m_PlayerStatus = GetComponent<PlayerStatus>();
            }

            if (m_PlayerStatus == null)
            {
                m_PlayerStatus = FindAnyObjectByType<PlayerStatus>();
            }

            if (m_BulletPrefab == null)
            {
                m_BulletPrefab = LoadBulletPrefab();
            }

            EnsureFireTransform();
            CacheStartFireLocalRotation();
        }

        private void LateUpdate()
        {
            AlignFireTransformToCamera();
        }

        private static GameObject LoadBulletPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Marpa Studio/Built-in/Prefab/Bullet.prefab");
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

        private void EnsureFireTransform()
        {
            if (m_FireTransform != null) return;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == m_FireObjectName)
                {
                    m_FireTransform = child;
                    return;
                }
            }

            var fireObject = GameObject.Find(m_FireObjectName);
            if (fireObject != null)
            {
                m_FireTransform = fireObject.transform;
                return;
            }

            if (firePosition != null)
            {
                m_FireTransform = firePosition;
            }
        }

        private void AlignFireTransformToCamera()
        {
            if (!m_MatchFireTransformToCamera) return;

            EnsureFireTransform();
            CacheStartFireLocalRotation();

            if (m_FireTransform == null || !m_HasStartFireLocalRotation) return;

            float pitch = GetCameraPitch();
            m_FireTransform.localRotation = m_StartFireLocalRotation * Quaternion.Euler(pitch, 0f, 0f);
        }

        private void CacheStartFireLocalRotation()
        {
            if (m_HasStartFireLocalRotation || m_FireTransform == null) return;

            m_StartFireLocalRotation = m_FireTransform.localRotation;
            m_HasStartFireLocalRotation = true;
        }

        private float GetCameraPitch()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return 0f;
            }

            Vector3 localCameraForward = transform.InverseTransformDirection(mainCamera.transform.forward);
            float horizontalMagnitude = new Vector2(localCameraForward.x, localCameraForward.z).magnitude;
            float pitch = Mathf.Atan2(localCameraForward.y, Mathf.Max(horizontalMagnitude, 0.0001f)) * Mathf.Rad2Deg;
            return Mathf.Clamp(pitch, m_MinAimPitch, m_MaxAimPitch);
        }

        private void Fire()
        {
            if (m_BulletPrefab == null)
            {
                m_BulletPrefab = LoadBulletPrefab();
            }

            EnsureFireTransform();

            if (m_BulletPrefab == null || m_FireTransform == null) return;

            if (m_PlayerStatus == null)
            {
                m_PlayerStatus = FindAnyObjectByType<PlayerStatus>();
            }

            if (m_PlayerStatus != null && !m_PlayerStatus.UseAmmo())
            {
                return;
            }

            Vector3 fireDirection = GetFireDirection();
            Vector3 firePosition = GetMuzzlePosition(fireDirection);
            Quaternion fireRotation = Quaternion.LookRotation(fireDirection, Vector3.up);
            var bullet = Instantiate(m_BulletPrefab, firePosition, fireRotation);

            if (!bullet.TryGetComponent<Rigidbody>(out var bulletRigidbody))
            {
                bulletRigidbody = bullet.AddComponent<Rigidbody>();
            }

            if (!bullet.TryGetComponent<Collider>(out var bulletCollider))
            {
                bulletCollider = bullet.AddComponent<SphereCollider>();
            }

            if (m_OwnerCollider != null)
            {
                Physics.IgnoreCollision(bulletCollider, m_OwnerCollider);
            }

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

        private Vector3 GetFireDirection()
        {
            Camera mainCamera = Camera.main;
            Vector3 direction = mainCamera != null ? mainCamera.transform.forward : transform.forward;

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
            //Destroy(gameObject);
        }
    }
}