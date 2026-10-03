using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyComments
{
    public sealed class GetMyCommentsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetMyCommentsQuery, GetMyCommentsResponse>
    {
        public async Task<GetMyCommentsResponse> Handle(GetMyCommentsQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var query = context.Tasks.AsNoTracking().Where(x => x.DeletedAtUtc == null && x.Project.DeletedAtUtc == null)
                .SelectMany(x => x.Comments).Where(x => x.AuthorUserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var comments = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new GetMyCommentsItem(x.Id, x.TaskId, x.Task.Title, x.Content, x.CreatedAtUtc,
                x.UpdatedAtUtc, x.Mentions.Select(m => new MyCommentMentionResponse(m.MentionedUserId, m.MentionedUser.DisplayName))
                .ToList())).ToListAsync(cancellationToken);

            return new GetMyCommentsResponse(comments, totalCount, request.Page, request.PageSize);
        }
    }
}