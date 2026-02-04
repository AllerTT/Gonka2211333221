using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("◉ КОЛЁСА")]
    [Tooltip("Все 4 коллайдера колёс")]
    public WheelCollider[] Колёса;

    [Header("◉ ДВИГАТЕЛЬ И УСКОРЕНИЕ")]
    [Tooltip("Сила двигателя при движении вперёд (чем больше, тем быстрее разгон)")]
    public float МощностьДвигателя = 2000f;
    
    [Tooltip("Сила двигателя при движении назад")]
    public float МощностьЗаднегоХода = 800f;
    
    [Tooltip("Максимальная скорость вперёд")]
    public float МаксимальнаяСкорость = 50f;
    
    [Tooltip("Характер разгона: 1 = плавный, 2 = резкий старт, 3 = очень резкий")]
    [Range(1f, 3f)]
    public float КриваяРазгона = 1.5f;
    
    [Tooltip("Максимальная скорость задним ходом")]
    public float МаксСкоростьНазад = 20f;

    [Header("◉ РУЛЕВОЕ УПРАВЛЕНИЕ")]
    [Tooltip("На сколько градусов поворачиваются колёса на месте")]
    public float МаксимальныйУголПоворота = 40f;
    
    [Tooltip("Как быстро уменьшается угол поворота на скорости (0 = не уменьшается)")]
    public float ВлияниеСкоростиНаПоворот = 0.1f;
    
    [Tooltip("Как быстро колёса возвращаются в прямое положение")]
    public float СкоростьВозвратаРуля = 10f;

    [Header("◉ ТОРМОЗА")]
    [Tooltip("Сила обычного тормоза (при нажатии S/стрелка вниз)")]
    public float СилаТормоза = 1500f;
    
    [Tooltip("Сила ручного тормоза (Пробел)")]
    public float СилаРучногоТормоза = 3000f;
    
    [Tooltip("При какой скорости включается задний ход")]
    public float ПорогЗаднегоХода = 1.5f;
    
    [Tooltip("Плавность переключения между вперёд/назад")]
    [Range(0.1f, 5f)]
    public float ПлавностьПереключения = 2f;

    [Header("◉ СЦЕПЛЕНИЕ С ДОРОГОЙ")]
    [Tooltip("Сцепление при разгоне и торможении")]
    [Range(0.5f, 3f)]
    public float ПродольноеСцепление = 2.0f;
    
    [Tooltip("Сцепление в поворотах (чем меньше, тем сильнее заносит)")]
    [Range(0.5f, 3f)]
    public float БоковоеСцепление = 1.5f;
    
    [Tooltip("Сцепление при заносе на ручнике")]
    [Range(0.1f, 1f)]
    public float СцеплениеПриДрифте = 0.3f;
    
    [Tooltip("Сопротивление воздуха")]
    public float СопротивлениеВоздуха = 0.1f;
    
    [Tooltip("Сопротивление вращению")]
    public float СопротивлениеВращению = 1f;
    
    [Tooltip("Торможение при отпускании газа")]
    public float ТорможениеНакатом = 1.5f;

    [Header("◉ СТАБИЛЬНОСТЬ")]
    [Tooltip("Сила, которая не даёт машине перевернуться")]
    public float СилаПротивОпрокидывания = 5000f;
    
    [Tooltip("Высота центра масс (чем ниже, тем устойчивее)")]
    public float ВысотаЦентраМасс = -0.6f;

    // Приватные переменные (оставляем на английском для кода)
    private Rigidbody rb;
    private WheelCollider[] frontWheels;
    private WheelCollider[] rearWheels;
    private float currentSteerAngle;
    private bool isHandbrakeActive;
    
    // Переменные для плавного заднего хода
    private bool isInReverseMode = false;
    private float reverseTransitionTimer = 0f;
    private const float REVERSE_TRANSITION_TIME = 0.3f;

    // Кэшированные настройки трения
    private WheelFrictionCurve defaultForwardFriction;
    private WheelFrictionCurve defaultSidewaysFriction;
    private WheelFrictionCurve handbrakeSidewaysFriction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Настройка центра масс для стабильности
        rb.centerOfMass = new Vector3(0, ВысотаЦентраМасс, 0);
        
        // Настройка сопротивлений
        rb.linearDamping = СопротивлениеВоздуха;
        rb.angularDamping = СопротивлениеВращению;

        if (Колёса.Length != 4)
        {
            Debug.LogError("Нужно ровно 4 WheelCollider!");
            return;
        }

        // Сортировка колёс: передние/задние по Z
        var sorted = Колёса.OrderBy(w => w.transform.localPosition.z).ToArray();
        rearWheels = new[] { sorted[0], sorted[1] };
        frontWheels = new[] { sorted[2], sorted[3] };

        // Настройка колёс
        ConfigureWheelColliders();
        
        // Сохранение настроек трения по умолчанию
        CacheDefaultFrictionSettings();
    }

    void ConfigureWheelColliders()
    {
        foreach (var wc in Колёса)
        {
            // Настройка подвески
            wc.suspensionDistance = 0.3f;
            wc.suspensionSpring = new JointSpring
            {
                spring = 35000f,
                damper = 4500f,
                targetPosition = 0.5f
            };

            // Настройка сцепления
            var forwardFriction = wc.forwardFriction;
            forwardFriction.stiffness = ПродольноеСцепление;
            wc.forwardFriction = forwardFriction;

            var sidewaysFriction = wc.sidewaysFriction;
            sidewaysFriction.stiffness = БоковоеСцепление;
            wc.sidewaysFriction = sidewaysFriction;
        }
    }
[Header("◉ ТИП ПРИВОДА")]
[Tooltip("Какие колёса ведущие")]
public ТипПривода типПривода = ТипПривода.ЗаднийПривод;

public enum ТипПривода
{
    ЗаднийПривод,
    ПереднийПривод,
    ПолныйПривод
}
WheelCollider[] GetDriveWheels()
{
    switch (типПривода)
    {
        case ТипПривода.ЗаднийПривод:
            return rearWheels;
        case ТипПривода.ПереднийПривод:
            return frontWheels;
        case ТипПривода.ПолныйПривод:
            return Колёса;
        default:
            return rearWheels;
    }
}
    void CacheDefaultFrictionSettings()
    {
        if (Колёса.Length > 0)
        {
            defaultForwardFriction = Колёса[0].forwardFriction;
            defaultSidewaysFriction = Колёса[0].sidewaysFriction;
            
            // Создание настроек трения для ручного тормоза
            handbrakeSidewaysFriction = defaultSidewaysFriction;
            handbrakeSidewaysFriction.stiffness = СцеплениеПриДрифте;
        }
    }

    void Update()
    {
        // Получение ввода
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool handbrake = Input.GetKey(KeyCode.Space);

        // Обновление таймера перехода в задний ход
        UpdateReverseTransition(vertical);

        // Обработка поворота
        HandleSteering(horizontal);
        
        // Обработка движения и торможения
        HandleMotorAndBrake(vertical, handbrake);
        
        // Обработка ручного тормоза
        HandleHandbrake(handbrake);
        
        // Накат
        rb.linearDamping = (Mathf.Abs(vertical) < 0.1f && !handbrake) ? ТорможениеНакатом : 0f;
    }

    void UpdateReverseTransition(float verticalInput)
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        bool isMovingForward = Vector3.Dot(rb.linearVelocity, transform.forward) > 0;

        // Определяем, хотим ли мы включить задний ход
        bool wantsReverse = verticalInput < -0.1f;
        bool canSwitchToReverse = currentSpeed < ПорогЗаднегоХода;

        if (wantsReverse && canSwitchToReverse)
        {
            // Начинаем переход в режим заднего хода
            reverseTransitionTimer += Time.deltaTime * ПлавностьПереключения;
            
            // Если машина движется вперёд, но медленно, сначала тормозим
            if (isMovingForward && currentSpeed > 0.5f)
            {
                // Применяем тормоз во время перехода
                foreach (var wheel in rearWheels)
                {
                    wheel.brakeTorque = СилаТормоза * Mathf.Clamp01(reverseTransitionTimer / REVERSE_TRANSITION_TIME);
                    wheel.motorTorque = 0f;
                }
                
                // Ждём, пока машина почти остановится
                if (currentSpeed < 0.8f && reverseTransitionTimer >= REVERSE_TRANSITION_TIME * 0.5f)
                {
                    isInReverseMode = true;
                }
            }
            else
            {
                // Машина почти остановилась или стоит, включаем задний ход
                if (reverseTransitionTimer >= REVERSE_TRANSITION_TIME)
                {
                    isInReverseMode = true;
                }
            }
        }
        else
        {
            // Сбрасываем таймер и выходим из режима заднего хода
            reverseTransitionTimer = Mathf.Max(0f, reverseTransitionTimer - Time.deltaTime * ПлавностьПереключения * 2f);
            
            // Если нажат газ вперёд или скорость стала большой, отключаем задний ход
            if (verticalInput > 0.1f || currentSpeed > ПорогЗаднегоХода * 1.5f)
            {
                isInReverseMode = false;
                reverseTransitionTimer = 0f;
            }
        }
    }

    void HandleSteering(float horizontalInput)
{
    if (horizontalInput != 0)
    {
        // Динамический угол поворота в зависимости от скорости
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude * ВлияниеСкоростиНаПоворот);
        float dynamicSteerAngle = МаксимальныйУголПоворота * (1f - speedFactor * 0.5f);
        
        // УБИРАЕМ ИНВЕРТИРОВАНИЕ ПРИ ЗАДНЕМ ХОДЕ
        // Просто всегда одинаковое управление
        currentSteerAngle = horizontalInput * dynamicSteerAngle;
    }
    else
    {
        // Плавный возврат колёс в исходное положение
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, 0f, Time.deltaTime * СкоростьВозвратаРуля);
    }

    // Применение поворота к передним колёсам
    foreach (var wheel in frontWheels)
    {
        wheel.steerAngle = currentSteerAngle;
    }
}

   void HandleMotorAndBrake(float verticalInput, bool handbrake)
{
    float currentSpeed = rb.linearVelocity.magnitude;
    float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
    
    // Коэффициент плавности перехода
    float reverseBlend = Mathf.Clamp01(reverseTransitionTimer / REVERSE_TRANSITION_TIME);

    // Определяем ведущие колёса
    WheelCollider[] ведущиеКолеса = GetDriveWheels();

    // Обработка для ведущих колёс
    foreach (var wheel in ведущиеКолеса)
    {
        if (!handbrake)
        {
            if (isInReverseMode && verticalInput < -0.1f)
            {
                // ПЛАВНЫЙ ЗАДНИЙ ХОД с переходом
                wheel.brakeTorque = 0f;
                
                // Плавно увеличиваем мощность заднего хода
                float reverseInput = Mathf.Abs(verticalInput);
                float reversePower = Mathf.Pow(reverseInput, КриваяРазгона);
                
                // ДОБАВЛЕНО: множитель для усиления
                float множительЗаднегоХода = 1.5f; // Увеличиваем силу заднего хода на 50%
                wheel.motorTorque = -reversePower * МощностьЗаднегоХода * множительЗаднегоХода * reverseBlend;
                
                // Ограничение скорости заднего хода
                if (Mathf.Abs(forwardSpeed) > МаксСкоростьНазад && forwardSpeed < 0)
                {
                    wheel.motorTorque = 0f;
                    wheel.brakeTorque = СилаТормоза * 0.3f;
                }
            }
            else if (verticalInput > 0.1f)
            {
                // ДВИЖЕНИЕ ВПЕРЁД
                wheel.brakeTorque = 0f;
                
                // Плавный переход из заднего хода
                float forwardBlend = isInReverseMode ? (1f - reverseBlend) : 1f;
                
                // ДОБАВЛЕНО: усиление переднего хода
                float множительПереднегоХода = 1.2f;
                
                // Ограничение скорости
                if (currentSpeed < МаксимальнаяСкорость || forwardSpeed < 0)
                {
                    float accelerationPower = Mathf.Pow(verticalInput, КриваяРазгона) * forwardBlend;
                    wheel.motorTorque = accelerationPower * МощностьДвигателя * множительПереднегоХода;
                }
                else if (forwardSpeed > 0)
                {
                    wheel.motorTorque = 0f;
                }
            }
        }
    }
}
    void HandleHandbrake(bool handbrake)
    {
        if (handbrake != isHandbrakeActive)
        {
            isHandbrakeActive = handbrake;
            
            if (handbrake)
            {
                // Применяем сниженное сцепление на задних колёсах для дрифта
                foreach (var wheel in rearWheels)
                {
                    wheel.sidewaysFriction = handbrakeSidewaysFriction;
                }
                // Сбрасываем режим заднего хода
                isInReverseMode = false;
                reverseTransitionTimer = 0f;
            }
            else
            {
                // Восстанавливаем обычное сцепление
                foreach (var wheel in rearWheels)
                {
                    wheel.sidewaysFriction = defaultSidewaysFriction;
                }
            }
        }
    }

    void FixedUpdate()
    {
        // Стабилизация против опрокидывания
        StabilizeCar();
    }

    void StabilizeCar()
    {
        // Лучи вниз по бокам машины
        Vector3[] rayStarts = new Vector3[]
        {
            transform.position + transform.right * 1f + transform.up * 0.5f,
            transform.position - transform.right * 1f + transform.up * 0.5f
        };

        float rayLength = 2f;
        RaycastHit hit;
        
        for (int i = 0; i < rayStarts.Length; i++)
        {
            if (!Physics.Raycast(rayStarts[i], -transform.up, out hit, rayLength))
            {
                // Если луч не касается земли, применяем стабилизирующую силу
                Vector3 forcePoint = transform.position + (i == 0 ? transform.right : -transform.right) * 1f;
                rb.AddForceAtPosition(-transform.up * СилаПротивОпрокидывания * Time.fixedDeltaTime, forcePoint);
            }
        }
    }

    // Вспомогательный метод для отладки
    void OnGUI()
    {
        if (Application.isEditor)
        {
            GUI.Label(new Rect(10, 10, 300, 100), 
                $"Режим заднего хода: {isInReverseMode}\n" +
                $"Переход: {reverseTransitionTimer:F2}\n" +
                $"Скорость: {rb.linearVelocity.magnitude:F1}\n" +
                $"Скорость вперёд: {Vector3.Dot(rb.linearVelocity, transform.forward):F1}");
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && Колёса != null && Колёса.Length > 0)
        {
            ConfigureWheelColliders();
            CacheDefaultFrictionSettings();
        }
    }
}