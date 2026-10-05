using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskAttachment.Commands.AddTaskAttachment;

namespace TeamManager.Api.Controllers.Tasks
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskAttachmentsController(ISender sender) : ControllerBase
    {
        [HttpPost("{taskId:long}/attachments")]
        [Authorize]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<long>> AddAttachment(long taskId, IFormFile file, CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream();

            var attachmentId = await sender.Send(
                new AddTaskAttachmentCommand(taskId, file.FileName, file.ContentType, file.Length, stream), cancellationToken);

            return Ok(attachmentId);
        }
    }
}
