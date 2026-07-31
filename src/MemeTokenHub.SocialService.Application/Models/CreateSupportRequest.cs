using System.ComponentModel.DataAnnotations;
namespace MemeTokenHub.SocialService.Application.Models; public sealed class CreateSupportRequest { [StringLength(1000)] public string? Statement { get; init; } }
