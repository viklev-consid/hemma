using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Features.Login;
using Hemma.Modules.Users.Features.Register;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.SmokeTests;

[Collection("Smoke")]
[Trait("Category", "Smoke")]
public sealed class SmokeTests(SmokeTestFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient client = fixture.CreateAnonymousClient();

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── 10.1a ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StackBoots_HealthEndpointReturns200()
    {
        var response = await client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── 10.1b ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterLoginGetMe_FullPipeline()
    {
        // Register
        var registerResponse = await client.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest("smoke@example.com", "Password1!", "Smoke User"));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var user = await db.Users.FirstAsync(u => u.Email == Email.Create("smoke@example.com").Value);
            user.ConfirmEmail(clock);
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/v1/users/login",
            new LoginRequest("smoke@example.com", "Password1!"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.NotEmpty(login.AccessToken);

        // Get /me with the issued token
        var authed = fixture.CreateAuthenticatedClientWithToken(login.AccessToken);
        var meResponse = await authed.GetAsync("/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("smoke@example.com", me.GetProperty("email").GetString());
    }

    // ── 10.1c ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmationEmailArrivesInMailpit_AfterRegister()
    {
        using var http = new HttpClient();
        await http.DeleteAsync($"{fixture.MailpitApiUrl}/api/v1/messages");

        const string email = "notify@example.com";
        await client.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest(email, "Password1!", "Notify User"));

        // Wolverine processes UserRegisteredV1 asynchronously — poll for up to 10 s.
        MailpitMessagesResponse? result = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            result = await http.GetFromJsonAsync<MailpitMessagesResponse>(
                $"{fixture.MailpitApiUrl}/api/v1/messages");

            if (result?.Messages?.Any(IsConfirmationEmailForRegisteredUser) == true)
            {
                break;
            }
        }

        Assert.NotNull(result);
        Assert.Contains(result.Messages ?? [], IsConfirmationEmailForRegisteredUser);

        bool IsConfirmationEmailForRegisteredUser(MailpitMessage message) =>
            string.Equals(message.Subject, "Confirm your email address", StringComparison.Ordinal) &&
            message.To?.Any(recipient => string.Equals(recipient.Address, email, StringComparison.OrdinalIgnoreCase)) == true;
    }

    // ── 10.1d ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenApiDocument_GeneratesSuccessfully()
    {
        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("organizations", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TickerQ", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ticker", json, StringComparison.OrdinalIgnoreCase);

        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal("3.1.1", doc.GetProperty("openapi").GetString());
        Assert.True(doc.TryGetProperty("paths", out var paths), "OpenAPI document should contain paths.");
        Assert.True(paths.TryGetProperty("/v1/households/my", out _));
        Assert.True(paths.TryGetProperty("/v1/economy/analytics/category-trend", out _));
        Assert.True(paths.TryGetProperty("/v1/economy/analytics/spend-breakdown", out _));
        Assert.True(paths.TryGetProperty("/v1/economy/analytics/period-comparison", out _));
        Assert.True(paths.TryGetProperty("/v1/economy/analytics/income-vs-expense", out _));
        Assert.True(paths.TryGetProperty("/v1/economy/analytics/variance-history", out _));
        Assert.True(paths.TryGetProperty("/v1/economy/analytics/top-transactions", out _));

        var pathNames = paths.EnumerateObject().Select(path => path.Name).ToArray();
        Assert.DoesNotContain(pathNames, path => path.StartsWith("/admin/jobs", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(pathNames, path => path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(pathNames, path => path.StartsWith("/v1/admin/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(pathNames, path => path.StartsWith("/v1/organizations", StringComparison.OrdinalIgnoreCase));

        var loginResponseSchema = doc
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(nameof(LoginResponse));
        var schemas = doc
            .GetProperty("components")
            .GetProperty("schemas");

        AssertEnumSchema(schemas, "HouseholdRole", ["owner", "member"]);
        AssertEnumSchema(schemas, "PlatformRole", ["admin", "user"]);
        AssertEnumSchema(schemas, "SubscriptionMatchState", ["actual", "predicted", "suggested"]);
        AssertEnumSchema(schemas, "Currency", ["SEK"]);
        Assert.Equal(
            "#/components/schemas/HouseholdRole",
            schemas.GetProperty("MyHouseholdItem").GetProperty("properties").GetProperty("role").GetProperty("$ref").GetString());
        Assert.Equal(
            "#/components/schemas/PlatformRole",
            schemas.GetProperty("GetCurrentUserResponse").GetProperty("properties").GetProperty("role").GetProperty("$ref").GetString());
        Assert.Equal(
            "#/components/schemas/SubscriptionMatchState",
            schemas.GetProperty("MonthChargeResponse").GetProperty("properties").GetProperty("matchState").GetProperty("$ref").GetString());
        AssertSchemaType(
            schemas.GetProperty(nameof(MoneyDto)).GetProperty("properties").GetProperty("amount"),
            "string");

        var loginStatusEnum = loginResponseSchema
            .GetProperty("properties")
            .GetProperty("status")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();

        Assert.Equal(
            [LoginResponseStatus.Authenticated, LoginResponseStatus.TwoFactorRequired],
            loginStatusEnum);
    }

    // ── Mailpit API response shapes ───────────────────────────────────────────

    private static void AssertEnumSchema(JsonElement schemas, string schemaName, string[] expected)
    {
        var values = schemas
            .GetProperty(schemaName)
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();

        Assert.Equal(expected, values);
    }

    private static void AssertSchemaType(JsonElement schema, string expected)
    {
        var type = schema.GetProperty("type");
        if (type.ValueKind == JsonValueKind.Array)
        {
            Assert.Contains(expected, type.EnumerateArray().Select(value => value.GetString()));
            return;
        }

        Assert.Equal(expected, type.GetString());
    }

    private sealed record MailpitMessagesResponse(MailpitMessage[]? Messages, int Total);
    private sealed record MailpitMessage(MailpitAddress[]? To, string? Subject);
    private sealed record MailpitAddress(string Address);
}
