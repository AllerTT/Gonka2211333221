using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PureArcadeCarController : MonoBehaviour
{
    [Header("Физика")]
    public float mass = 1200f;
    public float acceleration = 25f;
    public float maxForwardSpeed = 35f;
    public float maxReverseSpeed = 20f;
    public float turnSpeed = 80f;
    public float driftSideways = 6f;
    public float autoAlignSpeed = 1.5f;
    public float groundFriction = 0.98f; // <1 = замедление

    [Header("Визуал колёс")]

    public Transform[] frontWheels;

    public Transform[] rearWheels;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool drifting = Input.GetKey(KeyCode.LeftShift);

        UpdateWheelVisuals(h, v);
    }

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool drifting = Input.GetKey(KeyCode.LeftShift);

        // Сохраняем текущую скорость
        Vector3 worldVelocity = rb.linearVelocity;
        Vector3 localVel = transform.InverseTransformDirection(worldVelocity);
        float forwardSpeed = localVel.z;
        float speed = worldVelocity.magnitude;

        // Ограничение скорости
        if (forwardSpeed > maxForwardSpeed)
        {
            localVel.z = maxForwardSpeed;
            rb.linearVelocity = transform.TransformDirection(localVel);
        }
        else if (forwardSpeed < -maxReverseSpeed)
        {
            localVel.z = -maxReverseSpeed;
            rb.linearVelocity = transform.TransformDirection(localVel);
        }

        // Ускорение
        rb.AddForce(transform.forward * v * acceleration, ForceMode.Acceleration);

        // Поворот
        if (speed > 1f)
        {
            if (Mathf.Abs(h) > 0.1f)
            {
                float currentAngle = transform.eulerAngles.y;
                float newAngle = currentAngle + h * turnSpeed * Time.fixedDeltaTime;
                rb.MoveRotation(Quaternion.Euler(0, newAngle, 0));
            }
            else if (forwardSpeed > 0f)
            {
                float targetAngle = Mathf.Atan2(worldVelocity.x, worldVelocity.z) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.y;
                float smoothed = Mathf.LerpAngle(currentAngle, targetAngle, autoAlignSpeed * Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Euler(0, smoothed, 0));
            }
        }

        // Дрифт
        if (drifting && v != 0)
        {
            rb.AddForce(transform.right * h * driftSideways, ForceMode.Acceleration);
            rb.angularDamping = 0.5f;
        }
        else
        {
            rb.angularDamping = 0.1f;
        }

        // Заменяем `rb.velocity *= friction` на `drag`
        if (Mathf.Abs(v) < 0.1f)
        {
            rb.linearDamping = 2f; // накат
        }
        else
        {
            rb.linearDamping = (1f - groundFriction) * 10f; // имитация трения
        }
    }

   void UpdateWheelVisuals(float h, float v)
    {
        float speed = rb.linearVelocity.magnitude;
        float rotationFront = -speed * 120f * Time.deltaTime;
        float rotationRear = rotationFront * 1.2f; // задние быстрее

        // Вращаем передние колёса
        foreach (var wheel in frontWheels)
        {
            if (wheel != null)
                wheel.Rotate(Vector3.right, rotationFront);
        }

        // Вращаем задние колёса
        foreach (var wheel in rearWheels)
        {
            if (wheel != null)
                wheel.Rotate(Vector3.right, rotationRear);
        }

        // Поворот рулевых колёс (только визуал)
        foreach (var wheel in frontWheels)
        {
            if (wheel != null)
            {
                float targetAngle = h * 30f;
                wheel.localRotation = Quaternion.Slerp(
                    wheel.localRotation,
                    Quaternion.Euler(0, targetAngle, 0),
                    8f * Time.deltaTime
                );
            }
        }
    }
}