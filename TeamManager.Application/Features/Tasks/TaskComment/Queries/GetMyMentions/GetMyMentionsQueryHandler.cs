using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Enums;

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

            var query = context.Tasks.AsNoTracking().Where(x => x.DeletedAtUtc == null && x.Project.DeletedAtUtc == null
            && x.Project.Members.Any(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active && m.User.IsActive))
                .SelectMany(x => x.Comments).Where(x => x.Mentions.Any(m => m.MentionedUserId == userId));

            var totalCount = await query.CountAsync(cancellationToken);

            // UQ_CommentMentions_Comment_User keeps at most one mention row per user per comment, so the ordering key
            // is that row's date.
            var mentions = await query.OrderByDescending(x => x.Mentions.Where(m => m.MentionedUserId == userId).Max(m => m.CreatedAtUtc))
                .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
                .Select(x => new GetMyMentionsItem(x.Id, x.TaskId, x.Task.Title, x.AuthorUserId, x.Author.DisplayName, x.Content,
                x.CreatedAtUtc, x.UpdatedAtUtc)).ToListAsync(cancellationToken);

            return new GetMyMentionsResponse(mentions, totalCount, request.Page, request.PageSize);
        }
    }
}