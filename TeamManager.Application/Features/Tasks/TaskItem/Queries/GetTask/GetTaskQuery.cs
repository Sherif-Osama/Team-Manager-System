using MediatR;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTask
{
    public sealed record GetTaskQuery(long TaskId) : IRequest<GetTaskResponse>;
}