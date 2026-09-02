using MediatR;

namespace TeamManager.Application.Features.Users.Queries.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<GetMyProfileResponse>;
}
