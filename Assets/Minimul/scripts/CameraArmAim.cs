using UnityEngine;

public class CameraArmAim : MonoBehaviour
{
    private const float GizmoPointSize = 0.08f;

    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform target;
    [SerializeField] private float targetHeight = 1.1f;
    [SerializeField] private Transform arm;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private Vector3 rotationOffset;

    public void BindCamera(Camera camera)
    {
        aimCamera = camera;
        target = null;
    }

    public void BindTarget(Transform targetTransform)
    {
        target = targetTransform;
        aimCamera = null;
    }

    private void LateUpdate()
    {
        if (arm == null || muzzle == null) return;

        if (!TryGetAimPoint(out Vector3 aimPoint, out _)) return;

        Vector3 direction = aimPoint - muzzle.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        arm.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(rotationOffset);
    }

    private void OnDrawGizmos()
    {
        if (TryGetAimPoint(out Vector3 aimPoint, out Ray cameraRay))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(aimPoint, GizmoPointSize);

            if (target != null && muzzle != null)
            {
                Gizmos.DrawLine(muzzle.position, aimPoint);
            }
            else if (aimCamera != null)
            {
                Gizmos.DrawLine(cameraRay.origin, aimPoint);
            }
        }

        if (muzzle == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * maxDistance);
    }

    private bool TryGetAimPoint(out Vector3 aimPoint, out Ray cameraRay)
    {
        cameraRay = default;

        if (target != null)
        {
            if (target.TryGetComponent(out CharacterController controller))
            {
                aimPoint = controller.transform.TransformPoint(controller.center);
                return true;
            }

            aimPoint = target.position + Vector3.up * targetHeight;
            return true;
        }

        if (aimCamera == null)
        {
            aimPoint = default;
            return false;
        }

        cameraRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        aimPoint = cameraRay.GetPoint(maxDistance);
        float closestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in Physics.RaycastAll(cameraRay, maxDistance))
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.distance >= closestDistance) continue;

            closestDistance = hit.distance;
            aimPoint = hit.point;
        }

        return true;
    }
}
