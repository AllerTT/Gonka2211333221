using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;
    public float maxRayLength = 0.8f;

    [Header("Движение")]
    public float ускорение = 25f;
    public float максимальнаяСкоростьВперёд = 35f;
    public float максимальнаяСкоростьНазад = 20f;
    public float замедлениеНакатом = 2f;

    [Header("Поворот")]
    public float скоростьПоворота = 80f;
    public float скоростьВыравнивания = 1.5f;

    [Header("Дрифт")]
    public bool включитьДрифт = true;
    public float силаSideways = 6f;
    public float угловаяЗатуханиеВДрифте = 0.5f;

    [Header("Визуальные колёса")]
    public Transform[] передниеКолёса;
    public float уголПоворотаРуля = 30f;
    public float скоростьПоворотаРуля = 8f;

    [Header("Кривые (опционально)")]
    public AnimationCurve turnCurve = AnimationCurve.Linear(0, 1, 1, 0.5f); // слабее на скорости
    public AnimationCurve driftCurve = AnimationCurve.Linear(0, 1, 1, 0.3f); // затухание в дрифте

    private Rigidbody rb;
    private float inputHorizontal;
    private float inputVertical;
    private bool isDrifting;
    private bool grounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;
        rb.angularDamping = 0.1f;
    }

    void Update()
    {
        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        isDrifting = включитьДрифт && Input.GetKey(KeyCode.LeftShift);
    }

    void FixedUpdate()
    {
        // Проверка контакта с землёй
        grounded = Physics.Raycast(groundCheck.position, -transform.up, out RaycastHit hit, maxRayLength);

        // Локальная скорость
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = localVel.z;
        float speed = rb.linearVelocity.magnitude;

        // Ограничение скорости
        if (forwardSpeed > максимальнаяСкоростьВперёд)
        {
            localVel.z = максимальнаяСкоростьВперёд;
            rb.linearVelocity = transform.TransformDirection(localVel);
        }
        else if (forwardSpeed < -максимальнаяСкоростьНазад)
        {
            localVel.z = -максимальнаяСкоростьНазад;
            rb.linearVelocity = transform.TransformDirection(localVel);
        }

        // Ускорение (только на земле)
        if (grounded)
        {
            float effectiveAcceleration = ускорение * (inputVertical > 0 ? 1f : 0.5f);
            rb.AddForceAtPosition(transform.forward * inputVertical * effectiveAcceleration, groundCheck.position, ForceMode.Acceleration);
        }

        // Поворот и выравнивание
        if (grounded && speed > 1f)
        {
            float turnMultiplier = turnCurve.Evaluate(speed / максимальнаяСкоростьВперёд);
            float turnInput = inputHorizontal * скоростьПоворота * turnMultiplier;

            if (Mathf.Abs(inputHorizontal) > 0.1f)
            {
                // Активный поворот
                float currentAngle = transform.eulerAngles.y;
                float newAngle = currentAngle + turnInput * Time.fixedDeltaTime;
                rb.MoveRotation(Quaternion.Euler(0, newAngle, 0));
            }
            else if (forwardSpeed > 0f)
            {
                // Автовыравнивание (только вперёд)
                float targetAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.y;
                float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, скоростьВыравнивания * Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Euler(0, smoothedAngle, 0));
            }
            Debug.Log($"Input V: {inputVertical}, Speed: {rb.linearVelocity.magnitude:F1}, Grounded: {grounded}");
        }

        // Дрифт
        if (grounded && isDrifting && inputVertical != 0)
        {
            rb.AddForceAtPosition(transform.right * inputHorizontal * силаSideways, groundCheck.position, ForceMode.Acceleration);
            rb.angularDamping = угловаяЗатуханиеВДрифте * driftCurve.Evaluate(Mathf.Abs(localVel.x) / 20f);
        }
        else
        {
            rb.angularDamping = 0.1f;
        }

        // Накат
        rb.linearDamping = (Mathf.Abs(inputVertical) < 0.1f) ? замедлениеНакатом : 0f;
    }

    void LateUpdate()
    {
        // Визуальный поворот руля
        foreach (var wheel in передниеКолёса)
        {
            float targetAngle = inputHorizontal * уголПоворотаРуля;
            wheel.localRotation = Quaternion.Slerp(
                wheel.localRotation,
                Quaternion.Euler(0, targetAngle, 0),
                скоростьПоворотаРуля * Time.deltaTime
            );
        }

        // Визуальное вращение всех колёс (если нужно)
        // Можно добавить отдельный скрипт, как в SkidMarks.cs
    }
}