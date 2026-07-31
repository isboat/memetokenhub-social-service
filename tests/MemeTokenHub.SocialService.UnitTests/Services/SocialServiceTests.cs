using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Enums;
using NSubstitute;
using SocialApplicationService = MemeTokenHub.SocialService.Application.Services.SocialService;

namespace MemeTokenHub.SocialService.UnitTests.Services;

[TestFixture]
public sealed class SocialServiceTests
{
    [Test]
    public void FollowAsync_WhenUserFollowsThemselves_RejectsRequest()
    {
        ISocialRepository repository = Substitute.For<ISocialRepository>();
        IEventPublisher eventPublisher = Substitute.For<IEventPublisher>();
        SocialApplicationService service = new(repository, eventPublisher);
        CreateFollowRequest request = new() { TargetType = FollowTargetType.User, TargetId = "user-1" };

        Assert.ThrowsAsync<InvalidOperationException>(() => service.FollowAsync("user-1", request, CancellationToken.None));
    }

    [Test]
    public async Task GetFollowsAsync_WhenLimitExceedsMaximum_ClampsLimit()
    {
        ISocialRepository repository = Substitute.For<ISocialRepository>();
        IEventPublisher eventPublisher = Substitute.For<IEventPublisher>();
        repository.GetFollowsAsync("user-1", null, 100, 0, Arg.Any<CancellationToken>()).Returns(new PagedResult<Domain.Entities.Follow> { Items = [], Limit = 100, Offset = 0, Total = 0 });
        SocialApplicationService service = new(repository, eventPublisher);

        PagedResult<Domain.Entities.Follow> result = await service.GetFollowsAsync("user-1", null, 500, -3, CancellationToken.None);

        Assert.That(result.Limit, Is.EqualTo(100));
        await repository.Received(1).GetFollowsAsync("user-1", null, 100, 0, Arg.Any<CancellationToken>());
    }
}
