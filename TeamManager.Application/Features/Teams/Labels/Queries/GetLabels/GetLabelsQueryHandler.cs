using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed class GetLabelsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetLabelsQuery, GetLabelsResponse>
    {
        public async Task<GetLabelsResponse> Handle(GetLabelsQuery request, CancellationToken cancellationToken)
        {
            var labels = context.Labels.AsNoTracking().Where(x => x.TeamId == request.TeamId);

            if (!string.IsNullOrWhiteSpace(request.Search))
                labels = labels.Where(x => x.Name.Contains(request.Search.Trim()));

            var totalCount = await labels.CountAsync(cancellationToken);

            var items = await labels.OrderBy(x => x.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
                .Select(x => new GetLabelsItemResponse(x.Id, x.Name, x.ColorHex, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetLabelsResponse(items, request.Page, request.PageSize, totalCount);
        }
    }
}