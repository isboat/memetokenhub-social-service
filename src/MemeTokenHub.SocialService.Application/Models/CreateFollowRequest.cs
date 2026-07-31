using System.ComponentModel.DataAnnotations;
using MemeTokenHub.SocialService.Domain.Enums;
namespace MemeTokenHub.SocialService.Application.Models; public sealed class CreateFollowRequest { public FollowTargetType TargetType { get; init; } [Required, StringLength(100)] public required string TargetId { get; init; } }
