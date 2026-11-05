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
