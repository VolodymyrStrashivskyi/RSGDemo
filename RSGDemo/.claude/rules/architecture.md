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
- Шланги: спільне в `Gameplay/Hoses/` (`HoseRope` — verlet-фізика шланга з тертям об землю, `HoseTubeMesh`, `HoseCoupling`, `HoseSocket`, `HoseLeash`), конкретний шланг — під-фіча (`Hoses/Supply/` — жовтий, `Hoses/Attack/` — червоний з котушки).
  Меш шланга будується у світових координатах: корінь префаба шланга стоїть у нулі.
  Червоний шланг: `AttackHose` — `HoseRope` зі змінною довжиною (`TryPayOut` додає сегмент біля котушки, поки шланг коротший за відстань до ствола + запас або натягнутий), `HoseNozzle` (IGrabbable + IHandGrips + IAimable) висить у кронштейні на `NozzleHookPoint` праворуч від ніші; у руках тіло тримає його біля грудей власними руками (`HoldOffset`/`AimPivot` у межах досяжності рук 29 см), ствол хитається за поглядом.
  Котушка — `HoseReel` на машині (`FireTruckView.HoseReel`): барабан зі спицями крутиться, шланг на ньому — спіраль шарами (`HoseTubeMesh` у координатах барабана, перебудова лише при зміні довжини); намотана довжина = `MaxLength` − довжина мотузки, точка сходу шланга (`TakeoffPosition`/`TakeoffDirection`) завжди спереду барабана і їде вздовж осі, поки шар змотується. Намальованих кілець у `FireTruck_Body.asset` (копія меша з FBX, він же колайдер) більше немає — після переімпорту `FireTruck.fbx` копію треба зробити знову.
  Колайдер машини — на шарі `Vehicle`: шланги (`GroundMask`) і калюжа його не бачать, щоб шланг не застрягав на даху ніші; струмінь (`CollisionMask`) і промінь взаємодії — бачать. `HoseRope` шукає землю лише під найближчою перешкодою над вузлом, щоб вузол не видирався драбинкою на нависаючі деталі.
  Зіткнення шланга: кожен вільний вузол — кулька завширшки зі шланг (`CollisionRadius`); `CollisionPasses` разів за крок рух вузла перевіряється кулькою (`TrySphereCast`) з ковзанням уздовж стіни, і вузол виштовхується з перешкод (`TryPushOutSphere`) за маскою `HoseRopeSettings.CollisionMask` (Default + Vehicle). Кульки стоять і у вузлах, і посередині кожного сегмента, тож шланг суцільний, без щілин для балясин і кутів. Поверхня, що дивиться вгору, блокує вузол лише тоді, коли він опинився б під нею (провалитися крізь перекриття не можна), а ковзання по ній — справа пошуку землі; низька перешкода (до 30 см) для вузла на землі — сходинка, на яку він заїжджає. Поки шланг несуть, `HoseTrail` записує шлях гравця по землі (перший запис — поточна форма шланга, повернення назад обрізає петлю лише в межах прямої видимості) і `HoseRope.Guide` м'яко тягне вузли вбік до цього шляху (`TrailPull`), тож шланг іде тими дверима й сходами, якими пройшов гравець. Колайдер будинку — порожнистий меш, тому раз на кілька кроків вузол перевіряє променями зі зворотними гранями, чи не опинився всередині стіни чи перекриття, і виходить через найближчу грань (`TryEscapeHollow`). Котушка віддає шланг, коли не вистачає довжини або розтяг перевищив `PayOutStretchRatio`, не швидше `MaxPayOutSpeed`; повідець не пускає далі повної довжини шланга по прямій від котушки, а коли віддавати вже нічого (котушка порожня або шланг фіксованої довжини) і розтяг перевищив `LeashStretchRatio`, забороняє лише рух прямо від точки повороту — останнього вузла, що обгинає перешкоду (`FindLeashPivot`), або котушки; убік і назад гравець іде вільно, як на повідку. Точки хвату: `up` — тильний бік кисті, `forward` — напрям пальців.
  Від першої особи — як у шутерах: справжні руки (29 см) не дістають до кадру, тому під камерою є окремі `FirstPersonArms` (копія ригу з мешем рук, ×1.5, плечі розведені) зі своїм `FirefighterHands`, видимі лише коли щось у руках.
  Предмет з `IFirstPersonHeld` (ствол, жовта муфта) дає свою копію для камери — `FirstPersonViewModel` (меш, точки хвату, поза перед камерою); координатор передає тілу хват справжнього предмета, а камері — копію (`FirstPersonCamera.Hold`). Без `IFirstPersonHeld` обидві пари рук беруть той самий хват.
  Шланг від першої особи: `HoseView` будує другу копію меша (шар `FirstPersonOnly`), кінець якої плавно (`_firstPersonBlendLength`) підведено до `FirstPersonViewModel.HoseEnd`; справжній шланг на цей час іде на `FirstPersonHidden`. `LateTick` VContainer іде перед `LateUpdate` MonoBehaviour, тож камера вже на місці, коли шланг будується.
  Шари: `FirstPersonOnly` (руки й копії для камери) малює лише камера гравця, у вікні Scene його сховано; справжній предмет і руки тіла на час тримання переходять на `FirstPersonHidden`, тож камера гравця їх не бачить, а інші камери бачать тіло, що тримає предмет.
- Струмінь води: `Gameplay/WaterStream/` — `WaterStreamView` (трасує балістичну дугу через `IRaycastService`, будує меш `WaterStreamMesh` з шейдером `Project/Water Stream`, керує бризками, туманом, сплеском і калюжею) знає лише `Flow` (0..1+, тиск); хто подає воду, задає `Flow`.
  Гідрант: `HydrantOutflow` вмикає струмінь, коли гідрант відкритий, а `SupplyLine` не під'єднана (сплеск на старті, спад тиску при закритті). Параметри — `WaterStreamConfig`.
  Струмінь штовхає те, у що влучив: `IWaterPushable` (тіло пожежника — швидкість поштовху з масою й тертям у `FirefighterConfig`), інакше `Rigidbody`. Гравець (шар `Player`) входить у `CollisionMask` струменя. Калюжа — `PuddleView` + `PuddleSimulation`: сітка глибини води на землі (меш облягає землю один раз через `TryRaycastFarthest` — найнижче влучання, щоб гідрант, машина й шланг не піднімали калюжу, вода стікає вниз, тонкий шар зупиняється, висихає), струмінь лише доливає воду в точку падіння; шейдер `Project/Water Puddle` читає глибину з текстури `_WetnessTex`, брижі — від `_ImpactPosition`.
- UI живе в `Gameplay/UI/`: View (MonoBehaviour) + Presenter (C#-клас, підписка в `Bind`, відписка в `Dispose`). HUD створює `HudFactory`.
- Логіка будинку (`HouseInterior.prefab`) — у модулі `Gameplay/House/` (двері — `House/Doors/`).
- Сповіщення між частинами — `event Action` на конкретних класах. Без глобального EventBus.
- Новий `*LifetimeScope.cs`: VContainer одразу перезаписує його шаблоном — перечитай файл
  після створення і запиши вміст ще раз (див. скіл `vcontainer`).
