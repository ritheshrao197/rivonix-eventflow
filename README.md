# Rivonix-EventFlow

**Controlled Event Processing, Pipelines, and State Management for Unity**

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen)](CONTRIBUTING.md)

---

## 📦 Overview

**Rivonix-EventFlow** is a controlled event processing system for Unity. Instead of sending events straight to listeners, EventFlow lets you validate, transform, inspect, and route events through a pipeline before dispatch. Built for performance and developer experience, it reduces spaghetti code while making runtime flow visible in tooling.

### Why EventFlow?

Unlike `UnityEvent` or a traditional event bus, EventFlow adds a pipeline layer between trigger and dispatch:

`Trigger -> Pipeline Steps -> Dispatch -> Listeners`

That makes it a better fit for real gameplay flows where you need:

- Validation before dispatch
- Event transformation or score modification
- Clear debugger visibility into execution order
- Safer, more controlled event processing

```csharp
// ❌ Without EventFlow - Tight coupling
player.health.OnDamage += ui.ShowDamage;
player.health.OnDamage += audio.PlayOuch;
player.health.OnDeath += achievementSystem.UnlockAchievement;

// ✅ With EventFlow - Clean decoupling
EventBus.Trigger(new PlayerDamagedEvent { damage = 10 });
// Any system can listen without the player knowing
