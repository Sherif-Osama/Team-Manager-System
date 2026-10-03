using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments
{
    public sealed class GetTaskCommentsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetTaskCommentsQuery, GetTaskCommentsResponse>
    {
        public async Task<GetTaskCommentsResponse> Handle(GetTaskCommentsQuery request, CancellationToken cancellationToken)
        {
            var query = context.Tasks.AsNoTracking().Where(x => x.Id == request.TaskId).SelectMany(x => x.Comments);

            var totalCount = await query.CountAsync(cancellationToken);

            var comments = await query.OrderBy(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new GetTaskCommentsItem(x.Id, x.AuthorUserId, x.Author.DisplayName,
                x.Content, x.CreatedAtUtc, x.UpdatedAtUtc,
                x.Mentions.Select(m => new CommentMentionResponse(m.MentionedUserId, m.MentionedUser.DisplayName)).ToList()))
                .ToListAsync(cancellationToken);

            return new GetTaskCommentsResponse(comments, totalCount, request.Page, request.PageSize);
        }
    }
}