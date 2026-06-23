using UnityEngine;

namespace ithappy.Animals_FREE
{
    public class HorseFrontLegStabilizer : MonoBehaviour
    {
        [SerializeField]
        private bool m_FreezePosition = true;
        [SerializeField]
        private bool m_FreezeRotation = true;

        private readonly string[] m_FrontLegPaths =
        {
            "Horse_001_rig/Root/spine.005/shoulder.R",
            "Horse_001_rig/Root/spine.005/shoulder.R/thigh.R",
            "Horse_001_rig/Root/spine.005/shoulder.R/thigh.R/shin.R",
            "Horse_001_rig/Root/spine.005/shoulder.R/thigh.R/shin.R/foot.R",
            "Horse_001_rig/Root/spine.005/shoulder.R/thigh.R/shin.R/foot.R/toe.R",
            "Horse_001_rig/Root/spine.005/shoulder.L",
            "Horse_001_rig/Root/spine.005/shoulder.L/thigh.L",
            "Horse_001_rig/Root/spine.005/shoulder.L/thigh.L/shin.L",
            "Horse_001_rig/Root/spine.005/shoulder.L/thigh.L/shin.L/foot.L",
            "Horse_001_rig/Root/spine.005/shoulder.L/thigh.L/shin.L/foot.L/toe.L"
        };

        private BonePose[] m_Bones;

        private void Awake()
        {
            CacheFrontLegPose();
        }

        private void OnEnable()
        {
            if (m_Bones == null || m_Bones.Length == 0)
            {
                CacheFrontLegPose();
            }
        }

        private void LateUpdate()
        {
            if (m_Bones == null) return;

            foreach (BonePose bone in m_Bones)
            {
                if (bone.Transform == null) continue;

                if (m_FreezePosition)
                {
                    bone.Transform.localPosition = bone.LocalPosition;
                }

                if (m_FreezeRotation)
                {
                    bone.Transform.localRotation = bone.LocalRotation;
                }
            }
        }

        private void CacheFrontLegPose()
        {
            m_Bones = new BonePose[m_FrontLegPaths.Length];

            for (int i = 0; i < m_FrontLegPaths.Length; i++)
            {
                Transform bone = transform.Find(m_FrontLegPaths[i]);
                m_Bones[i] = new BonePose(bone);
            }
        }

        private readonly struct BonePose
        {
            public readonly Transform Transform;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public BonePose(Transform transform)
            {
                Transform = transform;
                LocalPosition = transform != null ? transform.localPosition : Vector3.zero;
                LocalRotation = transform != null ? transform.localRotation : Quaternion.identity;
            }
        }
    }
}
