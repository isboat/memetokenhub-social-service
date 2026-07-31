using System.ComponentModel.DataAnnotations;
using MemeTokenHub.SocialService.Domain.Enums;
namespace MemeTokenHub.SocialService.Application.Models; public sealed class CreatePostRequest { public string? TokenId { get; init; } [Required, StringLength(5000, MinimumLength = 1)] public required string Content { get; init; } public IReadOnlyCollection<string> MediaUrls { get; init; } = []; public PostAccess Access { get; init; } }
