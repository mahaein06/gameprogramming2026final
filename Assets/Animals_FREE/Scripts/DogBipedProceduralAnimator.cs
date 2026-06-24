using UnityEngine;

namespace ithappy.Animals_FREE
{
    [DisallowMultipleComponent]
    public class DogBipedProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private float m_WalkSwing = 18f;
        [SerializeField] private float m_RunSwing = 30f;
        [SerializeField] private float m_WalkSpeed = 5f;
        [SerializeField] private float m_RunSpeed = 8f;
        [SerializeField] private float m_IdleSwing = 2f;
        [SerializeField] private float m_BreathingSpeed = 1.2f;
        [SerializeField] private float m_NeckBreathingAngle = 6f;
        [SerializeField] private float m_HeadBreathingAngle = 4f;
        [SerializeField] private float m_MotionSmoothness = 8f;

        private CreatureMover m_Mover;
        private float m_MoveBlend;
        private float m_WalkCycle;

        private Bone m_RightLeg;
        private Bone m_LeftLeg;
        private Bone m_RightArm;
        private Bone m_LeftArm;
        private Bone m_Spine;
        private Bone m_Neck;
        private Bone m_Head;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSceneDogSetup()
        {
            foreach (var mover in FindObjectsOfType<CreatureMover>())
            {
                var dog = mover.gameObject;
                if (dog.transform.Find("Dog_001_rig") == null) continue;

                if (dog.TryGetComponent<Animator>(out var animator))
                {
                    animator.enabled = false;
                }

                if (dog.TryGetComponent<CharacterController>(out var controller))
                {
                    controller.enabled = true;
                }

                if (!dog.TryGetComponent<DogBipedProceduralAnimator>(out _))
                {
                    //dog.AddComponent<DogBipedProceduralAnimator>();
                }
            }
        }

        private void Awake()
        {
            m_Mover = GetComponent<CreatureMover>();

            m_RightLeg = new Bone(transform.Find("Dog_001_rig/Root/spine.004/shoulder.R"));
            m_LeftLeg = new Bone(transform.Find("Dog_001_rig/Root/spine.004/shoulder.L"));
            m_RightArm = new Bone(transform.Find("Dog_001_rig/Root/spine.004/spine.005/spine.006/front_shoulder.R"));
            m_LeftArm = new Bone(transform.Find("Dog_001_rig/Root/spine.004/spine.005/spine.006/front_shoulder.L"));
            m_Spine = new Bone(transform.Find("Dog_001_rig/Root/spine.004"));
            m_Neck = new Bone(transform.Find("Dog_001_rig/Root/spine.004/spine.005/spine.006/spine.007/spine.008"));
            m_Head = new Bone(transform.Find("Dog_001_rig/Root/spine.004/spine.005/spine.006/spine.007/spine.008/scull"));
        }

        private void LateUpdate()
        {
            var axis = m_Mover == null ? Vector2.zero : m_Mover.Axis;
            var targetMoveBlend = axis.sqrMagnitude > 0.01f ? 1f : 0f;
            var run = m_Mover != null && m_Mover.IsRun;
            m_MoveBlend = Mathf.MoveTowards(m_MoveBlend, targetMoveBlend, m_MotionSmoothness * Time.deltaTime);

            var speed = run ? m_RunSpeed : m_WalkSpeed;
            var movingSwing = run ? m_RunSwing : m_WalkSwing;
            var swing = Mathf.Lerp(m_IdleSwing, movingSwing, m_MoveBlend);
            m_WalkCycle += speed * Time.deltaTime * Mathf.Lerp(0.25f, 1f, m_MoveBlend);

            var phase = Mathf.Sin(m_WalkCycle) * swing;
            var counterPhase = -phase;
            var breath = Mathf.Sin(Time.time * m_BreathingSpeed);
            var moving = m_MoveBlend > 0.05f;

            if (!moving)
            {
                m_RightLeg.ApplyX(phase * 0.15f);
                m_LeftLeg.ApplyX(counterPhase * 0.15f);
                m_RightArm.Reset();
                m_LeftArm.Reset();
                m_Spine.ApplyZ(breath * 2.5f);
                m_Neck.ApplyX(breath * m_NeckBreathingAngle);
                m_Head.ApplyX(-breath * m_HeadBreathingAngle);
                return;
            }

            var direction = axis.y < -0.1f ? -1f : 1f;
            phase *= direction;
            counterPhase *= direction;

            m_RightLeg.ApplyX(phase);
            m_LeftLeg.ApplyX(counterPhase);
            m_RightArm.Reset();
            m_LeftArm.Reset();
            m_Spine.ApplyZ(Mathf.Lerp(breath * 1.2f, phase * 0.04f, m_MoveBlend));
            m_Neck.ApplyX(breath * m_NeckBreathingAngle * 0.25f);
            m_Head.ApplyX(Mathf.Abs(phase) * 0.025f - breath * m_HeadBreathingAngle * 0.2f);
        }

        private readonly struct Bone
        {
            private readonly Transform m_Transform;
            private readonly Quaternion m_BaseRotation;

            public Bone(Transform transform)
            {
                m_Transform = transform;
                m_BaseRotation = transform == null ? Quaternion.identity : transform.localRotation;
            }

            public void ApplyX(float degrees)
            {
                if (m_Transform == null) return;
                m_Transform.localRotation = m_BaseRotation * Quaternion.Euler(degrees, 0f, 0f);
            }

            public void ApplyZ(float degrees)
            {
                if (m_Transform == null) return;
                m_Transform.localRotation = m_BaseRotation * Quaternion.Euler(0f, 0f, degrees);
            }

            public void Reset()
            {
                if (m_Transform == null) return;
                m_Transform.localRotation = m_BaseRotation;
            }
        }
    }
}
