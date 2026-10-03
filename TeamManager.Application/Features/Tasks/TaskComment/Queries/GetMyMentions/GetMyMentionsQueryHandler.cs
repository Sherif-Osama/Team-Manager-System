using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyMentions
{
    public sealed class GetMyMentionsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetMyMentionsQuery, GetMyMentionsResponse>
    {
        public async Task<GetMyMentionsResponse> Handle(GetMyMentionsQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var query = context.Tasks.AsNoTracking().Where(x => x.DeletedAtUtc == null && x.Project.DeletedAtUtc == null)
                .SelectMany(x => x.Comments).SelectMany(comment => comment.Mentions.Where(m => m.MentionedUserId == userId)
                        .Select(m => new
                        {
                            Comment = comment,
                            Mention = m
                        }));

            var totalCount = await query.CountAsync(cancellationToken);

            var mentions = await query.OrderByDescending(x => x.Mention.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new GetMyMentionsItem(x.Comment.Id, x.Comment.TaskId, x.Comment.Task.Title,
                    x.Comment.AuthorUserId, x.Comment.Author.DisplayName, x.Comment.Content, x.Comment.CreatedAtUtc,
                    x.Comment.UpdatedAtUtc)).ToListAsync(cancellationToken);

            return new GetMyMentionsResponse(mentions, totalCount, request.Page, request.PageSize);
        }
    }
}