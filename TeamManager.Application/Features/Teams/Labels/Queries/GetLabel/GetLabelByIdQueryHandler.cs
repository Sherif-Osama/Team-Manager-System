using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabel
{
    public sealed class GetLabelByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetLabelByIdQuery, GetLabelByIdResponse>
    {
        public async Task<GetLabelByIdResponse> Handle(GetLabelByIdQuery request, CancellationToken cancellationToken)
        {
            var label = await context.Labels.AsNoTracking().Where(x => x.Id == request.LabelId && x.TeamId == request.TeamId)
                .Select(x => new GetLabelByIdResponse(x.Id, x.TeamId, x.Name, x.ColorHex, x.CreatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);

            if (label is null)
                throw new LabelNotFoundException(request.LabelId);

            return label;
        }
    }
}