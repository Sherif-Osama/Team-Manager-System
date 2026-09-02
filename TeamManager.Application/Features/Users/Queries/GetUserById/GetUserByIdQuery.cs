using MediatR;

namespace TeamManager.Application.Features.Users.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IRequest<GetUserByIdResponse>;
}