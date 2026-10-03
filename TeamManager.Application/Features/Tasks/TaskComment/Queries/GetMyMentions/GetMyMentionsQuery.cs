using MediatR;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyMentions
{
    public sealed record GetMyMentionsQuery(int Page = 1, int PageSize = 20) : IRequest<GetMyMentionsResponse>;
}