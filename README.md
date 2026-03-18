# Rivonix EventFlow

Production-ready event pipeline architecture for Unity

Rivonix EventFlow is a modular, type-safe event system designed to eliminate tightly coupled code and introduce structured event processing pipelines in Unity.

It goes beyond traditional event buses by allowing events to be validated, transformed, and controlled before they are dispatched, making large-scale systems easier to build, debug, and maintain.

## Why EventFlow?

Most Unity projects evolve into:

- tightly coupled systems
- implicit dependencies
- hard-to-debug event chains

Traditional approaches like `UnityEvent` or basic event buses only solve communication, not control.

EventFlow introduces a pipeline layer, giving you full control over how events are processed.

## Core Concept

Instead of:

`Event -> Listeners`

EventFlow enables:

`Event -> Pipeline -> Validation -> Transformation -> Dispatch -> Listeners`

This allows:

- filtering invalid events
- modifying event data safely
- controlling execution flow
- improving debugging visibility

## Key Features

### Type-Safe Event System

- Struct-based events with minimal GC pressure
- Compile-time safety with no string-based events

### Event Pipelines (Core Feature)

- Define ordered processing steps per event
- Validate, transform, or stop events before dispatch
- Priority-based execution

### Clean Public API

```csharp
EventFlow.Trigger(eventData);
EventFlow.Register<MyEvent>(OnEvent);
EventFlow.AddStep<MyEvent>(ProcessStep);
```

### State-Aware Execution

- Restrict events to specific game states
- Prevent invalid transitions and edge cases

### Built-in Scheduling

- Trigger delayed or repeated events
- Replace coroutines for event timing

### Debug & Observability Tools

- Real-time event tracking
- Pipeline visualization
- Listener inspection

### Lightweight & Dependency-Free

- Pure C# implementation
- No external frameworks
- Drop-in ready

## Quick Start

### 1. Define an Event

```csharp
public struct PlayerScoredEvent : IEvent
{
    public int points;
}
```

### 2. Add Pipeline Steps

```csharp
EventFlow.AddStep<PlayerScoredEvent>(ValidateScore);
EventFlow.AddStep<PlayerScoredEvent>(ApplyMultiplier);

FlowResult ValidateScore(ref PlayerScoredEvent e)
{
    if (e.points <= 0)
        return FlowResult.Stop;

    return FlowResult.Continue;
}

FlowResult ApplyMultiplier(ref PlayerScoredEvent e)
{
    e.points *= 2;
    return FlowResult.Continue;
}
```

### 3. Listen for Events

```csharp
void OnEnable()
{
    EventFlow.Register<PlayerScoredEvent>(OnScore);
}

void OnDisable()
{
    EventFlow.Unregister<PlayerScoredEvent>(OnScore);
}

void OnScore(PlayerScoredEvent e)
{
    Debug.Log($"Final Score: {e.points}");
}
```

### 4. Trigger Event

```csharp
EventFlow.Trigger(new PlayerScoredEvent { points = 100 });
```

## Example Flow

```text
PlayerScoredEvent
 [1] ValidateScore
 [2] ApplyMultiplier
 [3] ClampScore
 -> Dispatched to 2 listeners
```

## When to Use EventFlow

Use EventFlow when:

- building scalable gameplay systems
- decoupling UI, gameplay, and audio
- managing complex event chains
- you need control over event execution

## When Not to Use

Avoid for:

- simple one-to-one communication
- extremely performance-critical direct calls

## Architecture Overview

```text
EventFlow (Facade)
        ↓
EventFlowController (Pipeline Execution)
        ↓
EventBus (Dispatch)
        ↓
Listeners
```

## Comparison

| Feature | UnityEvent | Basic Event Bus | EventFlow |
| --- | --- | --- | --- |
| Type Safety | No | Yes | Yes |
| Pipeline Control | No | No | Yes |
| Debug Visibility | No | Partial | Yes |
| Scheduling | No | No | Yes |
| Scalability | Partial | Partial | Yes |

## Demo

Included sample demonstrates:

- player action triggers event
- pipeline modifies event data
- UI updates automatically
- debugger visualizes execution flow

## Installation

Unity Package Manager (Git URL)

```text
https://github.com/<your-username>/rivonix-eventflow.git
```

## Roadmap

- Advanced pipeline controls (enable/disable steps)
- Priority-based execution improvements
- Visual pipeline editor
- Multiplayer event sync layer

## About Rivonix

Rivonix is a modular Unity systems toolkit focused on building scalable, production-ready game architecture.

## License

MIT License
