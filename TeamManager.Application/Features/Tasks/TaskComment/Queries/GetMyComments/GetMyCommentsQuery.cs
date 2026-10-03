using MediatR;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyComments
{
    public sealed record GetMyCommentsQuery(int Page = 1, int PageSize = 20) : IRequest<GetMyCommentsResponse>;
}