# Kawa App Testing

## Use Case Tests

Test use cases directly when validating application behavior.

```csharp
[Fact]
public async Task ExecuteAsync_ReturnsValidationFailure_WhenEmailIsMissing()
{
    var useCase = new CreateUserUseCase();
    var request = new CreateUser.Request("Louisa", "");

    var result = await useCase.ExecuteAsync(request);

    Assert.True(result.IsFailure);
    Assert.Equal(KawaErrorKind.Validation, result.Error!.Kind);
}
```

Use case tests should cover:

- successful response shape
- predictable validation failures
- dependency behavior through mocks or fakes
- cancellation only when the use case has meaningful cancellation logic

## Endpoint Tests

Add endpoint tests when web behavior matters:

- route shape
- request binding
- dependency registration
- authorization policy attached by the application
- HTTP mapping when the application customizes transport behavior

Do not duplicate every use case rule through HTTP tests. Keep business logic coverage close to the use case.

## OpenAPI And Catalog Checks

Check OpenAPI or catalog output when a change affects public API shape, metadata, or generated clients:

```bash
dotnet run --project <web-project>
curl http://localhost:<port>/openapi/v1.json
curl http://localhost:<port>/kawa/catalog.json
```

Look for:

- route path
- request schema
- response schema
- `KawaErrorKind` response metadata
- use case name, summary, version, and tags
- nested schema reference names that distinguish same-named `Request` and `Response` contracts

## Test Command Choice

Prefer the narrowest useful command first:

```bash
dotnet test <test-project>
dotnet test --filter <RelevantTestName>
dotnet test
```

Run broader tests before reporting completion or before committing changes.

