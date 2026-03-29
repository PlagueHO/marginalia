# Testing Patterns

> Confidence: high
> Source: Established in initial test suite — 167 tests across backend and frontend

## Pattern

### Backend (MSTest + NSubstitute + FluentAssertions)

**Test organization:**

- One test file per class under test in a matching subdirectory (`Domain/`, `Services/`, `Repositories/`, `Configuration/`)
- `[TestCategory("Unit")]` or `[TestCategory("Integration")]` on every test class
- `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]` in MSTestSettings.cs

**Naming:** `MethodName_Scenario_ExpectedResult`

**Domain model tests include:**

- Construction with required fields
- Serialization produces camelCase property names
- Deserialization from camelCase JSON
- Record equality and `with` copy behavior
- Edge cases: empty strings, large values, null optionals

**Interface contract tests:**

- Use a concrete test double (e.g., `TestDocumentRepository` with `ConcurrentDictionary`) rather than mocking the interface
- This validates behavioral contracts the real implementation must satisfy
- Include thread safety tests with `Task.WhenAll` for concurrent operations

**NSubstitute tips:**

- Never mix literal `null` with `Arg.Any<>` — always use `Arg.Any<string?>()` for all args of the same type
- Use `Arg.Is<T>(predicate)` for specific value matching
- Use `.ThrowsAsync()` to test error handling paths

### Frontend (Vitest + @testing-library/react + jest-axe)

**Test organization:**

- Component tests in `tests/components/ComponentName.test.tsx`
- Service tests in `tests/services/serviceName.test.ts`
- Use `describe/it` blocks grouped by behavior: rendering, interactions, accessibility

**Setup:**

- `vitest.setup.ts` imports `@testing-library/jest-dom` and `jest-axe/extend-expect`
- `vitest.config.ts` sets `environment: 'jsdom'` and includes `tests/**/*.test.{ts,tsx}`

**Component test template:**

```tsx
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { axe } from 'jest-axe'

describe('ComponentName', () => {
  const defaultProps = { /* ... */ }
  beforeEach(() => { vi.clearAllMocks() })

  describe('rendering', () => { /* visible elements, aria labels */ })
  describe('interactions', () => { /* user events, callbacks */ })
  describe('accessibility', () => {
    it('passes axe checks', async () => {
      const { container } = render(<Component {...defaultProps} />)
      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
```

**API service tests:**

- Mock `fetch` globally with `vi.stubGlobal('fetch', mockFetch)`
- Call `mockFetch.mockReset()` in `beforeEach` to isolate tests
- Verify URL, method, headers, and body for each request
- Test error handling: non-ok responses, network failures, 204 No Content

**axe exclusions:**

- Document known violations with comments explaining the issue
- Use `axe(container, { rules: { 'rule-name': { enabled: false } } })` sparingly
- Always file a follow-up to fix the underlying accessibility issue

## When to Apply

- Every new component or service gets a corresponding test file
- Every domain model gets serialization round-trip tests
- Every interface gets contract tests before implementation begins
- Accessibility tests are mandatory for all UI components
