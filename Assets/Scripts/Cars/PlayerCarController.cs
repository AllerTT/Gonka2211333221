using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [System.Serializable]
    public class WheelGroup
    {
        [Header("Левое колесо")]
        public WheelCollider leftWheel;
        [Header("Правое колесо")]
        public WheelCollider rightWheel;

        [Space(5)]
        public bool motor;
        public bool steering;
    }

    [Header("Передняя ось")]
    public WheelGroup front = new WheelGroup();

    [Header("Задняя ось")]
    public WheelGroup rear = new WheelGroup();

    [Header("Основные настройки")]
    public float ускорение = 2500f;
    public float максимальнаяСкоростьВперёд = 30f;
    public float максимальнаяСкоростьНазад = 15f;
    public float максимальныйУголПоворота = 28f;
    public float скоростьВозвратаРуля = 12f;
    public float силаBrakeTorque = 6000f;
    public float замедлениеНакатом = 0.4f;

    [Header("Стартовый рывок (Launch Boost)")]
    public bool включитьСтартовыйРывок = true;
    public float силаРывка = 1.8f;
    public float длительностьРывка = 0.5f;

    [Header("Дрифт")]
    public bool включитьДрифт = true;
    [Range(0f, 1f)] public float снижениеСцепленияВДрифте = 0.25f;
    public float уголПоворотаВДрифте = 35f;

    private Rigidbody rb;
    private float inputHorizontal;
    private float inputVertical;
    private bool isDrifting = false;
    private float launchBoostTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;
        rb.angularDamping = 0.3f;
    }

    void Update()
    {
        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        // Дрифт работает, ПОКА зажат Left Shift
        isDrifting = включитьДрифт && Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(inputVertical) > 0.1f;
    }

    void FixedUpdate()
    {
        ApplySteering();
        ApplyDrift();
        ApplyMotorAndBrakes();
        ApplySpeedLimit();
        ApplyCoastDrag();
    }

    void ApplySteering()
    {
        float targetSteerAngle = inputHorizontal * максимальныйУголПоворота;

        if (isDrifting)
        {
            targetSteerAngle = inputHorizontal * уголПоворотаВДрифте;
        }

        if (front.steering)
        {
            front.leftWheel.steerAngle = Mathf.Lerp(front.leftWheel.steerAngle, targetSteerAngle, скоростьВозвратаРуля * Time.fixedDeltaTime);
            front.rightWheel.steerAngle = front.leftWheel.steerAngle;
        }

        if (rear.steering)
        {
            float rearAngle = targetSteerAngle * 0.4f;
            rear.leftWheel.steerAngle = Mathf.Lerp(rear.leftWheel.steerAngle, rearAngle, скоростьВозвратаРуля * Time.fixedDeltaTime);
            rear.rightWheel.steerAngle = rear.leftWheel.steerAngle;
        }
    }

    void ApplyDrift()
    {
        if (!включитьДрифт) return;

        float slipModifier = isDrifting ? снижениеСцепленияВДрифте : 1f;

        SetWheelFriction(rear.leftWheel, slipModifier);
        SetWheelFriction(rear.rightWheel, slipModifier);
        SetWheelFriction(front.leftWheel, 1f);
        SetWheelFriction(front.rightWheel, 1f);
    }

    void SetWheelFriction(WheelCollider wheel, float sidewaysMultiplier)
    {
        WheelFrictionCurve curve = wheel.sidewaysFriction;
        curve.stiffness = sidewaysMultiplier;
        wheel.sidewaysFriction = curve;
    }

    void ApplyMotorAndBrakes()
    {
        float motorTorque = 0f;
        float brakeTorque = 0f;

        float boostMultiplier = 1f;
        if (включитьСтартовыйРывок)
        {
            if (launchBoostTimer > 0f)
            {
                boostMultiplier = силаРывка;
                launchBoostTimer -= Time.fixedDeltaTime;
            }
            else if (Mathf.Abs(inputVertical) > 0.1f && rb.linearVelocity.magnitude < 2f)
            {
                launchBoostTimer = длительностьРывка;
            }
        }

        if (Mathf.Abs(inputVertical) > 0.05f)
        {
            motorTorque = inputVertical * ускорение * boostMultiplier;
        }
        else
        {
            brakeTorque = силаBrakeTorque * 0.2f;
        }

        front.leftWheel.motorTorque = front.motor ? motorTorque : 0f;
        front.rightWheel.motorTorque = front.motor ? motorTorque : 0f;
        rear.leftWheel.motorTorque = rear.motor ? motorTorque : 0f;
        rear.rightWheel.motorTorque = rear.motor ? motorTorque : 0f;

        ApplyBrakeToWheel(front.leftWheel, brakeTorque);
        ApplyBrakeToWheel(front.rightWheel, brakeTorque);
        ApplyBrakeToWheel(rear.leftWheel, brakeTorque);
        ApplyBrakeToWheel(rear.rightWheel, brakeTorque);
    }

    void ApplySpeedLimit()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = localVelocity.z;

        if (forwardSpeed > максимальнаяСкоростьВперёд)
        {
            localVelocity.z = максимальнаяСкоростьВперёд;
            rb.linearVelocity = transform.TransformDirection(localVelocity);
        }
        else if (forwardSpeed < -максимальнаяСкоростьНазад)
        {
            localVelocity.z = -максимальнаяСкоростьНазад;
            rb.linearVelocity = transform.TransformDirection(localVelocity);
        }
    }

    void ApplyBrakeToWheel(WheelCollider wheel, float torque)
    {
        wheel.brakeTorque = torque;
    }

    void ApplyCoastDrag()
    {
        rb.linearDamping = (Mathf.Abs(inputVertical) <= 0.05f) ? замедлениеНакатом : 0f;
    }
}