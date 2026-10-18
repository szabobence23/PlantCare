using PlantCare.Api.Validation;

namespace PlantCare.Tests.Unit;

/// <summary>AC1 – registration input rules.</summary>
[Trait("AC", "AC1")]
public class AuthInputValidatorTests
{
    [Fact]
    public void Valid_registration_has_no_errors()
    {
        Assert.Empty(AuthInputValidator.ValidateRegistration("anna@example.com", "jelszo123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nem-email")]
    [InlineData("anna@")]
    [InlineData("anna@example")]
    [InlineData("an na@example.com")]
    public void Invalid_email_is_rejected(string email)
    {
        Assert.Equal(["email"], AuthInputValidator.ValidateRegistration(email, "jelszo123").Keys);
    }

    [Theory]
    [InlineData("1234567", false)]  // 7 chars: one below the minimum
    [InlineData("12345678", true)]  // 8 chars: minimum (boundary)
    public void Password_length_boundary(string password, bool valid)
    {
        Assert.Equal(valid, AuthInputValidator.ValidateRegistration("anna@example.com", password).Count == 0);
    }

    [Fact]
    public void Email_is_normalized()
    {
        Assert.Equal("anna@example.com", AuthInputValidator.NormalizeEmail("  Anna@Example.COM "));
    }
}
