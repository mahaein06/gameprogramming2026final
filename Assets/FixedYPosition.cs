using UnityEngine;

public class FixedYPosition : MonoBehaviour
{
    [SerializeField] private bool lockY = true;
    [SerializeField] private float fixedY;

    private bool initialized;

    private void Awake()
    {
        CaptureCurrentY();
    }

    private void LateUpdate()
    {
        if (!lockY || !initialized) return;

        Vector3 position = transform.position;
        if (!Mathf.Approximately(position.y, fixedY))
        {
            position.y = fixedY;
            transform.position = position;
        }
    }

    public void CaptureCurrentY()
    {
        fixedY = transform.position.y;
        initialized = true;
    }

    public void SetFixedY(float y)
    {
        fixedY = y;
        initialized = true;
    }
}
