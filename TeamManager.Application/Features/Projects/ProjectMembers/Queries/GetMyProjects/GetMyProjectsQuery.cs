using MediatR;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetMyProjects
{
    public sealed record GetMyProjectsQuery(string? Search, ProjectRole? Role, ProjectStatus? Status, int Page = 1,
        int PageSize = 20) : IRequest<GetMyProjectsResponse>;
}