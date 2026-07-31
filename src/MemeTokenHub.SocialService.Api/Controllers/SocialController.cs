using MemeTokenHub.SocialService.Api.Extensions;
using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Entities;
using MemeTokenHub.SocialService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemeTokenHub.SocialService.Api.Controllers;

[ApiController]
[Route("api/social")]
public sealed class SocialController(ISocialService socialService) : ControllerBase
{
    /// <summary>Follows a user, token, or network as the authenticated user.</summary>
    [Authorize, HttpPost("follows")]
    public async Task<ActionResult<Follow>> Follow(CreateFollowRequest request, CancellationToken cancellationToken) => Ok(await socialService.FollowAsync(User.GetRequiredUserId(), request, cancellationToken));

    /// <summary>Stops following a user, token, or network.</summary>
    [Authorize, HttpDelete("follows/{targetType}/{targetId}")]
    public async Task<IActionResult> Unfollow(FollowTargetType targetType, string targetId, CancellationToken cancellationToken) { await socialService.UnfollowAsync(User.GetRequiredUserId(), targetType, targetId, cancellationToken); return NoContent(); }

    /// <summary>Returns targets tracked by the authenticated user.</summary>
    [Authorize, HttpGet("follows/me")]
    public async Task<ActionResult<PagedResult<Follow>>> GetMyFollows(FollowTargetType? targetType, int limit = 20, int offset = 0, CancellationToken cancellationToken = default) => Ok(await socialService.GetFollowsAsync(User.GetRequiredUserId(), targetType, limit, offset, cancellationToken));

    /// <summary>Likes a token as the authenticated user.</summary>
    [Authorize, HttpPost("tokens/{tokenId}/like")]
    public async Task<ActionResult<Engagement>> Like(string tokenId, CancellationToken cancellationToken) => Ok(await socialService.LikeAsync(User.GetRequiredUserId(), tokenId, cancellationToken));

    /// <summary>Adds a comment to a token.</summary>
    [Authorize, HttpPost("tokens/{tokenId}/comment")]
    public async Task<ActionResult<Engagement>> Comment(string tokenId, CreateCommentRequest request, CancellationToken cancellationToken) => Ok(await socialService.CommentAsync(User.GetRequiredUserId(), tokenId, request, cancellationToken));

    /// <summary>Returns likes and comments for a token.</summary>
    [AllowAnonymous, HttpGet("tokens/{tokenId}/engagement")]
    public async Task<ActionResult<PagedResult<Engagement>>> GetEngagement(string tokenId, int limit = 20, int offset = 0, CancellationToken cancellationToken = default) => Ok(await socialService.GetEngagementAsync(tokenId, limit, offset, cancellationToken));

    /// <summary>Casts or replaces the authenticated user's sentiment vote.</summary>
    [Authorize, HttpPost("tokens/{tokenId}/vote")]
    public async Task<ActionResult<TokenVote>> Vote(string tokenId, CreateVoteRequest request, CancellationToken cancellationToken) => Ok(await socialService.VoteAsync(User.GetRequiredUserId(), tokenId, request, cancellationToken));

    /// <summary>Removes the authenticated user's sentiment vote.</summary>
    [Authorize, HttpDelete("tokens/{tokenId}/vote")]
    public async Task<IActionResult> RemoveVote(string tokenId, CancellationToken cancellationToken) { await socialService.RemoveVoteAsync(User.GetRequiredUserId(), tokenId, cancellationToken); return NoContent(); }

    /// <summary>Returns sentiment totals and the viewer's vote when authenticated.</summary>
    [AllowAnonymous, HttpGet("tokens/{tokenId}/vote")]
    public async Task<ActionResult<VoteSummary>> GetVote(string tokenId, CancellationToken cancellationToken) => Ok(await socialService.GetVoteAsync(tokenId, User.Identity?.IsAuthenticated == true ? User.GetRequiredUserId() : null, cancellationToken));

    /// <summary>Creates a timestamped KOL endorsement.</summary>
    [Authorize, HttpPost("tokens/{tokenId}/support")]
    public async Task<ActionResult<TokenSupport>> Support(string tokenId, CreateSupportRequest request, CancellationToken cancellationToken) => Ok(await socialService.SupportAsync(User.GetRequiredUserId(), tokenId, request, cancellationToken));

    /// <summary>Withdraws an endorsement without removing its history.</summary>
    [Authorize, HttpDelete("tokens/{tokenId}/support")]
    public async Task<IActionResult> WithdrawSupport(string tokenId, CancellationToken cancellationToken) { await socialService.WithdrawSupportAsync(User.GetRequiredUserId(), tokenId, cancellationToken); return NoContent(); }

    /// <summary>Returns active and optionally withdrawn token endorsements.</summary>
    [AllowAnonymous, HttpGet("tokens/{tokenId}/supporters")]
    public async Task<ActionResult<PagedResult<TokenSupport>>> GetSupporters(string tokenId, bool includeWithdrawn = false, int limit = 20, int offset = 0, CancellationToken cancellationToken = default) => Ok(await socialService.GetSupportersAsync(tokenId, includeWithdrawn, limit, offset, cancellationToken));

    /// <summary>Publishes a community post as the authenticated user.</summary>
    [Authorize, HttpPost("posts")]
    public async Task<ActionResult<SocialPost>> CreatePost(CreatePostRequest request, CancellationToken cancellationToken) { SocialPost post = await socialService.CreatePostAsync(User.GetRequiredUserId(), request, cancellationToken); return CreatedAtAction(nameof(GetPost), new RouteValueDictionary { ["postId"] = post.Id }, post); }

    /// <summary>Returns a visible community post by identifier.</summary>
    [AllowAnonymous, HttpGet("posts/{postId}")]
    public async Task<ActionResult<SocialPost>> GetPost(string postId, CancellationToken cancellationToken) { SocialPost? post = await socialService.GetPostAsync(postId, cancellationToken); return post is null ? NotFound() : Ok(post); }

    /// <summary>Returns a filtered page of community posts.</summary>
    [AllowAnonymous, HttpGet("posts")]
    public async Task<ActionResult<PagedResult<SocialPost>>> GetPosts(string? authorId, string? tokenId, int limit = 20, int offset = 0, CancellationToken cancellationToken = default) => Ok(await socialService.GetPostsAsync(authorId, tokenId, limit, offset, cancellationToken));

    /// <summary>Returns a user's reputation score and badges.</summary>
    [AllowAnonymous, HttpGet("reputation/{userId}")]
    public async Task<ActionResult<Reputation>> GetReputation(string userId, CancellationToken cancellationToken) { Reputation? reputation = await socialService.GetReputationAsync(userId, cancellationToken); return reputation is null ? NotFound() : Ok(reputation); }
}
