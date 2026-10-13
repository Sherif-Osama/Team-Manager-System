namespace TeamManager.Application.Common.ActivityLog
{
    public static class ActivityTypes
    {
        public const string TaskStatusChanged = "task.status_changed";
        public const string TaskCreated = "task.created";
        public const string CommentAdded = "comment.added";
        public const string TaskAssigned = "task.assigned";
        public const string TeamInvitationCreated = "team.invitation.created";
        public const string TeamInvitationAccepted = "team.invitation.accepted";
        public const string TeamInvitationRejected = "team.invitation.rejected";
        public const string TeamInvitationCancelled = "team.invitation.cancelled";
    }
}