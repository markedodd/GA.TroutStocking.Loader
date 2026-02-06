# Testing strategy

This solution uses the .NET Generic Host for composition.

## Guiding principles

- Production types remain `internal` by default.
- The test project is granted access to internals via `InternalsVisibleTo`.
- Unit tests should focus on pure logic.
- Host/DI-based tests verify orchestration and dependency wiring.

## InternalsVisibleTo

The main project contains `Properties/AssemblyInfo.cs`:

- `[assembly: InternalsVisibleTo("GA.TroutStocking.Loader.Tests")]`

This allows tests to:

- Resolve internal services like `RunCommand` (or `IAppCommand`) from the DI container.
- Provide fakes for internal interfaces.

If you want to remove `InternalsVisibleTo` later, you must either:

- Make the relevant production contracts public (increasing API surface area), or
- Shift tests to only target public entry points.

## Recommended test patterns

- Prefer resolving the system under test from a test host:
  - Build a host
  - Register real services via `AddApplicationServices(...)`
  - Override specific dependencies using `services.Replace<T>(...)`

- Avoid mutating global state (`Console.SetOut`, `Console.SetError`, process-wide environment) in tests.
