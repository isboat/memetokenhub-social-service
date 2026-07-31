using System.ComponentModel.DataAnnotations;
using MemeTokenHub.SocialService.Domain.Enums;

namespace MemeTokenHub.SocialService.Application.Models;

public sealed class CreateVoteRequest
{
    [EnumDataType(typeof(VoteValue))]
    public VoteValue Value { get; init; }
}
