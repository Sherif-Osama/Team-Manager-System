namespace TeamManager.IntegrationTest.Projects.Configuration
{
    static class ProjectEndpoints
    {
        public const string Projects = "/api/projects";
        public const string ProjectByName = "/api/projects/by-name";
        public const string MyProjects = "/api/projectmembers/my";

        public static string CreateProject(Guid teamId) => $"/api/teams/{teamId}/projects";
        public static string CreateTask(Guid projectId) => $"/api/projects/{projectId}/tasks";
        public static string Task(long taskId) => $"/api/tasks/{taskId}";
        public static string TaskStatus(long taskId) => $"/api/tasks/{taskId}/status";
        public static string TaskSchedule(long taskId) => $"/api/tasks/{taskId}/schedule";
        public static string TaskPriority(long taskId) => $"/api/tasks/{taskId}/priority";
        public static string TaskAssignee(long taskId) => $"/api/tasks/{taskId}/assignee";
        public static string TaskDependencies(long taskId) => $"/api/tasks/{taskId}/dependencies";
        public static string TaskDependency(long taskId, long dependencyId) => $"/api/tasks/{taskId}/dependencies/{dependencyId}";
        public static string TaskChecklist(long taskId) => $"/api/tasks/{taskId}/checklist";
        public static string TaskChecklistItem(long taskId, long itemId) => $"/api/tasks/{taskId}/checklist/{itemId}";
        public static string TaskChecklistItemComplete(long taskId, long itemId) =>
            $"{TaskChecklistItem(taskId, itemId)}/complete";
        public static string TaskChecklistItemReopen(long taskId, long itemId) =>
            $"{TaskChecklistItem(taskId, itemId)}/reopen";
        public static string TaskComments(long taskId) => $"/api/tasks/{taskId}/comments";
        public static string TaskComment(long taskId, long commentId) => $"/api/tasks/{taskId}/comments/{commentId}";
        public const string MyComments = "/api/tasks/my-comments";
        public const string MyMentions = "/api/tasks/my-mentions";
        public const string MyTasks = "/api/tasks/my";
        public static string TeamTasks(Guid teamId) => $"/api/teams/{teamId}/tasks";
        public static string Project(Guid projectId) => $"/api/projects/{projectId}";
        public static string ProjectSchedule(Guid projectId) => $"/api/projects/{projectId}/schedule";
        public static string ProjectStatus(Guid projectId) => $"/api/projects/{projectId}/status";
        public static string ProjectOwner(Guid projectId) => $"/api/projects/{projectId}/owner";
        public static string ProjectActivity(Guid projectId) => $"/api/projects/{projectId}/activity";

        public static string Members(Guid projectId) => $"/api/projectmembers/{projectId}/members";
        public static string Member(Guid projectId, long memberId) => $"/api/projectmembers/{projectId}/members/{memberId}";
        public static string MemberRole(Guid projectId, long memberId) => $"/api/projectmembers/{projectId}/members/{memberId}/role";
    }
}
