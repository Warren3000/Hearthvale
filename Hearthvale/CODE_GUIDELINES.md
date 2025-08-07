# Code Guidelines for Hearthvale

## Project Goals
- Briefly describe the vision and objectives for the project.
- Target platforms and audience.
- Performance, scalability, and maintainability goals.
- Example: “Create a modular, maintainable RPG game with a focus on extensibility and clean architecture.”

## Reference Material
- For all combat system design, mechanics, and related gameplay features, use the Dark Chronicle Wiki as the primary source material.
- The goal is to create a combat system inspired by Dark Chronicle, adapted for 2D gameplay.
- When implementing combat features, reference mechanics, enemy behaviors, and systems from Dark Chronicle for consistency and inspiration.

## Platforms & Frameworks
- This project uses MonoGame (.NET 8) for game development.
- Tiled is used for map and level design.
- Aether.Physics2D is used for physics simulation. https://github.com/tainicom/Aether.Physics2D/releases/tag/v1.0
- Other major tools/libraries: Gum UI, SharpDX, etc.
- Note any platform-specific requirements or coding patterns.

## Code Structure
- Use feature-based folders (e.g., Entities, Managers, UI). Group related classes in namespaces.
- Use singular folder names (e.g., Entity, Manager).
- Separate engine code from game logic.
- Use solution folders for large projects.

## Coding Style
- Use PascalCase for classes and methods, camelCase for variables.
- 4-space indentation. Braces on new lines.
- Use explicit access modifiers (public, private, etc.).
- Prefer explicit types over var except for LINQ and anonymous types.
- Use regions for large files and TODO comments for work-in-progress.
- Use XML documentation comments for public APIs.

## Architectural Patterns
- Favor composition over inheritance.
- Use dependency injection for managers and services.
- Use event-driven systems for gameplay logic.
- Prefer component-based architecture for entities (ECS or composition).
- Decouple systems via interfaces and dependency injection.

## Best Practices
- Require unit/integration tests for core systems.
- Encourage code reviews and pair programming for major features.
- Document design decisions in DESIGN_DECISIONS.md.
- Log errors with context and use assertions for critical invariants.
- Profile and optimize bottlenecks only after measuring.

## Dos and Don’ts
- Do: Write unit tests for new features.
- Do: Use assertions for critical invariants.
- Do: Document public methods and APIs.
- Don’t: Use magic numbers; define constants.
- Don’t: Prematurely optimize or micro-manage memory.
- Don’t: Mix UI and game logic.

## AI Instructions
- Prefer concise, readable code. Use existing project patterns. Avoid unnecessary repetition.
- Prefer idiomatic C# and .NET 8 features.
- Use existing utility/helper classes when possible.
- Avoid introducing new dependencies without approval.

## Example Code Snippet
```csharp
/// <summary>
/// Example of preferred class structure and documentation
/// </summary>
public class ExampleManager
{
    // ...existing code...
    /// <summary>
    /// Performs work and logs the result.
    /// </summary>
    public void DoWork()
    {
        // ...implementation...
    }

    // Example of event subscription
    public void SubscribeEvents()
    {
        SomeEvent += OnSomeEvent;
    }
    public void UnsubscribeEvents()
    {
        SomeEvent -= OnSomeEvent;
    }
    private void OnSomeEvent(object sender, EventArgs e)
    {
        // ...event logic...
    }
}
