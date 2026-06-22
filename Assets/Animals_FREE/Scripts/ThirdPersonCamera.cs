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
        private Vector3 m_LocalCameraOffset;
        private Vector3 m_LocalLookPoint;

        protected override void Awake()
        {
            base.Awake();
            InitializeFromCurrentTransform();
        }

        private void LateUpdate()
        {
            Move(Time.deltaTime);
        }

        public void ReinitializeFromCurrentTransform()
        {
            InitializeFromCurrentTransform();
        }

        private void InitializeFromCurrentTransform()
        {
            if (m_Player == null)
            {
                m_LookPoint = transform.position + transform.forward * TargetDistance;
                m_TargetPos = transform.position;
                return;
            }

            var pivot = GetWorldPivot();
            m_LocalCameraOffset = m_Player.InverseTransformDirection(m_Transform.position - pivot);
            m_LocalLookPoint = m_Player.InverseTransformPoint(m_Transform.position + m_Transform.forward * TargetDistance);

            var fromPlayer = m_Transform.position - m_Player.position;
            m_Distance = Mathf.Clamp(fromPlayer.magnitude, MIN_DISTANCE, MAX_DISTANCE);
            m_Zoom = 1f - Mathf.InverseLerp(MIN_DISTANCE, MAX_DISTANCE, m_Distance);

            m_TargetPos = m_Transform.position;
            m_LookPoint = m_Player.TransformPoint(m_LocalLookPoint);
        }

        private Vector3 GetWorldPivot()
        {
            return m_Player == null ? m_Transform.position : m_Player.position + Vector3.up * m_Offset;
        }

        public override void SetInput(in Vector2 delta, float scroll)
        {
            base.SetInput(delta, scroll);

            if (m_UseSceneCameraPose && m_Player != null)
            {
                var pivot = GetWorldPivot();
                var pitch = Quaternion.AngleAxis(m_Angles.x, m_Player.right);
                var cameraOffset = m_Player.TransformDirection(m_LocalCameraOffset);

                m_TargetPos = pivot + pitch * cameraOffset;
                m_LookPoint = pivot;
                return;
            }
            var dir = new Vector3(0, 0, -m_Distance);
            var rot = Quaternion.Euler(m_Angles.x, m_Angles.y, 0f);

            var playerPos = (m_Player == null) ? Vector3.zero : m_Player.position;
            m_LookPoint = playerPos + m_Offset * Vector3.up;
            m_TargetPos = m_LookPoint + rot * dir;
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

                m_Transform.LookAt(m_LookPoint);
            }

            void target()
            {
                if (m_Target == null)
                {
                    return;
                }

                m_Target.position = m_Transform.position + m_Transform.forward * TargetDistance;
            }
        }
    }
}
