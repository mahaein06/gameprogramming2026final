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
        [SerializeField] private float m_MuzzleOffset = 0.35f;
        [SerializeField] private float m_BulletLifeTime = 5f;
        [SerializeField] private PlayerStatus m_PlayerStatus;
        private Collider m_OwnerCollider;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSceneDogShooter()
        {
            foreach (var mover in FindObjectsOfType<CreatureMover>())
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

            if (m_FireTransform == null)
            {
                var fireObject = GameObject.Find(m_FireObjectName);
                if (fireObject != null)
                {
                    m_FireTransform = fireObject.transform;
                }
            }

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
            if (WasFirePressed())
            {
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
                {
                    return;
                }

                Fire();
            }
        }

        private bool WasFirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private void EnsureFireTransform()
        {
            if (m_FireTransform != null) return;

            var fireObject = GameObject.Find(m_FireObjectName);
            if (fireObject != null)
            {
                m_FireTransform = fireObject.transform;
            }
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

            var fireRotation = m_FireTransform.rotation;
            var firePosition = m_FireTransform.position + m_FireTransform.forward * m_MuzzleOffset;
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

            bulletRigidbody.linearVelocity = m_FireTransform.forward * m_FirePower;
            Destroy(bullet, m_BulletLifeTime);
        }
    }

    public class BulletCollisionDestroyer : MonoBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
            Destroy(gameObject);
        }
    }
}



