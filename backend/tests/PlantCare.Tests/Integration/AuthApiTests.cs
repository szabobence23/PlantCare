using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts;

namespace PlantCare.Tests.Integration;

/// <summary>AC1 – registration and login.</summary>
[Trait("AC", "AC1")]
public class AuthApiTests(PlantCareApiFactory factory) : ApiTestBase(factory)
{
    private static string NewEmail() => $"anna-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Valid_registration_creates_account_and_user_can_log_in()
    {
        var client = CreateAnonymousClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "jelszo123" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.False(string.IsNullOrEmpty((await ReadAsync<AuthResponse>(register)).Token));

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = email.ToUpperInvariant(), password = "jelszo123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await ReadAsync<AuthResponse>(login);
        Assert.Equal(email, auth.Email);

        // The token really authenticates.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/plants")).StatusCode);
    }

    [Theory]
    [InlineData("nem-email", "jelszo123", "email")]
    [InlineData("anna@example.com", "rovid", "password")]
    [InlineData("", "", "email,password")]
    public async Task Invalid_or_missing_registration_data_is_rejected_with_field_errors(string email, string password, string fields)
    {
        var response = await CreateAnonymousClient().PostAsJsonAsync("/api/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadValidationErrorsAsync(response);
        Assert.Equal(fields.Split(','), problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task Empty_body_is_rejected()
    {
        var response = await CreateAnonymousClient().PostAsync("/api/auth/register",
            new StringContent("", null, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Email_already_registered_is_rejected_case_insensitively()
    {
        var client = CreateAnonymousClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "jelszo123" });

        var again = await client.PostAsJsonAsync("/api/auth/register", new { email = email.ToUpperInvariant(), password = "masikjelszo" });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Contains("már létezik", (await ReadAsync<ProblemDetails>(again)).Detail);
    }

    [Theory]
    [InlineData("rosszjelszo")]
    [InlineData("JELSZO123")]
    public async Task Wrong_password_is_rejected(string password)
    {
        var client = CreateAnonymousClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "jelszo123" });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("Hibás e-mail cím vagy jelszó.", (await ReadAsync<ProblemDetails>(login)).Detail);
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_error_as_wrong_password()
    {
        var login = await CreateAnonymousClient().PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = "jelszo123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("Hibás e-mail cím vagy jelszó.", (await ReadAsync<ProblemDetails>(login)).Detail);
    }

    [Theory]
    [InlineData("GET", "/api/plants")]
    [InlineData("POST", "/api/plant-recognition")]
    [InlineData("DELETE", "/api/plants/8d1f5b1e-0000-0000-0000-000000000000")]
    public async Task Protected_endpoints_require_a_valid_token(string method, string url)
    {
        var client = CreateAnonymousClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url))).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "nem.ervenyes.token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url))).StatusCode);
    }
}
