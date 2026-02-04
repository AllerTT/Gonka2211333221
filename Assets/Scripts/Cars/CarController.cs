using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("◉ КОЛЁСА")]
    [Tooltip("Все 4 коллайдера колёс")]
    public WheelCollider[] Колёса;

    [Header("◉ ДВИГАТЕЛЬ И УСКОРЕНИЕ")]
    [Tooltip("Базовая сила двигателя при движении вперёд (редактируемая)")]
    public float БазоваяМощностьДвигателя = 2000f;
    
    [Tooltip("Сила двигателя при движении назад")]
    public float МощностьЗаднегоХода = 800f;
    
    [Tooltip("Базовая максимальная скорость вперёд (редактируемая)")]
    public float БазоваяМаксимальнаяСкорость = 50f;
    
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
    
    [Tooltip("Множитель мощности для заднего привода (увеличивает ускорение)")]
    [Range(1f, 2f)]
    public float МножительЗаднегоПривода = 1.3f;

    public enum ТипПривода
    {
        ЗаднийПривод,
        ПереднийПривод,
        ПолныйПривод
    }

    [Header("◉ НИТРО (БУСТ)")]
    [Tooltip("Включить систему нитро")]
    public bool ИспользоватьНитро = true;

    [Tooltip("Клавиша активации нитро (Left Control)")]
    public KeyCode КлавишаНитро = KeyCode.LeftControl;

    [Tooltip("Множитель мощности двигателя при активации нитро (например: 2.0 = в 2 раза сильнее)")]
    [Range(1.5f, 4f)]
    public float МножительМощностиНитро = 2.5f;

    [Tooltip("Множитель максимальной скорости при активации нитро")]
    [Range(1.1f, 2f)]
    public float МножительСкоростиНитро = 1.3f;

    [Tooltip("Максимальный запас нитро (в процентах, 100% = полный запас)")]
    [Range(0f, 100f)]
    public float МаксимальныйЗапасНитро = 100f;

    [Tooltip("Текущий запас нитро (в процентах)")]
    [Range(0f, 100f)]
    public float ТекущийЗапасНитро = 100f;

    [Tooltip("Скорость расхода нитро (% в секунду)")]
    [Range(5f, 50f)]
    public float СкоростьРасходаНитро = 20f;

    [Tooltip("Скорость восстановления нитро (% в секунду)")]
    [Range(1f, 20f)]
    public float СкоростьВосстановленияНитро = 5f;

    [Tooltip("Минимальная скорость для активации нитро (км/ч)")]
    public float МинимальнаяСкоростьДляНитро = 10f;

    [Tooltip("Звуковой эффект нитро (опционально)")]
    public AudioSource ЗвукНитро;

    [Header("◉ ФИЗИКА ПРЫЖКОВ И КОЧЕК")]
    [Tooltip("Базовая сила наклона машины вперед в прыжке")]
    [Range(0f, 100f)]
    public float СилаНаклонаВПрыжке = 25f;

    [Tooltip("Дополнительная хаотичная сила для раскачки в воздухе")]
    [Range(0f, 50f)]
    public float ХаотичнаяСилаРаскачки = 15f;

    [Tooltip("Скорость наклона (чем выше, тем быстрее машина наклоняется)")]
    [Range(0.1f, 5f)]
    public float СкоростьНаклона = 2f;

    [Tooltip("Демпфирование вращения в воздухе (чем выше, тем стабильнее)")]
    [Range(0f, 10f)]
    public float ДемпфированиеВВоздухе = 3f;

    [Tooltip("Сила отскока при приземлении на все колёса")]
    [Range(0f, 30f)]
    public float СилаОтскока = 8f;

    [Tooltip("Сила отскока при приземлении на задние колёса")]
    [Range(0f, 40f)]
    public float СилаОтскокаЗадних = 12f;

    [Tooltip("Сила отскока при приземлении на передние колёса")]
    [Range(0f, 40f)]
    public float СилаОтскокаПередних = 10f;

    [Tooltip("Максимальный угол наклона в прыжке (градусы)")]
    [Range(0f, 60f)]
    public float МаксимальныйУголНаклона = 35f;

    [Tooltip("Чувствительность к кочкам (чем больше, тем сильнее реакция)")]
    [Range(0f, 2f)]
    public float ЧувствительностьККочкам = 0.5f;

    [Tooltip("Сила подбрасывания от кочек")]
    [Range(0f, 20f)]
    public float СилаПодбрасыванияОтКочек = 5f;

    [Tooltip("Время задержки перед началом наклона (секунды)")]
    [Range(0f, 1f)]
    public float ЗадержкаНаклона = 0.1f;

    // Приватные переменные
    private Rigidbody rb;
    private WheelCollider[] frontWheels;
    private WheelCollider[] rearWheels;
    private float currentSteerAngle;
    private bool isHandbrakeActive;
    
    // ПЕРЕМЕННЫЕ ДЛЯ РАСЧЕТОВ (не отображаются в инспекторе)
    private float текущаяМощностьДвигателя;      // Используется в расчетах
    private float текущаяМаксимальнаяСкорость;   // Используется в расчетах
    
    // Переменные для нитро
    private bool нитроАктивно = false;
    
    // Переменные для отладки
    private float предыдущаяСкорость;
    private float текущееУскорение;
    private float максимальнаяСилаТормоза;
    private float максимальныеОбороты;
    private List<float> последниеСкорости = new List<float>();
    private const int КоличествоОтсчетов = 10;
    
    // Переменные для физики прыжков
    private bool вВоздухе = false;
    private int колёсНаЗемле = 0;
    private float времяВВоздухе = 0f;
    private Vector3 начальнаяСкоростьПрыжка;
    private Vector3 начальныйУголПоворота;
    private float хаотичныйМножительX = 0f;
    private float хаотичныйМножительZ = 0f;
    private bool первоеПриземление = false;
    private float времяСоВзлёта = 0f;
    private float силаНаклонаВПрыжкеТекущая = 0f;
    
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
        
        // Инициализация нитро и текущих значений
        ИнициализироватьРасчетныеЗначения();
        
        // Ограничение начального запаса нитро
        ТекущийЗапасНитро = Mathf.Min(ТекущийЗапасНитро, МаксимальныйЗапасНитро);
        
        // Инициализация отладки
        предыдущаяСкорость = rb.linearVelocity.magnitude;
        времяСоВзлёта = Time.time;
    }

    void ИнициализироватьРасчетныеЗначения()
    {
        // Изначально текущие значения равны базовым
        текущаяМощностьДвигателя = БазоваяМощностьДвигателя;
        текущаяМаксимальнаяСкорость = БазоваяМаксимальнаяСкорость;
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

    float GetDriveMultiplier()
    {
        // Возвращаем множитель в зависимости от типа привода
        switch (типПривода)
        {
            case ТипПривода.ЗаднийПривод:
                return МножительЗаднегоПривода;
            case ТипПривода.ПереднийПривод:
                return 1f;
            case ТипПривода.ПолныйПривод:
                return 1f;
            default:
                return 1f;
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
        
        // Обработка нитро (передаем vertical для проверки движения вперед)
        HandleNitro(vertical);
        
        // Накат
        rb.linearDamping = (Mathf.Abs(vertical) < 0.1f && !handbrake) ? ТорможениеНакатом : 0f;
        
        // Отладка нитро (по нажатию N)
        DebugNitroInfo();
        
        // Расчет данных для отладки
        CalculateDebugData();
        
        // Обновление значений при изменении в инспекторе
        ОбновитьРасчетныеЗначения();
    }

    void ОбновитьРасчетныеЗначения()
    {
        // Если нитро не активно, обновляем текущие значения из базовых
        if (!нитроАктивно)
        {
            текущаяМощностьДвигателя = БазоваяМощностьДвигателя;
            текущаяМаксимальнаяСкорость = БазоваяМаксимальнаяСкорость;
        }
    }

    void CalculateDebugData()
    {
        // Расчет ускорения
        float текущаяСкорость = rb.linearVelocity.magnitude;
        текущееУскорение = (текущаяСкорость - предыдущаяСкорость) / Time.deltaTime;
        предыдущаяСкорость = текущаяСкорость;
        
        // Сохраняем последние скорости для расчета RPM
        последниеСкорости.Add(текущаяСкорость);
        if (последниеСкорости.Count > КоличествоОтсчетов)
        {
            последниеСкорости.RemoveAt(0);
        }
        
        // УПРОЩЕННЫЙ РАСЧЕТ: Находим максимальную силу тормоза на любом колесе
        максимальнаяСилаТормоза = 0f;
        foreach (var колесо in Колёса)
        {
            if (колесо.brakeTorque > максимальнаяСилаТормоза)
                максимальнаяСилаТормоза = колесо.brakeTorque;
        }
        
        // Расчет максимальных оборотов колес
        float maxRPM = 0;
        foreach (var колесо in Колёса)
        {
            if (колесо.rpm > maxRPM) maxRPM = колесо.rpm;
        }
        максимальныеОбороты = maxRPM;
    }

    void HandleSteering(float horizontalInput)
    {
        if (horizontalInput != 0)
        {
            // Динамический угол поворота в зависимости от скорости
            float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude * ВлияниеСкоростиНаПоворот);
            float dynamicSteerAngle = МаксимальныйУголПоворота * (1f - speedFactor * 0.5f);
            
            // Уменьшаем угол поворота в воздухе
            if (вВоздухе)
            {
                dynamicSteerAngle *= 0.3f;
            }
            
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
        
        // Получаем множитель для текущего типа привода
        float множительПривода = GetDriveMultiplier();
        
        // === 1. СБРОС ВСЕХ КРУТЯЩИХ МОМЕНТОВ ===
        foreach (var wheel in Колёса)
        {
            wheel.motorTorque = 0f;
            wheel.brakeTorque = 0f;
        }
        
        // === 2. ОБРАБОТКА РУЧНОГО ТОРМОЗА (имеет наивысший приоритет) ===
        if (handbrake)
        {
            // Применяем ручной тормоз на задних колёсах
            foreach (var wheel in rearWheels)
            {
                wheel.brakeTorque = СилаРучногоТормоза;
            }
            // Лёгкое торможение передних колёс для баланса
            foreach (var wheel in frontWheels)
            {
                wheel.brakeTorque = СилаРучногоТормоза * 0.3f;
            }
            return; // Ручной тормоз отменяет все другие режимы
        }
        
        // === 3. ДВИЖЕНИЕ ВПЕРЁД ===
        if (verticalInput > 0.1f)
        {
            // ПРОВЕРЯЕМ ОГРАНИЧЕНИЕ СКОРОСТИ
            if (currentSpeed < текущаяМаксимальнаяСкорость || forwardSpeed < 0)
            {
                float accelerationPower = Mathf.Pow(verticalInput, КриваяРазгона);
                
                // ПРИМЕНЯЕМ МНОЖИТЕЛЬ ПРИВОДА для заднего привода
                float итоговаяМощность = текущаяМощностьДвигателя * множительПривода;
                
                foreach (var wheel in driveWheels)
                {
                    wheel.motorTorque = accelerationPower * итоговаяМощность;
                }
            }
            
            // Если нажимаем газ вперёд, но движемся назад - дополнительное торможение
            if (isMovingBackward)
            {
                foreach (var wheel in Колёса)
                {
                    wheel.brakeTorque = СилаТормоза * 0.8f;
                }
            }
            return;
        }
        
        // === 4. ТОРМОЖЕНИЕ И ЗАДНИЙ ХОД (кнопка S) ===
        if (verticalInput < -0.1f)
        {
            // Определяем состояние для выбора режима
            float скоростьВперёдКмч = forwardSpeed * 3.6f; // В км/ч
            
            // Если движемся вперед со скоростью больше 5 км/ч - ТОРОМОЗИМ
            if (скоростьВперёдКмч > 5f)
            {
                // ПРОСТО ИСПОЛЬЗУЕМ ЗНАЧЕНИЕ ИЗ ИНСПЕКТОРА БЕЗ КОЭФФИЦИЕНТОВ
                float силаТорможения = СилаТормоза; // ПРЯМОЕ ПРИМЕНЕНИЕ ЗНАЧЕНИЯ ИЗ ИНСПЕКТОРА
                
                // ПРИМЕНЯЕМ ПОЛНУЮ СИЛУ НА ВСЕ КОЛЁСА
                foreach (var wheel in Колёса)
                {
                    wheel.brakeTorque = силаТорможения;
                }
            }
            // Если почти остановились (скорость < 5 км/ч) или движемся назад - ВКЛЮЧАЕМ ЗАДНИЙ ХОД
            else
            {
                float reverseInput = Mathf.Abs(verticalInput);
                float reversePower = Mathf.Pow(reverseInput, КриваяРазгона);
                
                // Моторный крутящий момент ТОЛЬКО на ведущих колёсах
                foreach (var wheel in driveWheels)
                {
                    wheel.motorTorque = -reversePower * МощностьЗаднегоХода;
                }
                
                // Ограничение скорости заднего хода
                if (isMovingBackward && Mathf.Abs(forwardSpeed) > МаксСкоростьНазад)
                {
                    foreach (var wheel in Колёса)
                    {
                        wheel.brakeTorque = СилаТормоза * 0.5f;
                    }
                }
                
                // Если пытаемся ехать назад, но все еще движемся вперед - дополнительное торможение
                if (скоростьВперёдКмч > 0.5f && скоростьВперёдКмч <= 5f)
                {
                    foreach (var wheel in Колёса)
                    {
                        wheel.brakeTorque = СилаТормоза * 0.8f;
                    }
                }
            }
            return;
        }
        
        // === 5. ОТПУЩЕНЫ ВСЕ КЛАВИШИ - НАКАТ С ЛЁГКИМ ТОРМОЖЕНИЕМ ===
        if (Mathf.Abs(verticalInput) < 0.1f)
        {
            // Лёгкое торможение для плавной остановки
            float brakeForce = 0f;
            
            if (currentSpeed > 1f) // Если скорость больше 1 м/с
            {
                if (isMovingForward)
                {
                    brakeForce = СилаТормоза * 0.05f; // Очень легкое торможение при движении вперед
                }
                else if (isMovingBackward)
                {
                    brakeForce = СилаТормоза * 0.1f; // Немного сильнее при движении назад
                }
            }
            
            if (brakeForce > 0)
            {
                foreach (var wheel in Колёса)
                {
                    wheel.brakeTorque = brakeForce;
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

    void HandleNitro(float verticalInput)
    {
        if (!ИспользоватьНитро) return;
        
        float currentSpeed = rb.linearVelocity.magnitude * 3.6f; // Переводим в км/ч для удобства
        bool isMovingForward = Vector3.Dot(rb.linearVelocity, transform.forward) > 0.1f;
        
        // Условия для активации нитро:
        bool canActivateNitro = Input.GetKey(КлавишаНитро) && 
                               ТекущийЗапасНитро > 0 && 
                               verticalInput > 0.1f && 
                               currentSpeed > МинимальнаяСкоростьДляНитро && 
                               isMovingForward;
        
        // Активация нитро
        if (canActivateNitro && !нитроАктивно)
        {
            нитроАктивно = true;
            
            // УВЕЛИЧИВАЕМ ТЕКУЩУЮ МОЩНОСТЬ ДВИГАТЕЛЯ для ускорения (не изменяем базовые значения)
            текущаяМощностьДвигателя = БазоваяМощностьДвигателя * МножительМощностиНитро;
            
            // Увеличиваем текущую максимальную скорость
            текущаяМаксимальнаяСкорость = БазоваяМаксимальнаяСкорость * МножительСкоростиНитро;
            
            // Включить звук нитро если есть
            if (ЗвукНитро != null && !ЗвукНитро.isPlaying)
            {
                ЗвукНитро.Play();
            }
        }
        
        // Деактивация нитро
        bool shouldDeactivate = нитроАктивно && (!canActivateNitro || ТекущийЗапасНитро <= 0);
        if (shouldDeactivate)
        {
            нитроАктивно = false;
            
            // ВОЗВРАЩАЕМ ТЕКУЩИЕ ЗНАЧЕНИЯ К БАЗОВЫМ (не изменяем базовые значения)
            текущаяМощностьДвигателя = БазоваяМощностьДвигателя;
            текущаяМаксимальнаяСкорость = БазоваяМаксимальнаяСкорость;
            
            // Выключить звук нитро
            if (ЗвукНитро != null && ЗвукНитро.isPlaying)
            {
                ЗвукНитро.Stop();
            }
        }
        
        // Расход нитро (только когда активно)
        if (нитроАктивно)
        {
            ТекущийЗапасНитро -= Time.deltaTime * (СкоростьРасходаНитро / 100f) * 100f;
            ТекущийЗапасНитро = Mathf.Max(0, ТекущийЗапасНитро);
        }
        // Восстановление нитро (когда не активно)
        else if (ТекущийЗапасНитро < МаксимальныйЗапасНитро)
        {
            ТекущийЗапасНитро += Time.deltaTime * (СкоростьВосстановленияНитро / 100f) * 100f;
            ТекущийЗапасНитро = Mathf.Min(ТекущийЗапасНитро, МаксимальныйЗапасНитро);
        }
    }

    void HandleJumpPhysics()
    {
        // Проверяем сколько колёс на земле и какие именно
        колёсНаЗемле = 0;
        int переднихНаЗемле = 0;
        int заднихНаЗемле = 0;
        
        foreach (var колесо in Колёса)
        {
            WheelHit hit;
            if (колесо.GetGroundHit(out hit))
            {
                колёсНаЗемле++;
                
                // Определяем переднее или заднее колесо
                if (System.Array.IndexOf(frontWheels, колесо) >= 0)
                {
                    переднихНаЗемле++;
                }
                else if (System.Array.IndexOf(rearWheels, колесо) >= 0)
                {
                    заднихНаЗемле++;
                }
                
                // Обработка кочек - добавляем небольшую вертикальную силу
                if (hit.force > 800f && ЧувствительностьККочкам > 0)
                {
                    // Определяем силу в зависимости от скорости
                    float скоростьКоэффициент = Mathf.Clamp01(rb.linearVelocity.magnitude / 30f);
                    float силаОтКочки = hit.force * ЧувствительностьККочкам * 0.0008f * скоростьКоэффициент;
                    силаОтКочки += СилаПодбрасыванияОтКочек * скоростьКоэффициент;
                    
                    // Применяем силу вверх
                    rb.AddForce(Vector3.up * силаОтКочки, ForceMode.Impulse);
                    
                    // Немного раскачиваем машину
                    float раскачка = Random.Range(-0.5f, 0.5f) * силаОтКочки * 0.1f;
                    rb.AddTorque(transform.right * раскачка, ForceMode.Impulse);
                }
            }
        }
        
        // Определяем, в воздухе ли машина
        bool былаВВоздухе = вВоздухе;
        вВоздухе = колёсНаЗемле < 2; // Если менее 2 колёс на земле - считаем в воздухе
        
        // Если машина только что взлетела
        if (!былаВВоздухе && вВоздухе)
        {
            времяВВоздухе = 0f;
            времяСоВзлёта = Time.time;
            начальнаяСкоростьПрыжка = rb.linearVelocity;
            начальныйУголПоворота = transform.eulerAngles;
            
            // Генерируем хаотичные множители для раскачки
            хаотичныйМножительX = Random.Range(-ХаотичнаяСилаРаскачки, ХаотичнаяСилаРаскачки);
            хаотичныйМножительZ = Random.Range(-ХаотичнаяСилаРаскачки, ХаотичнаяСилаРаскачки);
            
            // Начинаем с нулевой силы наклона
            силаНаклонаВПрыжкеТекущая = 0f;
            первоеПриземление = false;
        }
        
        // Если машина в воздухе
        if (вВоздухе)
        {
            времяВВоздухе += Time.fixedDeltaTime;
            
            // ПЛАВНОЕ НАРАСТАНИЕ СИЛЫ НАКЛОНА (вместо линейного)
            if (времяВВоздухе > ЗадержкаНаклона)
            {
                // Сила наклона быстро растет в начале, потом замедляется
                float прогресс = Mathf.Clamp01((времяВВоздухе - ЗадержкаНаклона) / 2f);
                силаНаклонаВПрыжкеТекущая = Mathf.Lerp(0f, СилаНаклонаВПрыжке, прогресс * прогресс); // Квадратичный рост
                
                // Добавляем хаотичность
                float хаотичность = Mathf.PerlinNoise(Time.time * 0.5f, времяВВоздухе * 0.3f) * 2f - 1f;
                
                // Автоматический наклон вперед с хаотичной составляющей
                float целевойУголX = Mathf.Clamp(
                    силаНаклонаВПрыжкеТекущая * времяВВоздухе * 0.5f + 
                    хаотичныйМножительX * хаотичность * 0.3f, 
                    -МаксимальныйУголНаклона, МаксимальныйУголНаклона
                );
                
                // Раскачка в поворотах с хаотичной составляющей
                float горизонталь = Input.GetAxis("Horizontal");
                float целевойУголZ = -горизонталь * ХаотичнаяСилаРаскачки * времяВВоздухе * 0.3f + 
                                    хаотичныйМножительZ * хаотичность * 0.2f;
                целевойУголZ = Mathf.Clamp(целевойУголZ, -МаксимальныйУголНаклона * 0.7f, МаксимальныйУголНаклона * 0.7f);
                
                // Плавный поворот к целевому углу
                Quaternion целевойПоворот = Quaternion.Euler(
                    целевойУголX,
                    transform.eulerAngles.y,
                    transform.eulerAngles.z + целевойУголZ
                );
                
                // Используем SmoothDamp для более плавного вращения
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    целевойПоворот, 
                    СкоростьНаклона * 50f * Time.fixedDeltaTime * (1f + прогресс * 2f) // Быстрее в начале
                );
                
                // Дополнительное хаотичное вращение на основе скорости
                if (rb.linearVelocity.magnitude > 10f)
                {
                    float скоростьВлияние = rb.linearVelocity.magnitude / 50f;
                    Vector3 хаотичноеВращение = new Vector3(
                        Random.Range(-0.5f, 0.5f) * хаотичность * скоростьВлияние,
                        Random.Range(-0.2f, 0.2f) * хаотичность * скоростьВлияние,
                        Random.Range(-0.5f, 0.5f) * хаотичность * скоростьВлияние
                    );
                    rb.AddTorque(хаотичноеВращение, ForceMode.Force);
                }
            }
            
            // Демпфирование вращения в воздухе
            rb.angularVelocity *= (1f - ДемпфированиеВВоздухе * Time.fixedDeltaTime);
            
            // Легкое влияние ввода игрока на вращение в воздухе
            float вертикаль = Input.GetAxis("Vertical");
            if (Mathf.Abs(вертикаль) > 0.1f)
            {
                float влияниеРуля = вертикаль * времяВВоздухе * 5f;
                rb.AddTorque(transform.right * влияниеРуля * 0.1f, ForceMode.Force);
            }
        }
        else if (былаВВоздухе && !вВоздухе)
        {
            // ПРИЗЕМЛЕНИЕ - сложная логика отскоков
            float времяВПолёте = Time.time - времяСоВзлёта;
            float силаПриземления = Mathf.Clamp01(времяВПолёте * 0.5f);
            
            // Определяем тип приземления
            if (переднихНаЗемле >= 2 && заднихНаЗемле < 2)
            {
                // Приземление на передние колёса - подбрасываем зад
                ПрименитьОтскокЗаднейЧасти(силаПриземления);
            }
            else if (заднихНаЗемле >= 2 && переднихНаЗемле < 2)
            {
                // Приземление на задние колёса - подбрасываем перед
                ПрименитьОтскокПереднейЧасти(силаПриземления);
            }
            else if (переднихНаЗемле >= 1 && заднихНаЗемле >= 1)
            {
                // Приземление на несколько колёс - общий отскок
                ПрименитьОбщийОтскок(силаПриземления);
            }
            
            // Если это первое приземление после долгого полета
            if (!первоеПриземление && времяВПолёте > 0.5f)
            {
                первоеПриземление = true;
                
                // Дополнительный хаотичный отскок для эффектности
                if (Random.value > 0.3f)
                {
                    float дополнительныйОтскок = Random.Range(0.5f, 1.5f) * силаПриземления;
                    rb.AddForce(Vector3.up * дополнительныйОтскок * 3f, ForceMode.Impulse);
                    
                    // Немного раскачиваем
                    float раскачка = Random.Range(-1f, 1f) * дополнительныйОтскок;
                    rb.AddTorque(transform.right * раскачка, ForceMode.Impulse);
                }
            }
            
            // Возвращаем машину в горизонтальное положение с эффектом пружины
            StartCoroutine(ВыровнятьМашинуПослеПриземления());
        }
    }

    void ПрименитьОтскокЗаднейЧасти(float сила)
    {
        // Подбрасываем заднюю часть машины
        Vector3 точкаПриложения = transform.position - transform.forward * 1.5f + transform.up * 0.5f;
        float силаОтскока = СилаОтскокаЗадних * сила;
        
        rb.AddForceAtPosition(Vector3.up * силаОтскока, точкаПриложения, ForceMode.Impulse);
        
        // Добавляем вращение, чтобы зад поднялся
        rb.AddTorque(transform.right * силаОтскока * 0.3f, ForceMode.Impulse);
        
        // Небольшое хаотичное вращение
        float хаос = Random.Range(-0.5f, 0.5f);
        rb.AddTorque(transform.forward * силаОтскока * хаос * 0.2f, ForceMode.Impulse);
    }

    void ПрименитьОтскокПереднейЧасти(float сила)
    {
        // Подбрасываем переднюю часть машины
        Vector3 точкаПриложения = transform.position + transform.forward * 1.5f + transform.up * 0.5f;
        float силаОтскока = СилаОтскокаПередних * сила;
        
        rb.AddForceAtPosition(Vector3.up * силаОтскока, точкаПриложения, ForceMode.Impulse);
        
        // Добавляем вращение, чтобы перед поднялся
        rb.AddTorque(-transform.right * силаОтскока * 0.3f, ForceMode.Impulse);
        
        // Небольшое хаотичное вращение
        float хаос = Random.Range(-0.5f, 0.5f);
        rb.AddTorque(transform.forward * силаОтскока * хаос * 0.2f, ForceMode.Impulse);
    }

    void ПрименитьОбщийОтскок(float сила)
    {
        // Общий отскок всей машины
        float силаОтскока = СилаОтскока * сила;
        rb.AddForce(Vector3.up * силаОтскока, ForceMode.Impulse);
        
        // Хаотичное вращение при отскоке
        float хаосX = Random.Range(-0.3f, 0.3f);
        float хаосZ = Random.Range(-0.3f, 0.3f);
        
        rb.AddTorque(transform.right * силаОтскока * хаосX * 0.5f, ForceMode.Impulse);
        rb.AddTorque(transform.forward * силаОтскока * хаосZ * 0.5f, ForceMode.Impulse);
    }

    System.Collections.IEnumerator ВыровнятьМашинуПослеПриземления()
    {
        float времяВыравнивания = 0.5f;
        float elapsed = 0f;
        
        Vector3 целевыеУглы = new Vector3(0, transform.eulerAngles.y, transform.eulerAngles.z);
        Quaternion целевойПоворот = Quaternion.Euler(целевыеУглы);
        
        while (elapsed < времяВыравнивания)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / времяВыравнивания;
            
            // Используем более плавную кривую для выравнивания
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Плавно выравниваем только наклон вперед/назад
            float текущийУголX = transform.eulerAngles.x;
            if (текущийУголX > 180) текущийУголX -= 360;
            
            float новыйУголX = Mathf.Lerp(текущийУголX, 0f, smoothProgress * 0.8f);
            
            transform.rotation = Quaternion.Euler(
                новыйУголX,
                transform.eulerAngles.y,
                transform.eulerAngles.z
            );
            
            yield return null;
        }
    }

    void DebugNitroInfo()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            float nitroPercent = (ТекущийЗапасНитро / МаксимальныйЗапасНитро) * 100f;
            
            Debug.Log($"╔═══════════════════════════════════════╗");
            Debug.Log($"║          ИНФОРМАЦИЯ О НИТРО           ║");
            Debug.Log($"╠═══════════════════════════════════════╣");
            Debug.Log($"║ Статус: {(нитроАктивно ? "АКТИВНО" : "НЕ АКТИВНО")}");
            Debug.Log($"║ Запас: {ТекущийЗапасНитро:F1}% / {МаксимальныйЗапасНитро:F1}%");
            Debug.Log($"║ Запас в процентах: {nitroPercent:F1}%");
            Debug.Log($"╠═══════════════════════════════════════╣");
            Debug.Log($"║ БАЗОВАЯ мощность: {БазоваяМощностьДвигателя:F0}");
            Debug.Log($"║ ТЕКУЩАЯ мощность: {текущаяМощностьДвигателя:F0}");
            Debug.Log($"║ БАЗОВАЯ макс. скорость: {БазоваяМаксимальнаяСкорость:F1}");
            Debug.Log($"║ ТЕКУЩАЯ макс. скорость: {текущаяМаксимальнаяСкорость:F1}");
            Debug.Log($"║ Множитель мощности: x{МножительМощностиНитро}");
            Debug.Log($"║ Множитель скорости: x{МножительСкоростиНитро}");
            Debug.Log($"╠═══════════════════════════════════════╣");
            Debug.Log($"║         ПАРАМЕТРЫ ДВИЖЕНИЯ            ║");
            Debug.Log($"╠═══════════════════════════════════════╣");
            Debug.Log($"║ Скорость: {rb.linearVelocity.magnitude * 3.6f:F1} км/ч");
            Debug.Log($"║ Ускорение: {текущееУскорение:F2} м/с²");
            Debug.Log($"║ Торможение: {максимальнаяСилаТормоза:F0} Н·м");
            Debug.Log($"║ Обороты двигателя: {максимальныеОбороты:F0} RPM");
            Debug.Log($"║ В воздухе: {вВоздухе} | Колёс на земле: {колёсНаЗемле}");
            Debug.Log($"╚═══════════════════════════════════════╝");
        }
    }

    void FixedUpdate()
    {
        // Стабилизация против опрокидывания (только на земле)
        if (!вВоздухе)
        {
            StabilizeCar();
        }
        
        // Обработка физики прыжков
        HandleJumpPhysics();
    }

    void StabilizeCar()
    {
        if (колёсНаЗемле > 1)
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
    }

    void OnValidate()
    {
        if (Application.isPlaying && Колёса != null && Колёса.Length > 0)
        {
            ConfigureWheelColliders();
            CacheDefaultFrictionSettings();
            
            // Обновление нитро
            if (ИспользоватьНитро)
            {
                ТекущийЗапасНитро = Mathf.Min(ТекущийЗапасНитро, МаксимальныйЗапасНитро);
                
                // Обновляем расчетные значения
                ОбновитьРасчетныеЗначения();
            }
        }
    }

    void OnGUI()
    {
        // Стиль для текста с обводкой
        GUIStyle outlinedStyle = new GUIStyle(GUI.skin.label);
        outlinedStyle.fontStyle = FontStyle.Bold;
        outlinedStyle.fontSize = 12;
        
        // Позиции для отображения
        float startX = 10;
        float startY = 10;
        float lineHeight = 22;
        
        if (ИспользоватьНитро)
        {
            float nitroPercent = (ТекущийЗапасНитро / МаксимальныйЗапасНитро) * 100f;
            
            // НИТРО с обводкой
            DrawOutlinedLabel(new Rect(startX, startY, 200, 25), 
                $"НИТРО: {nitroPercent:F0}% {(нитроАктивно ? "[АКТИВНО]" : "")}", 
                нитроАктивно ? Color.red : Color.cyan, Color.black, outlinedStyle);
            
            // Полоска нитро
            startY += lineHeight;
            GUI.Box(new Rect(startX, startY, 200, 10), "");
            GUI.color = нитроАктивно ? Color.red : Color.cyan;
            GUI.Box(new Rect(startX, startY, 200 * (nitroPercent / 100f), 10), "");
            GUI.color = Color.white;
            
            startY += 15; // Отступ после полоски
        }
        
        // Отступ для следующих параметров
        startY += 5;
        
        // Скорость
        DrawOutlinedLabel(new Rect(startX, startY, 200, 25), 
            $"Скорость: {rb.linearVelocity.magnitude * 3.6f:F1} км/ч", 
            Color.white, Color.black, outlinedStyle);
        
        // Ускорение
        startY += lineHeight;
        Color accelerationColor = текущееУскорение > 0 ? Color.green : (текущееУскорение < 0 ? Color.red : Color.white);
        DrawOutlinedLabel(new Rect(startX, startY, 200, 25), 
            $"Ускорение: {текущееУскорение:F2} м/с²", 
            accelerationColor, Color.black, outlinedStyle);
        
        // Торможение (максимальная сила на колесе)
        startY += lineHeight;
        Color brakeColor = максимальнаяСилаТормоза > 0 ? Color.yellow : Color.white;
        DrawOutlinedLabel(new Rect(startX, startY, 250, 25), 
            $"Торможение: {максимальнаяСилаТормоза:F0} Н·м", 
            brakeColor, Color.black, outlinedStyle);
        
        // Обороты двигателя
        startY += lineHeight;
        float rpmPercent = Mathf.Clamp01(максимальныеОбороты / 1000f);
        Color rpmColor = Color.Lerp(Color.white, Color.red, rpmPercent);
        DrawOutlinedLabel(new Rect(startX, startY, 200, 25), 
            $"Обороты: {максимальныеОбороты:F0} RPM", 
            rpmColor, Color.black, outlinedStyle);
        
        // Статус прыжка
        startY += lineHeight;
        Color jumpColor = вВоздухе ? Color.yellow : Color.white;
        DrawOutlinedLabel(new Rect(startX, startY, 250, 25), 
            $"Статус: {(вВоздухе ? $"В ВОЗДУХЕ ({времяВВоздухе:F1}с)" : "НА ЗЕМЛЕ")}", 
            jumpColor, Color.black, outlinedStyle);
        
        if (вВоздухе)
        {
            startY += lineHeight;
            DrawOutlinedLabel(new Rect(startX, startY, 250, 25), 
                $"Наклон: {силаНаклонаВПрыжкеТекущая:F1}%", 
                Color.cyan, Color.black, outlinedStyle);
        }
        
        // Дополнительная информация (только при активном нитро)
        if (нитроАктивно)
        {
            startY += lineHeight + 5;
            DrawOutlinedLabel(new Rect(startX, startY, 250, 25), 
                $"Мощность: +{МножительМощностиНитро:F1}x | Скорость: +{МножительСкоростиНитро:F1}x", 
                Color.red, Color.black, outlinedStyle);
        }
    }

    void DrawOutlinedLabel(Rect rect, string text, Color textColor, Color outlineColor, GUIStyle style)
    {
        // Сохраняем оригинальный цвет
        Color originalColor = GUI.color;
        
        // Рисуем обводку (4 раза со смещением)
        style.normal.textColor = outlineColor;
        
        GUI.Label(new Rect(rect.x - 1, rect.y - 1, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + 1, rect.y - 1, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - 1, rect.y + 1, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
        
        // Рисуем основной текст
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
        
        // Восстанавливаем цвет
        GUI.color = originalColor;
    }
}