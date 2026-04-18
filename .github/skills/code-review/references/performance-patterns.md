# Performance Review Patterns

Use this reference when performance is a review focus. These patterns apply across most languages and frameworks.

## Algorithm & Data Structure Issues

### O(n^2) When O(n) is Possible
**Pattern**: Nested loops where the inner loop searches for a match in a list.
**Fix**: Use a hash set or hash map for O(1) lookups.
```
# Bad: O(n*m) — checking if items in list A exist in list B
for item in list_a:
    if item in list_b:  # O(m) per check
        ...

# Good: O(n+m) — convert to set first
set_b = set(list_b)  # O(m) once
for item in list_a:
    if item in set_b:  # O(1) per check
        ...
```

### Repeated Work in Loops
**Pattern**: Computing the same value or calling the same function on every iteration.
**Fix**: Hoist invariant computation outside the loop.

### Unbounded Collection Growth
**Pattern**: Lists, maps, or caches that grow without limit.
**Fix**: Add size limits, eviction policies, or time-based expiry.

## Database Performance

### N+1 Query Problem
**Pattern**: Fetching a list, then running one query per item to load related data.
**Fix**: Use JOINs, eager loading, or batch queries.

**How to spot it**: A query inside a loop that iterates over results from another query.

### Missing Indexes
**Pattern**: Queries filtering or sorting on columns without indexes, causing full table scans.
**Fix**: Add appropriate indexes. Be cautious of over-indexing (each index has write-time cost).

### SELECT * When Only a Few Columns Are Needed
**Pattern**: Fetching all columns when only 2-3 are used.
**Fix**: Specify only the needed columns. Reduces network transfer and memory.

### Expensive Operations Inside Transactions
**Pattern**: Long-running computations, external API calls, or file I/O inside a database transaction.
**Fix**: Move non-DB work outside the transaction to reduce lock contention.

## Memory & Resource Patterns

### Unnecessary Copies
**Pattern**: Creating copies of large data structures when a reference or slice would suffice.
**Fix**: Pass by reference, use views/slices, or restructure to avoid copying.

### Resource Leaks
**Pattern**: Opening files, connections, or handles without ensuring cleanup on all code paths (including exceptions).
**Fix**: Use try-with-resources (Java), `with` (Python), `defer` (Go), RAII (Rust/C++).

### Large Object Retained in Long-Lived Scope
**Pattern**: Temporary large objects referenced from a long-lived scope (class field, global, closure), preventing garbage collection.
**Fix**: Set references to null/None when no longer needed, or restructure to limit scope.

## Async & Concurrency Performance

### Sequential Awaits for Independent Operations
**Pattern**: `await a(); await b();` when a and b don't depend on each other.
**Fix**: `await Promise.all([a(), b()])` or equivalent parallel execution.

### Blocking the Event Loop
**Pattern**: CPU-intensive work or synchronous I/O in a single-threaded async context (Node.js, Python asyncio).
**Fix**: Offload to worker threads/processes or use async I/O.

### Excessive Lock Contention
**Pattern**: Using a single lock to protect all shared state, when different parts of the state are accessed independently.
**Fix**: Use fine-grained locks, read-write locks, or lock-free data structures.

## Frontend Performance

### Unnecessary Re-renders (React)
**Pattern**: Components re-rendering when their inputs haven't meaningfully changed.
**Common causes**: New object/array references created on every render, missing `useMemo`/`useCallback` for expensive computations.

### Layout Thrashing
**Pattern**: Reading layout properties (offsetHeight) then immediately writing (style changes) in a loop.
**Fix**: Batch reads together, then batch writes.

### Unoptimized Images/Assets
**Pattern**: Serving full-resolution images for thumbnail views, or loading all assets upfront.
**Fix**: Responsive images, lazy loading, appropriate formats (WebP, AVIF).

### Bundle Size
**Pattern**: Importing entire libraries when only a small part is needed.
**Fix**: Use tree-shakeable imports, dynamic imports for code splitting.

## Caching Patterns

### Missing Cache Invalidation
**Pattern**: Data is cached but never invalidated when the underlying data changes (create, update, delete operations).
**Fix**: Invalidate or update the cache entry whenever the source data is modified. Use consistent cache key patterns and consider write-through or write-behind strategies.

### Caching Null / Error Results
**Pattern**: Cache stores the result of a failed lookup or null response, causing subsequent requests to receive stale "not found" responses even after the data exists.
**Fix**: Only cache successful, non-null results. Alternatively, cache null results with a very short TTL.

### Cache Stampede (Thundering Herd)
**Pattern**: A popular cache key expires and many concurrent requests simultaneously hit the database to regenerate it.
**Fix**: Use lock/mutex on cache miss so only one request regenerates the cache. Alternatively, use background refresh before expiry or probabilistic early expiration.

## Network Performance

### Connection Pool Exhaustion
**Pattern**: Creating a new connection (HTTP, DB, Redis) per request instead of reusing pooled connections.
**Fix**: Use connection pooling (e.g., `IHttpClientFactory` in C#, connection pool in DB drivers, `aiohttp.ClientSession` in Python). Configure pool size limits appropriate to expected concurrency.

### Chatty Service Communication
**Pattern**: Multiple small network calls between services when a single batch call would suffice (similar to N+1 but across services rather than DB).
**Fix**: Use batch APIs, aggregate endpoints, or consider GraphQL for flexible data fetching across service boundaries.

### Missing Compression
**Pattern**: Transferring large payloads (JSON, HTML, static assets) without gzip/brotli compression.
**Fix**: Enable response compression at the server or reverse proxy level. For APIs transferring large payloads, consider binary formats (Protocol Buffers, MessagePack).

## GC Pressure & Object Allocation

### Excessive Short-Lived Allocations
**Pattern**: Creating many temporary objects in hot loops (e.g., string concatenation, boxing value types, LINQ in tight loops in C#).
**Fix**: Use `StringBuilder` for string building, `Span<T>` / `stackalloc` for temporary buffers (C#), or pre-allocated buffers. In Java, watch for autoboxing in loops (`Integer` vs `int`).

### Large Object Heap Fragmentation (C#/.NET)
**Pattern**: Frequent allocation and deallocation of large objects (>85KB), causing LOH fragmentation and Gen 2 collections.
**Fix**: Use `ArrayPool<T>` for large byte arrays, `RecyclableMemoryStream` for streams, or `MemoryPool<T>` for buffers.

## Serverless & Cold Start Performance

### Cold Start Overhead
**Pattern**: Lambda/Cloud Function takes 3-10 seconds on first invocation due to initialization (loading dependencies, establishing connections, JIT compilation).
**Fix**: Keep functions warm with scheduled pings, use provisioned concurrency for latency-critical paths, minimize dependency bundle size, use lazy initialization for non-essential services.

### Connection Re-establishment Per Invocation
**Pattern**: Creating new database/Redis connections in each Lambda invocation instead of reusing across warm invocations.
**Fix**: Initialize connections outside the handler function (module-level). Use connection pooling libraries designed for serverless (e.g., RDS Proxy, `serverless-mysql`, Prisma Data Proxy).

### Over-provisioned Memory / Under-provisioned CPU
**Pattern**: Serverless CPU is typically proportional to allocated memory. Functions with low memory allocation run slower even if they don't need the RAM.
**Fix**: Profile with different memory settings — often doubling memory reduces execution time by 50%+, resulting in equal or lower cost.

## WebSocket & Real-Time Performance

### Broadcasting to All Connections Individually
**Pattern**: Iterating through all connected WebSocket clients and sending messages one by one when broadcasting.
**Fix**: Use pub/sub infrastructure (Redis Pub/Sub, NATS) for multi-server broadcasts. For single-server, batch messages and use write buffering.

### Missing Heartbeat / Idle Connection Cleanup
**Pattern**: WebSocket connections that silently disconnect (network change, laptop sleep) remain in the connection pool, consuming memory and causing failed sends.
**Fix**: Implement ping/pong heartbeats with a timeout. Clean up connections that miss 2-3 consecutive pongs. Most frameworks support this natively.

### Unbounded Message Queue Per Connection
**Pattern**: Buffering outbound messages without a backpressure mechanism. If a slow client can't keep up, the server's memory grows unboundedly.
**Fix**: Set a maximum send buffer size per connection. Drop messages, aggregate, or disconnect slow clients when the buffer exceeds the threshold.

## Performance Review Heuristics

When quantifying performance concerns, consider these thresholds as starting points for discussion (not hard rules):

- **API response time**: >200ms for simple reads, >1s for complex writes — investigate
- **Database queries per request**: >5 queries for a single endpoint — likely N+1 or missing joins
- **Memory allocation per request**: Growing linearly with request count — likely a leak
- **Time complexity**: If input can exceed ~1000 items and algorithm is O(n²) — flag it
- **Bundle size**: >500KB JS for initial load without code splitting — flag it
