using UnityEngine;

namespace ithappy.Animals_FREE
{
    public class ThirdPersonCamera : PlayerCamera
    {
        [SerializeField, Range(0f, 2f)]
        private float m_Offset = 1.5f;
        [SerializeField, Range(0f, 360f)]
        private float m_CameraSpeed = 90f;
        [SerializeField]
        private bool m_UseSceneCameraPose = true;

        private Vector3 m_LookPoint;
        private Vector3 m_TargetPos;
        private float m_YawOffset;

        protected override void Awake()
        {
            base.Awake();
            InitializeFromCurrentTransform(true);
        }

        private void LateUpdate()
        {
            UpdateOrbitPose();
            Move(Time.deltaTime);
        }

        public override void BindPlayer(Transform player)
        {
            base.BindPlayer(player);
            InitializeFromCurrentTransform(true);
        }

        public void ReinitializeFromCurrentTransform()
        {
            InitializeFromCurrentTransform(true);
        }

        public void SetYawOffset(float yawOffset)
        {
            m_YawOffset = yawOffset;
            UpdateOrbitPose();
        }

        private void InitializeFromCurrentTransform(bool snapToTarget)
        {
            if (m_Player == null)
            {
                m_LookPoint = transform.position + transform.forward * TargetDistance;
                m_TargetPos = transform.position;
                UpdateTargetTransform();
                return;
            }

            var pivot = GetWorldPivot();
            m_Distance = ZoomToDistance();

            if (m_UseSceneCameraPose)
            {
                var toCamera = m_Transform.position - pivot;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    var lookRotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up).eulerAngles;
                    m_Angles = new Vector2(NormalizeAngle(lookRotation.x), NormalizeAngle(lookRotation.y + 90f - m_YawOffset));
                    m_Angles.x = Mathf.Clamp(m_Angles.x, m_MinAngle, m_MaxAngle);
                }
            }

            UpdateOrbitPose();
            if (snapToTarget)
            {
                SnapToTarget();
            }
        }

        private Vector3 GetWorldPivot()
        {
            return m_Player == null ? m_Transform.position : m_Player.position + Vector3.up * m_Offset;
        }

        public override void SetInput(in Vector2 delta, float scroll)
        {
            base.SetInput(delta, scroll);
            UpdateOrbitPose();
        }

        private float ZoomToDistance()
        {
            return (1f - Mathf.Clamp01(m_Zoom)) * (MAX_DISTANCE - MIN_DISTANCE) + MIN_DISTANCE;
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        private void UpdateOrbitPose()
        {
            if (m_Player == null)
            {
                return;
            }

            var dir = new Vector3(0, 0, -m_Distance);
            var rot = Quaternion.Euler(m_Angles.x, m_Angles.y + m_YawOffset, 0f);

            m_LookPoint = GetWorldPivot();
            m_TargetPos = m_LookPoint + rot * dir;
        }

        private void SnapToTarget()
        {
            m_Transform.position = m_TargetPos;
            LookAtTarget();
            UpdateTargetTransform();
        }

        private void Move(float deltaTime)
        {
            camera();
            target();

            void camera()
            {
                var direction = m_TargetPos - m_Transform.position;
                var delta = m_CameraSpeed * deltaTime;

                if (delta * delta > direction.sqrMagnitude)
                {
                    m_Transform.position = m_TargetPos;
                }
                else
                {
                    m_Transform.position += delta * direction.normalized;
                }

                LookAtTarget();
            }

            void target()
            {
                UpdateTargetTransform();
            }
        }

        private void LookAtTarget()
        {
            var lookDirection = m_LookPoint - m_Transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                m_Transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }

        private void UpdateTargetTransform()
        {
            if (m_Target != null)
            {
                m_Target.position = m_LookPoint + m_Transform.forward * TargetDistance;
            }
        }
    }
}
