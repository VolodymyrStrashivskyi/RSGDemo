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
- Сповіщення між частинами — `event Action` на конкретних класах. Без глобального EventBus.
- Новий `*LifetimeScope.cs`: VContainer одразу перезаписує його шаблоном — перечитай файл
  після створення і запиши вміст ще раз (див. скіл `vcontainer`).
