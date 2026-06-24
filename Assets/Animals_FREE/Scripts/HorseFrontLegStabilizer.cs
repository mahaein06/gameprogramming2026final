using UnityEngine;

namespace ithappy.Animals_FREE
{
    [DefaultExecutionOrder(10000)]
    public class HorseFrontLegStabilizer : MonoBehaviour
    {
        [SerializeField]
        private bool m_FreezePosition = true;
        [SerializeField]
        private bool m_FreezeRotation = true;

        private readonly string[] m_FrontLegPaths =
        {
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.R",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.R/front_thigh.R",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.R/front_thigh.R/front_shin.R",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.R/front_thigh.R/front_shin.R/front_foot.R",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.R/front_thigh.R/front_shin.R/front_foot.R/front_toe.R",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.L",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.L/front_thigh.L",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.L/front_thigh.L/front_shin.L",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.L/front_thigh.L/front_shin.L/front_foot.L",
            "Horse_001_rig/Root/spine.005/spine.006/spine.007/front_shoulder.L/front_thigh.L/front_shin.L/front_foot.L/front_toe.L"
        };

        private BonePose[] m_Bones;

        private void Awake()
        {
            CacheFrontLegPose();
        }

        private void OnEnable()
        {
            CacheFrontLegPose();
        }

        private void LateUpdate()
        {
            if (m_Bones == null || m_Bones.Length == 0)
            {
                CacheFrontLegPose();
            }

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
                Transform bone = FindBone(m_FrontLegPaths[i]);
                m_Bones[i] = new BonePose(bone);

                if (bone == null)
                {
                    Debug.LogWarning($"HorseFrontLegStabilizer could not find bone '{m_FrontLegPaths[i]}' under {name}.", this);
                }
            }
        }

        private Transform FindBone(string path)
        {
            Transform bone = transform.Find(path);
            if (bone != null) return bone;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string childPath = GetPathFromThisTransform(child);
                if (childPath.EndsWith(path, System.StringComparison.Ordinal))
                {
                    return child;
                }
            }

            string boneName = path.Substring(path.LastIndexOf('/') + 1);
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == boneName)
                {
                    string childPath = GetPathFromThisTransform(child);
                    if (childPath.Contains("Horse_001_rig/Root/spine.005/spine.006/spine.007", System.StringComparison.Ordinal))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private string GetPathFromThisTransform(Transform target)
        {
            if (target == null) return string.Empty;

            string path = target.name;
            Transform current = target.parent;
            while (current != null && current != transform)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
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