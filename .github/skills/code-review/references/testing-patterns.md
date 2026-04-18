# Testing Review Patterns

Use this reference when evaluating test quality during a code review. Good tests are as important as the code they cover — they serve as living documentation and a safety net for future changes.

## Table of Contents
- [Common Anti-Patterns](#common-anti-patterns)
- [What Good Tests Look Like](#what-good-tests-look-like)
- [Language-Specific Test Patterns](#language-specific-test-patterns)
- [Test Double Strategy](#test-double-strategy)
- [Integration & E2E Test Anti-Patterns](#integration--e2e-test-anti-patterns)
- [Contract Testing](#contract-testing)
- [Mutation Testing](#mutation-testing)
- [Property-Based Testing](#property-based-testing)
- [Snapshot / Golden File Testing](#snapshot--golden-file-testing)

## Common Anti-Patterns

### Assertion-Free Tests
**Pattern**: Tests that execute code but never assert anything meaningful. They pass as long as no exception is thrown.
**Why it's bad**: Gives false confidence — the test "passes" even if the output is completely wrong.
**Fix**: Every test should assert at least one observable outcome (return value, state change, side effect, or thrown exception).

### Testing Implementation Instead of Behavior
**Pattern**: Tests that assert on internal details (private method calls, exact SQL strings, internal state) rather than observable behavior.
**Why it's bad**: Tests break on every refactor even when behavior is preserved, discouraging improvement.
**Fix**: Test through public interfaces. Assert on what the code does, not how it does it.

### Excessive Mocking
**Pattern**: Tests that mock every dependency, including simple value objects, data structures, or stable libraries.
**Why it's bad**: Tests become coupled to the exact shape of the implementation. Mock behavior diverges from real behavior over time.
**Fix**: Only mock at true system boundaries (external APIs, databases, file systems, clocks). Use real objects for internal collaborators when practical.

### Copy-Paste Test Code
**Pattern**: Near-identical test methods with small variations copy-pasted instead of parameterized.
**Why it's bad**: Maintenance burden — fixing a test setup bug requires editing N copies.
**Fix**: Use parameterized/data-driven tests (`@pytest.mark.parametrize`, `[Theory]` in xUnit, table-driven tests in Go, `test.each` in Jest).

### Flaky Tests
**Pattern**: Tests that sometimes pass and sometimes fail without code changes. Common causes: timing dependencies, shared mutable state, test ordering dependencies, non-deterministic data.
**How to spot**: Look for `sleep()`, `Thread.sleep()`, shared static state, reliance on real time/dates, or tests that modify global configuration.
**Fix**: Use deterministic clocks/timers, isolate test state, use proper synchronization for async tests.

### Giant Test Setup (Arrange)
**Pattern**: 50+ lines of setup before the actual test action, often constructing deeply nested object graphs.
**Why it's bad**: Hard to understand what's actually being tested. Signals that the code under test may have too many dependencies.
**Fix**: Use builder patterns or factory methods for test data. Consider whether the production code needs refactoring to reduce coupling.

### Testing Only the Happy Path
**Pattern**: Tests only cover the success scenario. No tests for invalid inputs, error conditions, edge cases, or boundary values.
**Fix**: Add tests for: null/empty inputs, boundary values (0, -1, MAX_INT), error responses, timeout/retry behavior, concurrent access scenarios.

## What Good Tests Look Like

### Clear Structure
Each test should follow Arrange → Act → Assert (or Given → When → Then) with clear visual separation. The test name should describe the scenario being tested, not the method name.

### One Concept Per Test
A test should verify one logical concept. Multiple assertions are fine if they all verify different aspects of the same behavior — but testing two unrelated behaviors in one test makes failures ambiguous.

### Readable Without Context
Someone reading the test should understand the expected behavior without reading the production code. Good test names describe: the scenario, the action, and the expected outcome.

### Fast and Independent
Tests should not depend on each other's execution order or shared state. Each test sets up its own preconditions and cleans up after itself.

## Language-Specific Test Patterns

### JavaScript/TypeScript (Jest/Vitest)
- Use `describe` blocks to group related tests
- Prefer `toEqual` for deep comparison, `toBe` for primitives
- Use `beforeEach` for shared setup, not shared mutable variables
- For async tests: always `await` or return the promise; use `expect.assertions(N)` for tests with catch blocks

### Python (pytest)
- Use fixtures for reusable setup; prefer function-scoped fixtures
- Use `@pytest.mark.parametrize` for data-driven tests
- Use `pytest.raises` context manager for exception testing
- Avoid `unittest.TestCase` when using pytest — it limits fixture usage

### C# (xUnit/NUnit)
- Use `[Theory]` with `[InlineData]` or `[MemberData]` for parameterized tests
- Use `FluentAssertions` or similar for readable assertions
- Do not emit "Arrange", "Act", "Assert" section comments
- Follow the project's existing test naming convention

### Go
- Use table-driven tests with subtests (`t.Run`)
- Use `testify/assert` or standard `if` checks — be consistent within the project
- Use `t.Helper()` in test helper functions for better error reporting
- Use `t.Parallel()` where tests are independent

### Java (JUnit 5)
- Use `@ParameterizedTest` with `@ValueSource`, `@CsvSource`, or `@MethodSource` for data-driven tests
- Use `assertThrows` for exception testing instead of try-catch with `fail()`
- Use `@BeforeEach` / `@AfterEach` for per-test setup/teardown, `@BeforeAll` / `@AfterAll` for expensive shared resources
- Use `@Nested` classes to group related tests and share setup
- Use `@DisplayName` for readable test names when method names aren't sufficient
- Prefer AssertJ fluent assertions for complex assertions (e.g., `assertThat(list).hasSize(3).contains("a")`)

### Rust
- Use `#[cfg(test)] mod tests` for unit tests in the same file
- Use `#[should_panic(expected = "...")]` for expected panic tests
- Use `assert_eq!`, `assert_ne!`, and `assert!(condition, "message")` with descriptive messages
- Use `Result<(), Box<dyn Error>>` return type in tests for `?` operator support

## Test Double Strategy

Choose the right test double for the situation:

- **Fake**: A working implementation with shortcuts (e.g., in-memory database, local file store). Best for integration tests where you need realistic behavior without external dependencies.
- **Stub**: Returns canned responses. Use when you just need a dependency to return specific data for a test scenario.
- **Mock**: Verifies interactions (was this method called with these arguments?). Use sparingly — only when the interaction itself is the behavior under test (e.g., verifying an email was sent).
- **Spy**: Wraps a real object and records calls. Use when you need real behavior but also want to verify interactions.

**Rule of thumb**: Prefer fakes over mocks for most tests. Mocks test *how* code works; fakes test *what* code does. Fakes lead to less brittle tests.

**Common misuses to flag in reviews:**
- Mocking the class under test — only mock its dependencies, never the thing being tested
- Mocking value objects or data structures (DTOs, configs) — just construct the real object
- Mock returning a mock (`when(mock.getX()).thenReturn(anotherMock)`) — indicates test is coupled to implementation chain
- Not verifying mock interactions — setting up a mock but never asserting it was called correctly voids its purpose

## Integration & E2E Test Anti-Patterns

### Relying on External Services in CI
**Pattern**: Tests that hit real third-party APIs (payment providers, email services) in CI pipelines.
**Why it's bad**: Flaky failures from network issues, rate limits, or service downtime. Also risks real side effects (charges, emails sent).
**Fix**: Use fakes or recorded responses (VCR pattern / WireMock / nock) for CI. Reserve real-service tests for a separate, low-frequency integration suite.

### No Test Data Cleanup
**Pattern**: Integration tests that create records in a shared database but never clean them up.
**Why it's bad**: Tests pollute each other's state, causing ordering-dependent failures.
**Fix**: Use per-test transactions that roll back, or dedicated test databases that are reset between runs.

### Environment-Specific Assumptions
**Pattern**: Tests hardcode paths, ports, hostnames, or OS-specific behavior (e.g., `/tmp/`, `localhost:5432`, Windows vs. Unix line endings).
**Why it's bad**: Tests pass on one developer's machine and fail in CI or on a different OS.
**Fix**: Use environment variables or test config for environment-specific values. Use cross-platform path APIs.

### Testing Too Many Layers at Once
**Pattern**: E2E tests that verify business logic, UI rendering, and API behavior all in one test — when failures could be caught at a lower level.
**Why it's bad**: Slow, brittle, and hard to diagnose which layer failed. Minor UI changes break tests that are really about business logic.
**Fix**: Follow the testing pyramid — most tests should be unit tests, fewer integration tests, fewest E2E tests. Only use E2E for critical user flows.

### Timeout-Based Synchronization
**Pattern**: Using `sleep(2000)` or fixed waits to synchronize with async operations in E2E or integration tests.
**Why it's bad**: Too short → flaky failures; too long → slow test suite. Both vary by machine load.
**Fix**: Use polling/retry with a condition (e.g., wait until element appears, wait until queue is empty) with a maximum timeout.

## Contract Testing

### When to Suggest Contract Tests
Contract testing verifies that API producers and consumers agree on the interface contract (request/response shapes, status codes, headers). Suggest contract tests when reviewing:
- **Microservice API boundaries**: Services that communicate via HTTP/gRPC and are deployed independently
- **Frontend-backend interfaces**: When the frontend team and backend team work on separate repos/schedules
- **Third-party API integrations**: When consuming external APIs that may change without notice

### Common Frameworks
- **Pact**: Consumer-driven contract testing (JS, Java, Python, Go, Ruby, .NET, Rust). Consumer writes expectations, provider verifies.
- **Spring Cloud Contract**: JVM-focused, contracts written in Groovy DSL or YAML
- **Specmatic**: Contract-first testing from OpenAPI specs

### Anti-Patterns to Flag
- **Duplicating contract validation in E2E tests**: If contract tests exist, E2E tests shouldn't re-verify the same API shape — they should focus on user flows
- **Contracts that test implementation, not interface**: Contracts should verify schema, status codes, and required fields — not internal field ordering or optional fields
- **Stale contracts**: Contracts committed but never run in CI — they become outdated and give false confidence

## Mutation Testing

### When to Mention Mutation Testing
Mutation testing measures test suite effectiveness by introducing small code changes (mutants) and checking whether tests catch them. Mention it in reviews when:
- **Test coverage is high but assertions are weak**: 90% line coverage but tests only assert `!= null` — mutation testing would expose this
- **Critical business logic with shallow tests**: Payment calculations, access control, or data validation with simple happy-path tests
- **The team is debating "enough" testing**: Mutation score provides an objective metric beyond line coverage

### Common Frameworks
- **Stryker**: JavaScript/TypeScript, C#, Scala
- **PIT (pitest)**: Java/Kotlin — integrates with Maven/Gradle
- **mutmut**: Python
- **cargo-mutants**: Rust

### Key Concept: Mutation Score
`mutation_score = killed_mutants / total_mutants`. A score of 80%+ generally indicates a strong test suite. Below 60% suggests tests are passing without truly validating behavior.

### Anti-Pattern: Pursuing 100% Mutation Score
Not all mutants are worth killing. Mutants in logging, toString(), or trivial getters/setters are noise. Focus mutation testing on business-critical code paths.

## Property-Based Testing

### When to Suggest Property-Based Tests
Property-based testing (PBT) generates random inputs and checks that invariants hold. Suggest PBT in reviews when you see:
- **Serialization/deserialization**: `decode(encode(x)) == x` for all x
- **Sorting/ordering**: Output is always sorted, length is preserved, same elements
- **Parsers/formatters**: `parse(format(x)) == x`, format always produces valid output
- **Mathematical operations**: Commutativity, associativity, identity element
- **Data transformations**: Idempotency (`f(f(x)) == f(x)`), reversibility

### Common Frameworks
- Python: `hypothesis` — `@given(st.lists(st.integers()))` generates random lists of integers
- JavaScript/TypeScript: `fast-check` — `fc.property(fc.array(fc.integer()), arr => ...)`
- C#: `FsCheck` or `FsCheck.Xunit` — `[Property] public bool MyProperty(int[] arr) => ...`
- Java: `jqwik` — `@ForAll List<Integer> items`
- Rust: `proptest` — `proptest! { fn my_test(s in ".*") { ... } }`

### Anti-Pattern: Using PBT as a Fuzz Substitute
PBT frameworks generate structured, typed data — they are not fuzz testers. Don't use them to test raw byte parsing or find crashes in unsafe code; use actual fuzzers (AFL, libFuzzer) for that.

## Snapshot / Golden File Testing

### When Snapshots Help
- Complex output that's tedious to assert field-by-field (serialized JSON, HTML rendering, CLI output)
- Detecting unintended changes in stable output (API responses, generated code)

### Anti-Patterns to Flag
- **Snapshot churn**: If snapshots need updating every PR, they've become a rubber stamp — the reviewer just approves "update snapshots" without checking the diff
- **Snapshotting volatile data**: Timestamps, random IDs, or machine-specific paths in snapshots cause false failures
- **Oversized snapshots**: Snapshot files >500 lines are hard to review in diffs — consider snapshotting a subset or key sections
- **Snapshot-only testing**: Snapshots verify "output didn't change" but don't verify "output is correct" — pair with targeted assertions for critical properties

### Fix for Volatile Data
Normalize or mask volatile fields before snapshotting (e.g., replace timestamps with `"<TIMESTAMP>"`, replace UUIDs with `"<UUID>"`).
