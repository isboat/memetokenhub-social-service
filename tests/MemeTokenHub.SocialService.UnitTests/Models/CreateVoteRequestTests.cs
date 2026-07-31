using System.ComponentModel.DataAnnotations;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Enums;

namespace MemeTokenHub.SocialService.UnitTests.Models;

[TestFixture]
public sealed class CreateVoteRequestTests
{
    [Test]
    public void Validation_WhenVoteValueIsUndefined_ReturnsValidationError()
    {
        CreateVoteRequest request = new() { Value = (VoteValue)999 };
        List<ValidationResult> results = [];

        bool isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.That(isValid, Is.False);
    }
}
