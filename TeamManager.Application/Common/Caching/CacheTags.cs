namespace TeamManager.Application.Common.Caching
{
    public static class CacheTags
    {
        public static string UserPermissions(Guid userId) => $"teammanager:permissions:user:{userId:N}";
        public static string TeamLabels(Guid teamId) => $"teammanager:labels:team:{teamId:N}";
        public static string Task(long taskId) => $"teammanager:task:{taskId}";
        public static string TaskChecklist(long taskId) => $"teammanager:task:{taskId}:checklist";
        public static string TaskAttachments(long taskId) => $"teammanager:task:{taskId}:attachments";
        public static string TaskComments(long taskId) => $"teammanager:task:{taskId}:comments";
    }
}