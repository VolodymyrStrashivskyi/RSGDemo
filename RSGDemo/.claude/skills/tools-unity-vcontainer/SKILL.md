---
name: tools-unity-vcontainer
description: >
  VContainer dependency injection in this Unity project. Use when wiring DI, adding a
  LifetimeScope, registering, resolving, or injecting services (root or child scope),
  RegisterEntryPoint, or debugging "VContainerException", "Unable to resolve", "is not in
  this scene DontDestroyOnLoad", or a scope disposal crash. Out of scope: the DI architecture
  rationale (why constructor injection, how to structure scopes) lives in the bank's
  dependency-injection.md.
---

# VContainer DI

API facts verified against installed package source
(`Library/PackageCache/jp.hadashikick.vcontainer@*`), version **1.19.0**.

Deep API is split out — read on demand:
- Registration, lifetimes, components, factories, child scopes, testing →
  [references/registration-api.md](references/registration-api.md)
- Entry points + the `IAsyncStartable` signature-by-define quirk →
  [references/entry-points.md](references/entry-points.md)

## Root scope in this project (delta — not documented)

DI is bootstrapped through `VContainerSettings`, not manual code:

- `Assets/_Project/Resources/RootScope.prefab` (component `RootScope : LifetimeScope`) is
  assigned to `Assets/VContainerSettings.asset`, which sits in
  **PlayerSettings > Preloaded Assets**. That preload is how the root loads at runtime —
  do not remove it.
- The root prefab is instantiated on the **first scene load** (if its `autoRun` is true,
  the default). `DontDestroyOnLoad` is applied by the package — no manual DDOL.
- Any `LifetimeScope` without an explicit parent **auto-parents to the settings root**,
  so Play mode works from any scene.

## Registering app services vs features

- Root scope (`RootScope.Configure`) registers **only app-wide services**. Feature/state
  services live in child scopes created via `CreateChild` (see registration reference).
- `Singleton` means one instance **per registering container**, not app-global — a
  `Singleton` in a child scope dies with that child.
- Dispose every child scope on state exit; its `IDisposable` registrations dispose with it.

## Debugging `VContainerException: Unable to resolve`

Beyond a plain missing registration: confirm the resolving scope is actually a child of
the registering one — check `parentReference` / the settings root. A scope that looks
nested in the hierarchy but auto-parented to the root won't see feature registrations.

## Avoid

- Don't constructor-inject a child-scoped service into a root `Singleton` — it dies with
  the child scope. Cross-lifetime access goes through a factory or `IObjectResolver`.
- Don't call `RegisterComponentInHierarchy` from a DDOL root scope — it searches only the
  scope's own scene (the DDOL pseudo-scene), so a component in a normal scene won't be
  found (`...is not in this scene DontDestroyOnLoad`). Register via a
  `FindFirstObjectByType` factory instead (see registration reference).
- Don't add manual `DontDestroyOnLoad` to the root — `VContainerSettings` instantiates and
  persists it; a manual copy yields two root scopes.
- Don't `Dispose()` a scope immediately after `Exit()` — let pending continuations finish
  first (e.g. await a frame), or resolving inside a pending callback crashes.
- Don't use field/property `[Inject]` — this codebase uses constructor injection, and
  `[Inject] Construct` for MonoBehaviours only (see the bank's dependency-injection.md).
- Don't hand-create `VContainerSettings.asset` — use the menu action, or it won't land in
  Preloaded Assets and the root never loads.
