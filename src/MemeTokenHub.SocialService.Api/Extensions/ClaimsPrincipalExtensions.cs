using System.Security.Claims;

namespace MemeTokenHub.SocialService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("The authenticated user identifier is missing.");
}
