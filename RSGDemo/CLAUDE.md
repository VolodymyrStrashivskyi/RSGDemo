# CLAUDE.md

Інструкції для Claude Code в цьому репозиторії. Спілкування й документи — **українською**.
Усе, що потрапляє в гру чи редактор, — **англійською**: тексти UI, `Tooltip`, `Header`, назви меню, логи й повідомлення про помилки.

## Контекст проєкту

Конкурсна демка для RetroStyle Games (гра «Flame and Blame»). Соло, дедлайн **27.11.2026, 23:59** (краще здати раніше).
Здаємо відео + playable demo + [AI_USAGE.md](AI_USAGE.md) (де допомагав AI — доповнювати після кожної фічі).
Журі оцінює архітектуру, взаємодію частин, розширюваність і «соковитість» фізики.
Вимога: **ОДНА механіка, яка працює як система**, а не окремий ефект.

### Гра
Вид від першої особи. Будинок горить (двір + 2–3 кімнати), поруч пожежна машина. Гравець витягує шланг і гасить будинок ззовні й зсередини.

```
Гідрант ══ жовтий ══► Машина (бак + насос) ── червоний ──► Ствол → струмінь
```

- **Жовтий шланг**: гідрант → машина. Без нього вода з бака (~60 с), з ним — безкінечна.
- **Червоний шланг**: з котушки машини, довжина обмежена; чіпляється, затискається, наступили — тиск падає.
- **Ствол**: тиск регулюється, є віддача (на максимумі відкидає). Струмінь гасить і штовхає предмети.
- **Вогонь** — проста ціль: горить / загашено, без складного поширення.
- **«Blame»**: кожен збій ланцюга фіксується з ініціатором → звіт «хто винен» у кінці раунду.

Пріоритети: 1) ланцюг подачі води; 2) фізика червоного шланга; 3) струмінь.

## Стек

Unity **6000.5.3f1**, URP, PC (Windows).

- **Рендер** — URP, рендерер `Assets/Settings/PC_Renderer.asset` із фічею Screen Space Ambient Occlusion (затінення кутів; сила й радіус — у налаштуваннях фічі)

- **DI** — VContainer 1.19.0 (скіл `vcontainer`)
- **Асинхронність** — UniTask 2.5.11, скрізь замість `Task` і корутин (скіл `unitask`)
- **Важкі обчислення** — Burst 1.8.29 + C# Job System (`IJobParallelFor`, `NativeArray`); зараз — 3D-текстура обвуглення будинку
- **Анімації кодом** — PrimeTween 1.5.2, scoped registry `npmjs.org`, скоуп `com.kyrylokuzyk` (скіл `primetween`)
- **UI-текст** — лише `TextMeshProUGUI` (`TMPro`, входить у `com.unity.ugui`); легасі `UnityEngine.UI.Text` не використовуємо. Ресурси TMP — `Assets/TextMesh Pro/`. Шрифт HUD — Barlow Condensed (OFL, `Art/Fonts/`, TMP-ассети `BarlowCondensed-Bold/SemiBold SDF`)
- **UI-спрайти** — генеровані в `Art/Textures/UI/` (`UI_RoundedRect` — 9-slice для панелей і смуг, `UI_PanelCut` — панель зі зрізаним кутом, `UI_HazardStripe` — тайлова смуга небезпеки, `UI_Segment` — сегмент смуги, `UI_Icon_*` — іконки (крапля, бак, манометр, попередження, вогонь, галочка); заповнення смуги — через `anchorMax`, не `fillAmount`)
- **Звук** — `IAudioService` (`Common/Audio/`, глобальний сервіс: короткі звуки з пулу джерел, петлі `SoundLoop` з гучністю, висотою, позицією й низькочастотним фільтром); кожен звук — асет `SoundCue` (`Configs/Audio/Cues/`: варіанти кліпів, розкид гучності й висоти, канал, відстань чутності), гучність каналів — `AudioConfig`. Кліпи — `Art/Audio/`, лише CC0 (джерела й автори — `Art/Audio/CREDITS.md`), підготовка — `Tools/Audio/build_audio.py` (ffmpeg + numpy/scipy)
- **Ввід** — новий Input System; `activeInputHandler` = New only, старий `UnityEngine.Input` кине виняток у рантаймі
- **Керування редактором** — Unity CLI + пакет `com.unity.pipeline`
- **Дані** — параметри механік (шланги, бак, тиск, вогонь) у ScriptableObject, без хардкоду

> Додаючи нову технологію в проєкт, допиши її сюди. Бачиш у коді технологію, якої тут немає, — спитай, чи її внести.

## Де живе код

`Assets/_Project/` — увесь власний код і ресурси. Решта під `Assets/` — пакети, плагіни й налаштування URP.

```
Assets/_Project/
├── Art/                          # Художні ассети
│   ├── Materials/
│   ├── Models/
│   ├── Shaders/
│   └── Textures/
├── Configs/                      # Екземпляри ScriptableObject (.asset)
├── Develop/                      # УВЕСЬ код проєкту
│   ├── Editor/                   # Editor-only (EntryPointSceneAutoLoader, розділення скла вікон); Fire/ — вікно генератора осередків вогню; Firefighter/ — збирання рук із рукавицями
│   └── Runtime/                  # Код, що потрапляє в білд
│       ├── Common/               # Спільні сервіси з інтерфейсами, реєструються в DI (SceneManagement/, Input/, Raycast/, Audio/)
│       ├── Data/Configs/         # Класи всіх ScriptableObject-конфігів (не в папках фіч)
│       ├── Gameplay/             # Фічі-модулі прямо тут, без папки Features/
│       │   ├── Firefighter/      # Модуль пожежника: координатор (інпут + синхронізація) у корені,
│       │   │                     #   під-фічі Body/, Camera/, Interaction/ (сканер цілей), у кожної своя Factory/
│       │   ├── Interaction/      # IInteractable, а також Grabber/GrabAnchor/IGrabbable — що гравець тримає в руках
│       │   ├── WaterSupply/      # Модель ланцюга води (чистий C#): гідрант → лінія → насос ← бак, збої з ініціатором
│       │   ├── Hoses/            # Спільне для шлангів (фізика мотузки, слід гравця, меш, муфта, сокет); Supply/ — жовтий шланг, Attack/ — червоний з котушки і ствол
│       │   ├── WaterStream/      # Візуал струменя води: балістична дуга, меш, бризки, сплеск, калюжі (гідрант і ствол); IWaterImpacts — куди влучила вода
│       │   ├── Fire/             # Модель вогню (чистий C#): осередки FireSpot, FireSystem — поширення, гасіння літрами, повторний спалах, згорання; обвуглення через шейдер Burnable
│       │   ├── House/Doors/      # Модуль будинку; двері (IInteractable)
│       │   ├── UI/Hud/           # Canvas, приціл (View + Presenter), Factory/
│       │   ├── FireTruck/, Hydrant/, Street/   # Кожна з Factory/
│       │   └── Level/            # WorldTag (точки спавну)
│       ├── Infrastructure/       # Точки входу; DI/ — LifetimeScope-и
│       └── Utils/                # Статичні розширення без стану (`Smoothing` — згладжування незалежно від FPS, `VectorExtensions` — вектори без висоти)
├── Prefabs/                      # RootLifetimeScope.prefab та префаби гри
├── Resources/                    # Лише те, що вантажиться синхронно на старті
└── Scenes/                       # Boot, Gameplay
Assets/Resources/VContainerSettings.asset   # вказує на RootLifetimeScope.prefab
```

## Рантайм-пайплайн

`VContainerSettings` → `RootLifetimeScope.prefab` (`GlobalLifetimeScope`, живе всю гру) → **Boot** (`AppEntryPoint`: прогрів, завантаження Gameplay) → **Gameplay** (`GameplayEntryPoint`: по черзі будує рівень).
Повна схема і правила DI — [.claude/rules/architecture.md](.claude/rules/architecture.md). Прочитай перед змінами сцен чи DI.

## Перевірка в Unity

Компілює тільки відкритий Unity Editor, керуємо ним через Unity CLI. Не запускати `dotnet build` і batchmode. Після створення чи зміни скриптів:

1. `unity command editor invoke_menu --path "Assets/Refresh"`
2. дочекатися `unity command script recompile_status` → `status: completed`
3. `unity command console get --level error` — виправити помилки, перш ніж іти далі

Якщо Unity CLI не підключений (`unity pipeline editors`), кажи, що код не перевірено.

## Індекс правил `.claude/rules/`

Правила підвантажуються самі за `paths`. **Додаючи чи перейменовуючи файл у `.claude/rules/`, онови цей індекс.**

| Файл | Коли підвантажується | Про що |
|---|---|---|
| [code-style.md](.claude/rules/code-style.md) | `**/*.cs` | Структура класу, іменування (`_camelCase`, `PascalCase`, `UPPER_SNAKE_CASE`), форматування, Unity, async |
| [architecture.md](.claude/rules/architecture.md) | `Assets/_Project/**` | Флов сцен Boot → Gameplay, scope-и VContainer, точки входу, фабрики, події |

Скіли (`.claude/skills/`): `vcontainer`, `unitask`, `primetween` — довідка по API, підтягуються самі; `/review` — рев'ю змін, лише вручну.

## Git

- Корінь репозиторію — `D:\UnityProjects\RSGDemo`, Unity-проєкт лежить у підпапці `RSGDemo/`.
- Повідомлення комітів — **українською**.
- `*.csproj` і `*.sln` не комітимо (Unity їх перегенеровує). `Library/`, `Temp/`, `Logs/`, `UserSettings/` — уже в `.gitignore`.
