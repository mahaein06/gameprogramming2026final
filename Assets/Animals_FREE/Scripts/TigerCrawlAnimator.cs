using UnityEngine;

namespace ithappy.Animals_FREE
{
    [DisallowMultipleComponent]
    public class TigerCrawlAnimator : MonoBehaviour
    {
        [SerializeField] private float m_WalkCycleSpeed = 4f;
        [SerializeField] private float m_RunCycleSpeed = 6f;
        [SerializeField, Range(0f, 60f)] private float m_RearThighSwing = 28f;
        [SerializeField, Range(0f, 80f)] private float m_RearShinSwing = 42f;
        [SerializeField, Range(0f, 60f)] private float m_RearFootSwing = 24f;
        [SerializeField, Range(0f, 60f)] private float m_RightFrontThighSwing = 26f;
        [SerializeField, Range(0f, 80f)] private float m_RightFrontShinSwing = 38f;
        [SerializeField, Range(0f, 60f)] private float m_RightFrontFootSwing = 22f;
        [SerializeField, Range(0f, 1f)] private float m_BlendSpeed = 0.2f;
        [SerializeField] private float m_BreathCycleSpeed = 0.33f;
        [SerializeField, Range(0f, 4f)] private float m_IdleBreathAmount = 1.4f;
        [SerializeField, Range(0f, 4f)] private float m_MoveBreathAmount = 0.55f;
        [SerializeField] private float m_LookAroundInterval = 5.5f;
        [SerializeField] private float m_LookAroundDuration = 2.4f;
        [SerializeField, Range(0f, 35f)] private float m_LookAroundAngle = 18f;

        private CreatureMover m_Mover;
        private CharacterController m_Controller;
        private EnemyTerrainRoamer m_Roamer;
        private float m_Time;
        private float m_Weight;
        private bool m_Initialized;
        private float m_NextLookAroundTime;
        private float m_LookAroundStartTime;
        private float m_CurrentLookAroundDuration;
        private float m_TargetLookAroundAngle;

        private Bone m_RearRightThigh;
        private Bone m_RearRightShin;
        private Bone m_RearRightFoot;
        private Bone m_RearLeftThigh;
        private Bone m_RearLeftShin;
        private Bone m_RearLeftFoot;
        private Bone m_RightFrontThigh;
        private Bone m_RightFrontShin;
        private Bone m_RightFrontFoot;
        private Bone m_LeftFrontThigh;
        private Bone m_LeftFrontShin;
        private Bone m_LeftFrontFoot;
        private Bone m_BreathRoot;
        private Bone m_BreathChest;
        private Bone m_BreathShoulders;
        private Bone m_LookNeckLower;
        private Bone m_LookNeckMiddle;
        private Bone m_LookNeckUpper;

        private void Awake()
        {
            m_Mover = GetComponent<CreatureMover>();
            m_Controller = GetComponent<CharacterController>();
            m_Roamer = GetComponent<EnemyTerrainRoamer>();
            ScheduleNextLookAround(1.5f);
        }

        private void LateUpdate()
        {
            if (!m_Initialized)
            {
                CacheBones();
                return;
            }

            var moving = IsMoving();
            var targetWeight = moving ? 1f : 0f;
            m_Weight = Mathf.MoveTowards(m_Weight, targetWeight, Time.deltaTime / Mathf.Max(0.01f, m_BlendSpeed));

            if (m_Weight <= 0f)
            {
                ApplyPose(0f, 0f);
                ApplyBreathing(m_IdleBreathAmount);
                ApplyIdleLookAround();
                return;
            }

            m_Time += Time.deltaTime * (IsRunning() ? m_RunCycleSpeed : m_WalkCycleSpeed);
            ApplyPose(m_Time, m_Weight);
            ApplyBreathing(m_MoveBreathAmount);
            ApplyLookAroundPose(0f);
        }

        private bool IsMoving()
        {
            if (m_Mover != null && m_Mover.Axis.sqrMagnitude > 0.001f) return true;
            if (m_Roamer != null && m_Roamer.IsRoaming) return true;
            return m_Controller != null && new Vector3(m_Controller.velocity.x, 0f, m_Controller.velocity.z).sqrMagnitude > 0.001f;
        }

        private bool IsRunning()
        {
            return m_Mover != null && m_Mover.IsRun;
        }

        private void CacheBones()
        {
            m_RearRightThigh = new Bone(transform, "Tiger_001_rig/Root/spine.007/thigh.R");
            m_RearRightShin = new Bone(transform, "Tiger_001_rig/Root/spine.007/thigh.R/shin.R");
            m_RearRightFoot = new Bone(transform, "Tiger_001_rig/Root/spine.007/thigh.R/shin.R/foot.R");
            m_RearLeftThigh = new Bone(transform, "Tiger_001_rig/Root/spine.007/thigh.L");
            m_RearLeftShin = new Bone(transform, "Tiger_001_rig/Root/spine.007/thigh.L/shin.L");
            m_RearLeftFoot = new Bone(transform, "Tiger_001_rig/Root/spine.007/thigh.L/shin.L/foot.L");
            m_RightFrontThigh = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/shoulder.R/front_thigh.R");
            m_RightFrontShin = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/shoulder.R/front_thigh.R/front_shin.R");
            m_RightFrontFoot = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/shoulder.R/front_thigh.R/front_shin.R/front_foot.R");
            m_LeftFrontThigh = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/shoulder.L/front_thigh.L");
            m_LeftFrontShin = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/shoulder.L/front_thigh.L/front_shin.L");
            m_LeftFrontFoot = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/shoulder.L/front_thigh.L/front_shin.L/front_foot.L");
            m_BreathRoot = new Bone(transform, "Tiger_001_rig/Root/spine.007");
            m_BreathChest = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008");
            m_BreathShoulders = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009");
            m_LookNeckLower = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/spine.010");
            m_LookNeckMiddle = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/spine.010/spine.011");
            m_LookNeckUpper = new Bone(transform, "Tiger_001_rig/Root/spine.007/spine.008/spine.009/spine.010/spine.011/spine.012");
            m_Initialized = true;
        }

        private void ApplyPose(float time, float weight)
        {
            var rearRight = Mathf.Sin(time);
            var rearLeft = Mathf.Sin(time + Mathf.PI);
            var frontRight = Mathf.Sin(time + Mathf.PI * 0.5f);

            RotateX(m_RearRightThigh, rearRight * m_RearThighSwing, weight);
            RotateX(m_RearRightShin, -rearRight * m_RearShinSwing, weight);
            RotateX(m_RearRightFoot, rearRight * m_RearFootSwing, weight);

            RotateX(m_RearLeftThigh, rearLeft * m_RearThighSwing, weight);
            RotateX(m_RearLeftShin, -rearLeft * m_RearShinSwing, weight);
            RotateX(m_RearLeftFoot, rearLeft * m_RearFootSwing, weight);

            RotateX(m_RightFrontThigh, frontRight * m_RightFrontThighSwing, weight);
            RotateX(m_RightFrontShin, -frontRight * m_RightFrontShinSwing, weight);
            RotateX(m_RightFrontFoot, frontRight * m_RightFrontFootSwing, weight);

            Restore(m_LeftFrontThigh);
            Restore(m_LeftFrontShin);
            Restore(m_LeftFrontFoot);
        }


        private void ApplyBreathing(float amount)
        {
            var breath = Mathf.Sin(Time.time * m_BreathCycleSpeed * Mathf.PI * 2f);
            var lift = amount * breath;

            RotateX(m_BreathRoot, lift * 0.35f, 1f);
            RotateX(m_BreathChest, lift, 1f);
            RotateX(m_BreathShoulders, lift * -0.45f, 1f);
        }

        private void ApplyIdleLookAround()
        {
            if (Time.time >= m_NextLookAroundTime)
            {
                m_LookAroundStartTime = Time.time;
                m_CurrentLookAroundDuration = Mathf.Max(0.5f, m_LookAroundDuration);
                m_TargetLookAroundAngle = (Random.value < 0.5f ? -1f : 1f) * m_LookAroundAngle;
                ScheduleNextLookAround(m_LookAroundInterval + Random.Range(0.5f, 2.5f));
            }

            var elapsed = Time.time - m_LookAroundStartTime;
            if (elapsed < 0f || elapsed > m_CurrentLookAroundDuration)
            {
                ApplyLookAroundPose(0f);
                return;
            }

            var progress = Mathf.Clamp01(elapsed / m_CurrentLookAroundDuration);
            var turnOutAndBack = Mathf.Sin(progress * Mathf.PI);
            var softened = turnOutAndBack * turnOutAndBack * (3f - 2f * turnOutAndBack);
            ApplyLookAroundPose(m_TargetLookAroundAngle * softened);
        }

        private void ScheduleNextLookAround(float delay)
        {
            m_NextLookAroundTime = Time.time + Mathf.Max(0.1f, delay);
        }

        private void ApplyLookAroundPose(float yaw)
        {
            RotateY(m_LookNeckLower, yaw * 0.25f, 1f);
            RotateY(m_LookNeckMiddle, yaw * 0.35f, 1f);
            RotateY(m_LookNeckUpper, yaw * 0.4f, 1f);
        }
        private static void RotateX(Bone bone, float degrees, float weight)
        {
            if (!bone.IsValid) return;
            bone.Transform.localRotation = bone.BaseRotation * Quaternion.Euler(degrees * weight, 0f, 0f);
        }



        private static void RotateY(Bone bone, float degrees, float weight)
        {
            if (!bone.IsValid) return;
            bone.Transform.localRotation = bone.BaseRotation * Quaternion.Euler(0f, degrees * weight, 0f);
        }
        private static void Restore(Bone bone)
        {
            if (!bone.IsValid) return;
            bone.Transform.localRotation = bone.BaseRotation;
        }

        private struct Bone
        {
            public readonly Transform Transform;
            public readonly Quaternion BaseRotation;
            public bool IsValid => Transform != null;

            public Bone(Transform root, string path)
            {
                Transform = root.Find(path);
                BaseRotation = Transform != null ? Transform.localRotation : Quaternion.identity;
            }
        }
    }
}











