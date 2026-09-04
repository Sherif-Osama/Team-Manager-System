using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Queries.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<GetMyProfileResponse>;
}
