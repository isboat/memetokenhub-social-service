using MemeTokenHub.SocialService.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace MemeTokenHub.SocialService.ContractTests.Controllers;

[TestFixture]
public sealed class SocialControllerContractTests
{
    [Test]
    public void Controller_UsesExpectedRoutePrefix()
    {
        RouteAttribute? route = typeof(SocialController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().SingleOrDefault();
        Assert.That(route?.Template, Is.EqualTo("api/social"));
    }

    [Test]
    public void Controller_IsAnApiController()
    {
        bool hasApiControllerAttribute = typeof(SocialController).IsDefined(typeof(ApiControllerAttribute), true);
        Assert.That(hasApiControllerAttribute, Is.True);
    }
}
