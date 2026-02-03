using UnityEngine;

public class ArcadeWheelSpin : MonoBehaviour
{
    public Transform car;
    public bool isFront = true;

    void LateUpdate()
    {
        float speed = car.GetComponent<Rigidbody>().linearVelocity.magnitude;
        float rotation = -speed * 100f * Time.deltaTime;
        if (!isFront) rotation *= 1.2f; // задние быстрее

        transform.Rotate(Vector3.right, rotation);
    }
}