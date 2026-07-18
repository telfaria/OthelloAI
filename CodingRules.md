# C# Coding Rules

## Naming

Class:
PascalCase

Method:
PascalCase

Private field:
camelCase with _ prefix


Example:

```csharp
private readonly Board _board;
```

## Nullable
Enabled.
Avoid null unless meaningful.

## Collections

Prefer:
IReadOnlyList<T>

over:
List<T>

when exposing data.

## Performance

Avoid unnecessary:

- LINQ in search algorithms
- object allocation
- copying Board state unnecessarily

## Algorithms

Complex algorithms require comments explaining:

- purpose
- algorithm flow
- performance characteristics
