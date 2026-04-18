# Common Anti-Patterns by Language

When reviewing code, use this reference for language-specific pitfalls and idiomatic improvements. Read the section matching the language of the code under review.

## Table of Contents
- [JavaScript / TypeScript](#javascript--typescript)
- [Python](#python)
- [Go](#go)
- [Java](#java)
- [Rust](#rust)
- [C#](#c)
- [PHP](#php)
- [Kotlin](#kotlin)

## JavaScript / TypeScript

### Common Anti-Patterns
- **`==` instead of `===`**: Loose equality causes surprising coercions (`"0" == false` is `true`). Always use strict equality.
- **Missing `await`**: Forgetting `await` on async calls leads to working with Promise objects instead of values. The code may appear to work but silently drops errors.
- **Modifying state directly in React**: `state.items.push(x)` won't trigger re-render. Must create new references: `setItems([...items, x])`.
- **useEffect with missing dependencies**: Stale closures cause bugs that are hard to trace. ESLint's exhaustive-deps rule catches these.
- **`for...in` on arrays**: Iterates over all enumerable properties (including prototype), not just indices. Use `for...of`, `.forEach()`, or `.map()`.
- **Floating point comparison**: `0.1 + 0.2 !== 0.3`. Use epsilon comparison or integer arithmetic for money.
- **Catching errors without handling**: `catch (e) {}` silently swallows failures. At minimum, log the error.
- **Memory leaks from uncleared intervals/listeners**: `setInterval` and `addEventListener` need corresponding cleanup, especially in component unmount lifecycle.

### Idiomatic Patterns
- Prefer `const` by default, `let` when reassignment is needed, never `var`
- Use optional chaining (`?.`) and nullish coalescing (`??`) instead of verbose null checks
- Prefer `Array.prototype` methods (map, filter, reduce) over manual loops for data transformations
- Use template literals over string concatenation for readability
- In TypeScript, prefer narrowing (type guards) over type assertions (`as`)

## Python

### Common Anti-Patterns
- **Mutable default arguments**: `def f(items=[])` shares the same list across all calls. Use `def f(items=None): items = items or []`.
- **Bare `except:`**: Catches `SystemExit`, `KeyboardInterrupt`, and `GeneratorExit`. At minimum use `except Exception:`.
- **Using `type()` for type checking**: `isinstance()` handles inheritance correctly.
- **String concatenation in loops**: Building strings with `+=` in a loop is O(n^2). Use `"".join(parts)` or `io.StringIO`.
- **Not using context managers**: `open()` without `with` risks file handle leaks on exceptions.
- **`from module import *`**: Pollutes namespace, makes it impossible to trace where names come from.
- **Checking `== True` or `== None`**: Use `if x:` for truthiness, `if x is None:` for identity.

### Idiomatic Patterns
- Use f-strings for string formatting (Python 3.6+)
- Use dataclasses or NamedTuple instead of plain dicts for structured data
- Use `pathlib.Path` instead of `os.path` string manipulation
- Use list/dict/set comprehensions when they improve readability
- Use `enumerate()` instead of manual counter variables
- Use type hints for function signatures

## Go

### Common Anti-Patterns
- **Ignoring errors**: `result, _ := doSomething()` silently drops errors. Always handle or explicitly document why it's safe to ignore.
- **Goroutine leaks**: Launching goroutines without ensuring they can terminate (no context, no done channel, no timeout).
- **Data races with shared state**: Accessing shared variables from multiple goroutines without sync primitives. Use `go run -race` to detect.
- **Defer in loops**: `defer` runs at function end, not loop end. Resources opened in loops accumulate until the function returns.
- **Nil map writes**: Writing to an uninitialized map panics. Always `make(map[K]V)` first.
- **Interface pollution**: Defining interfaces with many methods upfront. Go idiom is small interfaces, defined by consumers.

### Idiomatic Patterns
- Accept interfaces, return structs
- Use `context.Context` for cancellation and deadlines
- Errors are values — wrap with `fmt.Errorf("...: %w", err)` for context
- Use table-driven tests
- Keep packages focused and name them by what they provide, not what they contain

## Java

### Common Anti-Patterns
- **Catching `Exception` or `Throwable` broadly**: Hides specific error types and makes debugging harder. Catch the narrowest applicable exception.
- **Null references without checks**: Use `Optional<T>` for values that may be absent, or validate inputs at boundaries.
- **String comparison with `==`**: Compares references, not values. Use `.equals()`.
- **Resource leaks (pre-try-with-resources)**: Always use try-with-resources for `AutoCloseable` objects.
- **Synchronized on non-final fields**: The lock reference can change, defeating the synchronization.
- **Mutable collections exposed from methods**: Return `Collections.unmodifiableList()` or defensive copies.

### Idiomatic Patterns
- Use records (Java 16+) for data carrier classes
- Prefer `var` for local variables when the type is obvious from the right-hand side
- Use Stream API for collection transformations, but don't over-chain to the point of unreadability
- Use sealed interfaces (Java 17+) for exhaustive type hierarchies

## Rust

### Common Anti-Patterns
- **Excessive `.unwrap()` or `.expect()`**: Panics in production code. Use `?` operator for error propagation, or pattern matching for explicit handling.
- **Cloning to satisfy the borrow checker**: Often indicates a design issue. Consider restructuring ownership instead.
- **Large `unsafe` blocks**: Minimize `unsafe` scope. Each `unsafe` block should have a safety comment explaining why the invariants hold.
- **Not using iterators**: Manual indexing instead of iterator chains loses safety guarantees and composability.
- **Mutable static variables**: `static mut` is always unsafe and a data race hazard. Use `std::sync::OnceLock`, `Mutex`, or `RwLock` for shared mutable state.
- **Leaking resources with `mem::forget` or `ManuallyDrop`**: Prevents destructors from running, causing resource leaks.

### Idiomatic Patterns
- Use `Result<T, E>` and `?` for error handling
- Prefer `&str` over `String` in function parameters
- Use `impl Trait` for return types when the concrete type is an implementation detail
- Use `derive` macros for common traits
- Use `thiserror` for library error types and `anyhow` for application error handling
- Prefer `Vec::with_capacity` when the size is known upfront to avoid reallocations

## C#

### Common Anti-Patterns
- **`== null` instead of `is null`**: The `==` operator can be overloaded, making null checks unreliable. Always use `is null` or `is not null` for null comparisons — they cannot be overridden and are more predictable.
- **Forgetting to dispose resources**: `SqlConnection`, `HttpClient`, `StreamReader` and other `IDisposable` types must be disposed. Use `using` declarations or `using` blocks to ensure cleanup on all code paths, including exceptions.
- **`async void` methods**: These are fire-and-forget — exceptions crash the process instead of being caught. Only use `async void` for event handlers. Everything else should return `Task` or `Task<T>`.
- **Blocking on async code**: Calling `.Result` or `.Wait()` on a Task causes deadlocks in UI and ASP.NET contexts. Use `await` all the way through the call chain.
- **Catching `Exception` broadly**: `catch (Exception ex)` hides specific error types. Catch the most specific exception type possible, and let unexpected exceptions propagate.
- **String concatenation in loops**: `+=` on strings in a loop is O(n^2). Use `StringBuilder` or `string.Join()`.
- **Mutable static state**: Static fields shared across requests in ASP.NET cause race conditions and data leaks between users. Use dependency injection with appropriate lifetimes instead.
- **Missing nullable reference type annotations**: Not enabling `<Nullable>enable</Nullable>` or ignoring warnings leads to `NullReferenceException` at runtime. Trust the type system and declare nullability explicitly.

### Idiomatic Patterns
- Use file-scoped namespace declarations (`namespace Foo;` instead of `namespace Foo { ... }`)
- Prefer pattern matching and `switch` expressions over chains of `if`/`else`
- Use `nameof()` instead of string literals when referring to member names (refactoring-safe)
- Use `record` types for immutable data transfer objects
- Use collection expressions (`[1, 2, 3]`) in C# 12+ instead of `new List<int> { 1, 2, 3 }`
- Naming: PascalCase for types/methods/properties, camelCase for locals/parameters, `_camelCase` for private fields, `I` prefix for interfaces
- Prefer `var` only when the type is apparent from the right-hand side; use explicit types otherwise
- Use primary constructors (C# 12+) for simple dependency injection

### Project-Specific Conventions
When reviewing C# code, always check the project's `.editorconfig`, `Directory.Build.props`, and analyzer settings for project-specific formatting and style rules. Align your feedback with whatever conventions the project has established rather than imposing defaults. Common configurable conventions include indentation style, brace placement, `using` statement style, expression-bodied member preferences, and import ordering.

## PHP

### Common Anti-Patterns
- **SQL injection via string interpolation**: `$db->query("SELECT * FROM users WHERE id = $id")` — always use prepared statements with `$stmt->execute([$id])`.
- **`extract()` on user input**: `extract($_POST)` creates variables from user-controlled keys, enabling variable overwrite attacks and mass assignment. Never use it on untrusted data.
- **Using `md5()` or `sha1()` for passwords**: Use `password_hash()` / `password_verify()` with `PASSWORD_BCRYPT` or `PASSWORD_ARGON2ID`.
- **Loose comparison pitfalls**: `"0" == false` is `true`, `"0e123" == "0e456"` is `true` (magic hash). Use `===` for strict comparison.
- **Suppressing errors with `@`**: `@file_get_contents(...)` hides failures silently. Check return values explicitly and handle errors.
- **Not escaping output**: Rendering user input without `htmlspecialchars($var, ENT_QUOTES, 'UTF-8')` leads to XSS.
- **Using `$_GET`/`$_POST` directly deep in business logic**: Accessing superglobals outside the controller layer couples business logic to HTTP. Pass validated values as function parameters.

### Idiomatic Patterns
- Use type declarations for parameters and return types (PHP 7.4+)
- Use `match` expression instead of `switch` when returning values (PHP 8.0+)
- Use constructor property promotion for simple DTOs (PHP 8.0+)
- Use enums instead of class constants for enumerated values (PHP 8.1+)
- Use `readonly` properties for immutable data (PHP 8.1+)
- Use named arguments for functions with many optional parameters

## Kotlin

### Common Anti-Patterns
- **Platform type `!` ignored**: Java interop returns platform types (`String!`) which bypass null safety. Explicitly annotate nullability when calling Java code.
- **Using `!!` (non-null assertion) liberally**: Throws `NullPointerException` on null — defeats Kotlin's null safety. Use `?.`, `?:`, or `let` blocks instead.
- **Blocking the main thread on Android**: Network/DB calls on the main thread cause ANR. Use `withContext(Dispatchers.IO)` or appropriate coroutine dispatchers.
- **Leaking coroutine scopes**: Launching coroutines in `GlobalScope` or without structured concurrency causes leaks. Use `viewModelScope`, `lifecycleScope`, or a custom `CoroutineScope` with proper cancellation.
- **Mutable collections exposed from classes**: Returning `MutableList` from a public API allows callers to modify internal state. Return `List` (read-only view) or defensive copies.
- **`lateinit` for nullable state**: `lateinit var` throws `UninitializedPropertyAccessException` if accessed before assignment. Use nullable types with `?.` for truly optional state.

### Idiomatic Patterns
- Use `data class` for DTOs and value objects — provides `equals()`, `hashCode()`, `copy()`
- Use sealed classes/interfaces for exhaustive type hierarchies
- Use scope functions (`let`, `run`, `apply`, `also`, `with`) appropriately — `let` for null checks, `apply` for object configuration
- Use `require()` / `check()` for preconditions and state validation
- Use string templates `"Hello, $name"` instead of concatenation
- Prefer `when` expression over `if/else` chains for multiple conditions
