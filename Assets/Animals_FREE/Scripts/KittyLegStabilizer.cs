using System.Collections.Generic;
using UnityEngine;

namespace ithappy.Animals_FREE
{
    [DefaultExecutionOrder(10000)]
    public class KittyLegStabilizer : MonoBehaviour
    {
        [SerializeField]
        private bool m_FreezePosition = true;
        [SerializeField]
        private bool m_FreezeRotation = true;

        private readonly List<BonePose> m_Bones = new List<BonePose>();

        private void Awake()
        {
            CacheLegPose();
        }

        private void OnEnable()
        {
            CacheLegPose();
        }

        private void LateUpdate()
        {
            if (m_Bones.Count == 0)
            {
                CacheLegPose();
            }

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

        private void CacheLegPose()
        {
            m_Bones.Clear();

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string path = GetPathFromThisTransform(child);
                if (IsKittyLegPath(path))
                {
                    m_Bones.Add(new BonePose(child));
                }
            }

            if (m_Bones.Count == 0)
            {
                Debug.LogWarning($"KittyLegStabilizer could not find Kitty leg bones under {name}.", this);
            }
        }

        private static bool IsKittyLegPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.Contains("Kitty_001_rig/")) return false;

            return path.Contains("shoulder.")
                || path.Contains("thigh.")
                || path.Contains("shin.")
                || path.Contains("foot.")
                || path.Contains("toe.");
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