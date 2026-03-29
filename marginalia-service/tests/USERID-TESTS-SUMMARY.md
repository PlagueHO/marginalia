# UserId Test Suite — Deliverables Summary

**QA Engineer:** Jared  
**Date:** 2026-03-22  
**Task:** Write tests for Cosmos DB userId multi-tenancy support

## Test Files Created (4 files, 43 tests)

### Unit Tests — Repository Contracts

1. **`tests/unit/Repositories/UserIdDocumentRepositoryContractTests.cs`** (14 tests)
   - Validates IDocumentRepository with userId partitioning
   - GetByIdAsync with userId + id composite key
   - GetByUserAsync returns only user's documents
   - SaveAsync stores userId correctly
   - DeleteAsync by userId + id
   - User isolation (users can't see each other's documents)
   - Concurrent operations with multiple users
   - Edge cases and error handling

2. **`tests/unit/Repositories/UserIdSessionRepositoryContractTests.cs`** (12 tests)
   - Validates ISessionRepository with userId partitioning
   - GetByIdAsync with userId + sessionId composite key
   - SaveAsync stores userId correctly
   - AddDocumentToSessionAsync with userId parameter
   - User isolation (same sessionId for different users → separate sessions)
   - Concurrent operations

3. **`tests/unit/Domain/UserIdDefaultingTests.cs`** (9 tests)
   - Document, UserSession, Suggestion default to "_anonymous"
   - Explicit userId values are preserved
   - Record `with` syntax preserves userId

### Integration Tests — Controller Behavior

4. **`tests/integration/Controllers/UserIdHeaderExtractionTests.cs`** (8 tests)
   - Controllers extract userId from X-User-Id header
   - Missing header → "_anonymous"
   - Empty/whitespace header → "_anonymous"
   - User isolation in GET operations
   - End-to-end validation with WebApplicationFactory

## Test Strategy

### Test Doubles Over Mocks
- Created `TestUserIdDocumentRepository` and `TestUserIdSessionRepository`
- Use composite keys (`userId:id`) to simulate Cosmos partitioning
- Test the interface contract, not implementation details
- Same tests will validate Cosmos repositories once they land

### Coverage Areas
✅ Repository CRUD operations with userId  
✅ Multi-tenant data isolation  
✅ UserId defaulting to "_anonymous"  
✅ Controller header extraction  
✅ Edge cases (null, empty, whitespace)  
✅ Concurrent operation safety  
✅ User can't access other users' data

## Build Status

**Expected Compilation Failures** (awaiting Gilfoyle's source changes):

1. **Domain Models** — Need userId property added:
   - `Document.cs` → add `public string UserId { get; init; } = "_anonymous";`
   - `UserSession.cs` → add `public string UserId { get; init; } = "_anonymous";`
   - `Suggestion.cs` → add `public string UserId { get; init; } = "_anonymous";`

2. **Repository Interfaces** — Need userId parameters:
   - `IDocumentRepository.GetByIdAsync(string userId, string id, ...)`
   - `IDocumentRepository.GetByUserAsync(string userId, ...)`
   - `IDocumentRepository.DeleteAsync(string userId, string id, ...)`
   - `ISessionRepository.GetByIdAsync(string userId, string sessionId, ...)`
   - `ISessionRepository.AddDocumentToSessionAsync(string userId, ...)`

3. **Controllers** — Need X-User-Id header extraction:
   - Extract from `Request.Headers["X-User-Id"]`
   - Default to "_anonymous" when missing/empty/whitespace

4. **Infrastructure** — Cosmos SDK dependency:
   - `Marginalia.Infrastructure.csproj` needs `<PackageReference Include="Newtonsoft.Json" />` added

## Validation Plan

Once Gilfoyle's changes land:

```bash
cd marginalia-service
dotnet build
dotnet test
```

Expected outcome: **43 new tests pass** (in addition to existing 79 backend tests).

If any fail:
1. Review test failure output
2. Coordinate with Gilfoyle on interface/behavior mismatch
3. Update tests or source as needed

## Documentation

- ✅ Test approach documented in `.squad/agents/jared/history.md`
- ✅ Decision rationale in `.squad/decisions/inbox/jared-cosmos-tests.md`
- ✅ This deliverables summary for handoff

## Notes for Gilfoyle

Your parallel work should implement:
1. Add `UserId` property to all domain models (default = "_anonymous")
2. Update repository interfaces with userId parameters
3. Implement Cosmos repositories with `/userId` partition key
4. Update in-memory repositories to accept (and ignore) userId parameters
5. Add X-User-Id header extraction in DocumentsController and SessionsController
6. Fix Cosmos SDK Newtonsoft.Json dependency

Tests are contract-first — they define the expected behavior. Once your implementation lands, run the test suite to validate correctness.
