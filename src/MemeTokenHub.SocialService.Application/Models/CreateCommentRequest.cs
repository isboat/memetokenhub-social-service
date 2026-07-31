using System.ComponentModel.DataAnnotations;
namespace MemeTokenHub.SocialService.Application.Models; public sealed class CreateCommentRequest { [Required, StringLength(2000, MinimumLength = 1)] public required string Content { get; init; } }
