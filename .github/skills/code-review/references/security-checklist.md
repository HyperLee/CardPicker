# Security Review Checklist

Use this checklist when the code under review handles user input, authentication, data storage, or external communication. Not every item applies to every review — focus on what's relevant.

## Input Handling

- [ ] All user input is validated at the system boundary (type, length, format, range)
- [ ] Input used in SQL queries goes through parameterized queries or prepared statements — never string interpolation
- [ ] Input rendered in HTML is escaped or sanitized to prevent XSS (check both server-rendered and client-side rendering)
- [ ] Input used in shell commands goes through safe APIs (e.g., subprocess with list args) — never `os.system()` or backtick interpolation
- [ ] Input used in file paths is validated against path traversal (`../`, null bytes, symlink attacks)
- [ ] Input used in regex is escaped or validated to prevent ReDoS (catastrophic backtracking)
- [ ] Deserialization of user-controlled data uses safe formats (JSON) not dangerous ones (pickle, eval, YAML `!!python`)
- [ ] Server-side requests do not use user-controlled URLs without allowlist validation (SSRF prevention)
- [ ] User input is never passed directly into server-side template engines (Jinja2, Twig, Freemarker) — use sandboxed rendering or escape template syntax to prevent SSTI

## Authentication & Authorization

- [ ] Authentication checks exist on all endpoints that require them
- [ ] Authorization checks verify the requesting user has permission for the specific resource, not just that they're logged in
- [ ] Object-level authorization checks prevent accessing other users' resources via ID manipulation (IDOR prevention)
- [ ] Session tokens are generated with cryptographically secure random generators
- [ ] Passwords are hashed with a modern algorithm (bcrypt, scrypt, argon2) — not MD5, SHA1, or plain SHA256
- [ ] Failed login attempts are rate-limited to prevent brute force
- [ ] Sensitive operations require re-authentication or step-up auth
- [ ] JWT tokens are validated (signature, expiration, issuer, audience) — not just decoded
- [ ] CSRF protection is in place for state-changing requests when using cookie-based authentication (SameSite attribute, CSRF tokens, or origin/referer validation)
- [ ] OAuth 2.0 / OIDC flows validate the `state` parameter to prevent CSRF and use PKCE for public clients to prevent authorization code interception

## Data Protection

- [ ] Secrets (API keys, passwords, tokens) are not hardcoded in source code
- [ ] Sensitive data is not logged (passwords, tokens, PII, credit card numbers)
- [ ] PII is encrypted at rest if required by compliance
- [ ] Database connections use parameterized queries
- [ ] Error messages don't expose internal implementation details to end users
- [ ] CORS configuration is restrictive (not `*` for authenticated endpoints)
- [ ] HTTP security headers are set (CSP, HSTS, X-Frame-Options, etc.)
- [ ] Content Security Policy (CSP) is configured to restrict script sources — avoids `unsafe-inline` and `unsafe-eval` where possible, uses nonces or hashes for inline scripts

## Cryptography

- [ ] Using well-known libraries, not custom crypto implementations
- [ ] Random values for security purposes use cryptographic RNG, not `Math.random()` or `random.random()`
- [ ] Encryption keys are not derived from predictable sources
- [ ] TLS is enforced for all network communication with external services

## Dependency & Supply Chain

- [ ] New dependencies are well-maintained and widely used
- [ ] Dependencies are pinned to specific versions (not floating ranges for security-critical packages)
- [ ] No known vulnerabilities in added dependencies (check advisories)
- [ ] Lock files (package-lock.json, yarn.lock, go.sum) are committed and maintained
- [ ] Build pipeline does not execute untrusted code or download from unverified sources

## API Security

- [ ] Rate limiting is implemented on public-facing endpoints
- [ ] Request body size limits are enforced to prevent DoS
- [ ] API responses don't leak internal identifiers, stack traces, or implementation details
- [ ] API versioning strategy exists for breaking changes
- [ ] Pagination is enforced on list endpoints to prevent unbounded responses

## Logging & Monitoring

- [ ] Sensitive data (PII, tokens, passwords) is never written to logs
- [ ] Security-relevant events are logged (failed auth, permission denied, input validation failures)
- [ ] Log entries include sufficient context for debugging (request ID, user ID, timestamp) without leaking secrets
- [ ] Audit trail exists for destructive or privileged operations

## WebSocket Security

- [ ] WebSocket connections require authentication (token validation on connection, not just on HTTP upgrade)
- [ ] Server validates origin header to prevent cross-site WebSocket hijacking
- [ ] Message payloads are validated — WebSocket messages bypass standard HTTP input validation middleware
- [ ] Rate limiting is applied per connection to prevent message flooding
- [ ] Connection limits per user/IP are enforced to prevent resource exhaustion

## GraphQL Security

- [ ] Query depth limiting is configured to prevent deeply nested queries that cause exponential backend work
- [ ] Query complexity/cost analysis is implemented — a single query can trigger thousands of resolver calls
- [ ] Introspection is disabled in production (attackers use it to map the entire schema)
- [ ] Field-level authorization is enforced in resolvers, not just at the query entry point
- [ ] Batch query limits are set to prevent attackers from sending arrays of expensive queries in one request

## File Upload Security

- [ ] File type is validated server-side by content (magic bytes / MIME sniffing), not just file extension — attackers rename `.php` to `.jpg`
- [ ] Uploaded files are stored outside the web root or served through a proxy that strips executable content
- [ ] File size limits are enforced server-side (not just client-side) to prevent storage exhaustion
- [ ] Filenames are sanitized or regenerated (UUID) — user-supplied filenames can contain path traversal, null bytes, or special characters
- [ ] Image processing libraries are up to date — libraries like ImageMagick have a history of RCE vulnerabilities (ImageTragick)
- [ ] Uploaded files are scanned for malware when handling user-generated content in sensitive environments

## Container Security

- [ ] Docker images use minimal base images (distroless, alpine) — not full OS images that include unnecessary attack surface
- [ ] Images are pinned to specific digests (`image@sha256:...`), not just tags — tags are mutable and can be overwritten
- [ ] Containers do not run as root — use `USER nonroot` or equivalent in Dockerfile
- [ ] No secrets (API keys, passwords) are baked into Docker image layers — use runtime secrets injection (Docker secrets, Vault, env vars)
- [ ] Images are scanned for known vulnerabilities in CI (Trivy, Snyk, Grype) before deployment
- [ ] Docker socket is not mounted into containers unless absolutely necessary (provides host-level access)

## JWT-Specific Security

- [ ] JWT algorithm is explicitly specified and validated server-side — never trust the `alg` header from the token (algorithm confusion attack: `alg: none` or switching RS256 to HS256)
- [ ] JWT secret keys are sufficiently long (≥256 bits for HS256) and rotated periodically
- [ ] JWTs carry minimal claims — avoid storing sensitive data in the payload (tokens are base64-encoded, not encrypted)
- [ ] Token refresh strategy exists — short-lived access tokens (~15 min) with longer-lived refresh tokens stored securely

## gRPC Security

- [ ] mTLS is configured for service-to-service communication — not just server-side TLS
- [ ] Message size limits are set (default unlimited in many gRPC implementations)
- [ ] Deadline/timeout propagation is configured to prevent cascading slowdowns
- [ ] Interceptors enforce authentication and authorization on all RPC methods
