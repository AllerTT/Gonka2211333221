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

    [Header("◉ ПОДВЕСКА")]
    [Tooltip("Ход подвески в метрах (расстояние от колеса до кузова)")]
    [Range(0.1f, 0.5f)]
    public float ХодПодвески = 0.3f;
    
    [Tooltip("Жёсткость пружины подвески (чем выше, тем жёстче)")]
    [Range(10000f, 80000f)]
    public float ЖёсткостьПружины = 35000f;
    
    [Tooltip("Сила демпфирования (поглощение ударов, чем выше — меньше отскоков)")]
    [Range(1000f, 10000f)]
    public float СилаДемпфирования = 4500f;
    
    [Tooltip("Целевая позиция пружины (0 = полностью сжата, 1 = полностью разжата)")]
    [Range(0f, 1f)]
    public float ЦелеваяПозицияПружины = 0.5f;
    
    // [Tooltip("Высота кузова над землёй (в метрах)")]
    // [Range(-0.3f, 0.3f)]
    // public float ВысотаКузова = -0.1f;  // ← ЗАКОММЕНТИРОВАНО

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

    [Header("◉ ТИП ПРИВОДА")]
    [Tooltip("Какие колёса ведущие")]
    public ТипПривода типПривода = ТипПривода.ЗаднийПривод;

    public enum ТипПривода
    {
        ЗаднийПривод,
        ПереднийПривод,
        ПолныйПривод
    }

    // Приватные переменные
    private Rigidbody rb;
    private WheelCollider[] frontWheels;
    private WheelCollider[] rearWheels;
    private float currentSteerAngle;
    private bool isHandbrakeActive;
    
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
            // Настройка подвески из инспектора
            wc.suspensionDistance = ХодПодвески;
            wc.suspensionSpring = new JointSpring
            {
                spring = ЖёсткостьПружины,
                damper = СилаДемпфирования,
                targetPosition = ЦелеваяПозицияПружины
            };

            // Настройка сцепления
            var forwardFriction = wc.forwardFriction;
            forwardFriction.stiffness = ПродольноеСцепление;
            wc.forwardFriction = forwardFriction;

            var sidewaysFriction = wc.sidewaysFriction;
            sidewaysFriction.stiffness = БоковоеСцепление;
            wc.sidewaysFriction = sidewaysFriction;
        }
        
        // Коррекция высоты кузова
        // AdjustCarHeight();  // ← ЗАКОММЕНТИРОВАНО
    }

    // // Коррекция высоты кузова относительно подвески
    // // void AdjustCarHeight()
    // // {
    // //     // Проверяем, что массив существует и не пуст
    // //     if (Колёса == null || Колёса.Length == 0) return;
    // //     
    // //     float avgWheelY = 0f;
    // //     int validWheelCount = 0;
    // //     
    // //     foreach (var wc in Колёса)
    // //     {
    // //         // Пропускаем null-элементы
    // //         if (wc == null) continue;
    // //         
    // //         avgWheelY += wc.transform.position.y;
    // //         validWheelCount++;
    // //     }
    // //     
    // //     // Если нет валидных колёс, выходим
    // //     if (validWheelCount == 0) return;
    // //     
    // //     avgWheelY /= validWheelCount;
    // //     
    // //     // Смещаем центр масс
    // //     Vector3 newCenterOfMass = rb.centerOfMass;
    // //     newCenterOfMass.y = ВысотаЦентраМасс + ВысотаКузова;
    // //     rb.centerOfMass = newCenterOfMass;
    // // }

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

        // Обработка поворота
        HandleSteering(horizontal);
        
        // Обработка движения и торможения
        HandleMotorAndBrake(vertical, handbrake);
        
        // Обработка ручного тормоза
        HandleHandbrake(handbrake);
        
        // Накат
        rb.linearDamping = (Mathf.Abs(vertical) < 0.1f && !handbrake) ? ТорможениеНакатом : 0f;
    }

    void HandleSteering(float horizontalInput)
    {
        if (horizontalInput != 0)
        {
            // Динамический угол поворота в зависимости от скорости
            float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude * ВлияниеСкоростиНаПоворот);
            float dynamicSteerAngle = МаксимальныйУголПоворота * (1f - speedFactor * 0.5f);
            
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
        bool isMovingForward = forwardSpeed > 0.5f;
        bool isMovingBackward = forwardSpeed < -0.5f;
        
        // Определяем ведущие колёса ДЛЯ МОТОРА
        WheelCollider[] driveWheels = GetDriveWheels();
        
        // === 1. СБРОС ВСЕХ КРУТЯЩИХ МОМЕНТОВ ===
        foreach (var wheel in Колёса)
        {
            wheel.motorTorque = 0f;
            wheel.brakeTorque = 0f;
        }
        
        // === 2. ОБРАБОТКА ТОРМОЗА (кнопка S) ===
        if (verticalInput < -0.1f && !handbrake)
        {
            // Если движемся вперёд — ТОРМОЗИМ
            if (isMovingForward && currentSpeed > 2f)
            {
                // Тормоз на ВСЕХ колёсах
                foreach (var wheel in Колёса)
                {
                    wheel.brakeTorque = СилаТормоза;
                }
                return; // Выходим — не применяем мотор
            }
            // Если почти остановились — ЕДЕМ НАЗАД
            else if (currentSpeed < 2f || isMovingBackward)
            {
                float reverseInput = Mathf.Abs(verticalInput);
                float reversePower = Mathf.Pow(reverseInput, КриваяРазгона);
                
                // Моторный крутящий момент ТОЛЬКО на ведущих колёсах
                foreach (var wheel in driveWheels)
                {
                    wheel.motorTorque = -reversePower * МощностьЗаднегоХода;
                }
                
                // Ограничение скорости назад
                if (isMovingBackward && Mathf.Abs(forwardSpeed) > МаксСкоростьНазад)
                {
                    foreach (var wheel in Колёса)
                    {
                        wheel.brakeTorque = СилаТормоза * 0.5f;
                    }
                }
                return;
            }
        }
        
        // === 3. ДВИЖЕНИЕ ВПЕРЁД ===
        if (verticalInput > 0.1f && !handbrake)
        {
            if (currentSpeed < МаксимальнаяСкорость || forwardSpeed < 0)
            {
                float accelerationPower = Mathf.Pow(verticalInput, КриваяРазгона);
                foreach (var wheel in driveWheels)
                {
                    wheel.motorTorque = accelerationPower * МощностьДвигателя;
                }
            }
        }
        
        // === 4. ТОРМОЖЕНИЕ ПРИ ОТПУСКАНИИ КЛАВИШ ===
        if (Mathf.Abs(verticalInput) < 0.1f && !handbrake)
        {
            // Лёгкое торможение для остановки движения
            float brakeForce = isMovingBackward ? СилаТормоза * 0.7f : СилаТормоза * 0.3f;
            foreach (var wheel in Колёса)
            {
                wheel.brakeTorque = brakeForce;
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
                // Применяем силу ручного тормоза на задних колёсах
                foreach (var wheel in rearWheels)
                {
                    wheel.brakeTorque = СилаРучногоТормоза;
                }
            }
            else
            {
                // Восстанавливаем обычное сцепление
                foreach (var wheel in rearWheels)
                {
                    wheel.sidewaysFriction = defaultSidewaysFriction;
                }
                // Сбрасываем тормоз
                foreach (var wheel in rearWheels)
                {
                    wheel.brakeTorque = 0f;
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

    // Визуализация колёс (опционально, для отладки)
    void OnDrawGizmosSelected()
    {
        if (Колёса == null || Колёса.Length == 0) return;
        
        Gizmos.color = Color.yellow;
        foreach (var wc in Колёса)
        {
            // Показываем радиус колеса
            Gizmos.DrawWireSphere(wc.transform.position, wc.radius);
            
            // Показываем ход подвески
            Vector3 suspensionStart = wc.transform.position + Vector3.up * wc.suspensionDistance;
            Gizmos.DrawLine(suspensionStart, wc.transform.position);
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