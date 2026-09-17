using FluentValidation.TestHelper;
using Portfolio.Application.Auth.Dtos;
using Portfolio.Application.Auth.Validators;
using Xunit;

namespace Portfolio.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@example.com", "SuperSecret123!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "SuperSecret123!")]
    [InlineData("not-an-email", "SuperSecret123!")]
    public void Invalid_email_fails(string email, string password)
    {
        var result = _validator.TestValidate(new RegisterRequest(email, password));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short1!")]
    public void Invalid_password_fails(string password)
    {
        var result = _validator.TestValidate(new RegisterRequest("user@example.com", password));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
