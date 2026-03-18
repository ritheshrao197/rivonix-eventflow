# Rivonix-EventFlow

**Professional Event System & State Management for Unity**

[![Unity Version](https://img.shields.io/badge/Unity-2019.4%2B-blue)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen)](CONTRIBUTING.md)

---

## 📦 Overview

**Rivonix-EventFlow** is a production-ready event system that revolutionizes how Unity games handle communication between components. Built for performance and developer experience, it eliminates spaghetti code and promotes clean, maintainable architecture.

### Why EventFlow?

```csharp
// ❌ Without EventFlow - Tight coupling
player.health.OnDamage += ui.ShowDamage;
player.health.OnDamage += audio.PlayOuch;
player.health.OnDeath += achievementSystem.UnlockAchievement;

// ✅ With EventFlow - Clean decoupling
EventBus.Trigger(new PlayerDamagedEvent { damage = 10 });
// Any system can listen without the player knowing
