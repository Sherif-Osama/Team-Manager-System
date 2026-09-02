using MediatR;

namespace TeamManager.Application.Features.Users.Queries.GetUserByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : IRequest<GetUserByEmailResponse>;
}