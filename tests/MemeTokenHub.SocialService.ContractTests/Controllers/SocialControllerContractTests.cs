using MemeTokenHub.SocialService.Api;
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
    [TestCase(nameof(SocialController.Support))]
    [TestCase(nameof(SocialController.WithdrawSupport))]
    public void KolSupportEndpoints_RequireSupportCapability(string methodName)
    {
        System.Reflection.MethodInfo method = typeof(SocialController).GetMethod(methodName) ?? throw new InvalidOperationException($"{methodName} was not found.");
        Microsoft.AspNetCore.Authorization.AuthorizeAttribute authorize = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single();

        Assert.That(authorize.Policy, Is.EqualTo(AuthorizationPolicies.WriteSupport));
    }

}
