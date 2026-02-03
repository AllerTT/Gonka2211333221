using UnityEngine;

public class WheelVisualSync : MonoBehaviour
{
    public WheelCollider wheelCollider;

    void LateUpdate()
    {
        if (wheelCollider == null) return;

        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);

        transform.position = pos;
        transform.rotation = rot;
    }
}