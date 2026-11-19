---
paths:
  - "Assets/_Project/**"
---

# Архітектура: флов сцен і DI

## Флов сцен

```
Старт гри
  │  VContainerSettings (Assets/Resources, Preloaded Assets)
  ▼
RootLifetimeScope.prefab ─ GlobalLifetimeScope   (DontDestroyOnLoad, живе всю гру)
  │  глобальні сервіси: ISceneLoader
  ▼
Boot.unity                                        (індекс 0 у Build Settings)
  DI           ─ LifetimeScope, Auto Inject: ENTRY_POINT
  ENTRY_POINT  ─ AppEntryPoint
                   Construct(ISceneLoader) → Awake: targetFrameRate
                   → Start: LoadSceneAsync(SceneList.Gameplay)
  ▼
Gameplay.unity
  DI           ─ GameplayLifetimeScope, Auto Inject: ENTRY_POINT
  ENTRY_POINT  ─ GameplayEntryPoint
                   Start: по черзі будує рівень
```

- Сцени: `Assets/_Project/Scenes/`. Назви — константи в `SceneList`.
- У редакторі Play з будь-якої сцени стартує з Boot:
  `Develop/Editor/EntryPointSceneAutoLoader.cs`, меню `Tools/PlayFromBootstrap/Enabled`.
- Код: `Develop/Runtime/Infrastructure` (точки входу, `DI/` зі scope),
  `Develop/Runtime/Common` (спільні сервіси), `Develop/Runtime/Gameplay` (геймплей).

## Правила

- Кожна сцена має два корені: `DI` (свій `LifetimeScope`) і `ENTRY_POINT` (MonoBehaviour точки входу).
  `ENTRY_POINT` додано в **Auto Inject Game Objects** scope сцени.
- Точка входу отримує залежності через `[Inject] Construct(...)` і нічого не робить сама —
  лише викликає сервіси/фабрики по черзі.
- `GlobalLifetimeScope` — тільки сервіси на всю гру. Усе геймплейне — у `GameplayLifetimeScope`.
- Нова сцена: файл у `Scenes/`, константа в `SceneList`, запис у Build Settings, пара `DI` + `ENTRY_POINT`.
- Об'єкти зі сцени створюються фабриками через `IObjectResolver.Instantiate`, щоб отримали інжект.
  Префаби для фабрик — у ScriptableObject-конфігу, не в `Resources.Load`.
- Сервіси — звичайні C#-класи з інтерфейсом, реєструються в scope. MonoBehaviour — це View.
- Стан окремо від інпуту: інпут лише викликає методи систем (готовність до мережі).
- Фіча — папка-модуль прямо в `Gameplay/` (без `Features/`), може мати під-фічі (`Firefighter/Body`, `Firefighter/Camera`).
  Кожна сутність, яку спавнимо, має свою фабрику в `Factory/` поруч. Окремих інсталерів модулів немає —
  усе реєструється в `GameplayLifetimeScope`. Конфіги — лише в `Data/Configs`.
- Інпут модуля читає його координатор (`FirefighterCoordinator`) і передає в тіло/камеру; окремих InputHandler-ів немає.
- Простір імен `...Firefighter.Camera` перекриває `UnityEngine.Camera` — у модулі пишемо повне ім'я.
- Від першої особи: тіло пожежника на шарі `FirstPersonHidden` (виключений з culling mask камери гравця), руки — окремий меш на `Default`.
  Після переімпорту `Firefighter.fbx` запусти `Tools/Firefighter/Split First Person Mesh`.
- Інтеракція: усе, з чим взаємодіє гравець, реалізує `IInteractable` (`Gameplay/Interaction`). `InteractionScanner` щокадру
  шукає ціль променем з камери через `IRaycastService`; координатор на E викликає `Interact()`. Гравець — на шарі `Player`,
  який виключено з маски `InteractionConfig` (камера всередині капсули).
- Тримання предметів: `Grabber` (що в руках) + `GrabAnchor` (точка перед грудьми на тілі гравця, `Player.prefab`) + `IGrabbable` — у `Gameplay/Interaction`.
  Предмет з `IHandGrips` (точки хвату) координатор передає в `FirefighterHands`: власний `TwoBoneIk` (риг Generic, без Animation Rigging) у `LateUpdate` тягне руки до хвату поверх анімації.
  E без цілі кидає предмет. Предмет у руках може обмежувати рух тіла, якщо реалізує `IMovementConstraint` (`Firefighter/Body`);
  координатор додає/знімає обмеження за подіями `Grabber`.
- Подача води: модель у `Gameplay/WaterSupply/` — чистий C# без MonoBehaviour (`HydrantSource` → `SupplyLine` → `Pump` ← `WaterTank`,
  `WaterSupplyChain` тікає і збирає збої `SupplyFailure` з ініціатором для Blame). View (шланг, вентиль, HUD) лише викликають методи моделі.
- Шланги: спільне в `Gameplay/Hoses/` (`HoseRope` — verlet-фізика шланга з тертям об землю, `HoseTubeMesh`, `HoseCoupling`, `HoseSocket`, `HoseLeash`), конкретний шланг — під-фіча (`Hoses/Supply/`).
  Меш шланга будується у світових координатах: корінь префаба шланга стоїть у нулі.
- Струмінь води: `Gameplay/WaterStream/` — `WaterStreamView` (трасує балістичну дугу через `IRaycastService`, будує меш `WaterStreamMesh` з шейдером `Project/Water Stream`, керує бризками, туманом, сплеском і калюжею) знає лише `Flow` (0..1+, тиск); хто подає воду, задає `Flow`.
  Гідрант: `HydrantOutflow` вмикає струмінь, коли гідрант відкритий, а `SupplyLine` не під'єднана (сплеск на старті, спад тиску при закритті). Параметри — `WaterStreamConfig`.
  Струмінь штовхає те, у що влучив: `IWaterPushable` (тіло пожежника — швидкість поштовху з масою й тертям у `FirefighterConfig`), інакше `Rigidbody`. Гравець (шар `Player`) входить у `CollisionMask` струменя. Калюжа — `PuddleView` + `PuddleSimulation`: сітка глибини води на землі (меш облягає землю один раз через `TryRaycastFarthest` — найнижче влучання, щоб гідрант, машина й шланг не піднімали калюжу, вода стікає вниз, тонкий шар зупиняється, висихає), струмінь лише доливає воду в точку падіння; шейдер `Project/Water Puddle` читає глибину з текстури `_WetnessTex`, брижі — від `_ImpactPosition`.
- UI живе в `Gameplay/UI/`: View (MonoBehaviour) + Presenter (C#-клас, підписка в `Bind`, відписка в `Dispose`). HUD створює `HudFactory`.
- Логіка будинку (`HouseInterior.prefab`) — у модулі `Gameplay/House/` (двері — `House/Doors/`).
- Сповіщення між частинами — `event Action` на конкретних класах. Без глобального EventBus.
- Новий `*LifetimeScope.cs`: VContainer одразу перезаписує його шаблоном — перечитай файл
  після створення і запиши вміст ще раз (див. скіл `vcontainer`).
