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
        public const string TeamMemberRemoved = "team.member.removed";
        public const string ProjectMemberRemoved = "project.member.removed";
        public const string TeamMemberRoleChanged = "team.member.role_changed";
        public const string ProjectMemberRoleChanged = "project.member.role_changed";
        public const string TeamOwnershipTransferred = "team.ownership.transferred";
        public const string ProjectOwnershipTransferred = "project.ownership.transferred";
    }
}