# VContainer registration API reference

API facts verified against installed package `Library/PackageCache/jp.hadashikick.vcontainer@*`, version **1.19.0**.

## Lifetime types

| Lifetime | Description | Use case |
|----------|-------------|----------|
| `Singleton` | One instance in the registering scope; visible to child scopes | App-wide services in the root scope |
| `Scoped` | One instance per scope; child scopes get their own | Feature-specific services |
| `Transient` | New instance every resolve | Factory outputs, short-lived objects |

`Singleton` in a child scope lives only as long as that child — "singleton" means one per registering container, not app-global.

## Basic registration

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
    builder.Register<GameManager>(Lifetime.Singleton);

    builder.Register<AudioSystem>(Lifetime.Singleton)
        .AsImplementedInterfaces()
        .AsSelf();

    builder.RegisterInstance(existingConfig);
}
```

## Unity component registration

```csharp
builder.RegisterComponent(existingComponent);          // instance already in scene
builder.RegisterComponentInHierarchy<HudView>();       // scene-scoped — see the DDOL trap in SKILL.md
builder.RegisterComponentOnNewGameObject<Marker>(Lifetime.Scoped);
builder.RegisterComponentInNewPrefab(prefab, Lifetime.Scoped);
```

`RegisterComponentInHierarchy` searches only the scene the registering `LifetimeScope` belongs to (`FindComponentProvider` captures `lifetimeScope.gameObject.scene`). For a DDOL root that is the DDOL pseudo-scene — a component in a normally loaded scene will NOT be found. Register via factory instead:

```csharp
builder.Register(_ =>
    UnityEngine.Object.FindFirstObjectByType<HudView>(FindObjectsInactive.Include),
    Lifetime.Scoped);
```

## Factory registration

```csharp
// Func-factories (up to 4 params):
builder.RegisterFactory<Enemy>(() => new Enemy(), Lifetime.Scoped);
builder.RegisterFactory<string, IWeapon>(name => new Weapon(name), Lifetime.Transient);

// Hand-written factory over the resolver (use for cross-lifetime creation + injection):
public class EnemyFactory
{
    private readonly IObjectResolver _resolver;
    public EnemyFactory(IObjectResolver resolver) => _resolver = resolver;

    public Enemy Create(EnemyConfig config)
    {
        Enemy enemy = new Enemy(config);
        _resolver.Inject(enemy);
        return enemy;
    }
}
```

## Child scopes

```csharp
LifetimeScope child = parentScope.CreateChild(builder =>
{
    builder.Register<BattleState>(Lifetime.Scoped);
});

LifetimeScope child2 = parentScope.CreateChild(new BattleInstaller());
TScope fromPrefab = parentScope.CreateChildFromPrefab(scopePrefab, installer);

// Extra registrations for a scope about to be created/loaded:
using (LifetimeScope.Enqueue(builder => builder.RegisterInstance(payload)))
{
    // scene load or CreateChild here picks up the enqueued registrations
}

// Parent override for scopes loaded with a scene:
using (LifetimeScope.EnqueueParent(parentScope)) { /* load scene */ }
```

`CreateChild` sets `child.parentReference.Object`, so the hierarchy is explicit. Dispose the child on state exit; its `IDisposable` registrations dispose with it.

## Injection

```csharp
// Constructor injection — preferred, plain C# classes:
public PlayerController(IInputService input) => _input = input;

// Method injection — MonoBehaviour entry points only:
[Inject]
public void Construct(ITargetingService targeting) => _targeting = targeting;
```

Field/property `[Inject]` exists but is avoided in this codebase (see the bank's `dependency-injection.md`). `[InjectIgnore]` excludes a member.

## Testing

```csharp
LifetimeScope scope = LifetimeScope.Create(builder =>
{
    builder.RegisterInstance(mockInput).As<IInputService>();
    builder.Register<PlayerController>(Lifetime.Singleton);
});
// Create(IInstaller) and Create(Action<IContainerBuilder>) both exist. Dispose in TearDown.
```
