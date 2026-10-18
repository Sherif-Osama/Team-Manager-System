namespace TeamManager.Application.Common.Caching
{
    public static class CacheTags
    {
        public static string UserPermissions(Guid userId) => $"teammanager:permissions:user:{userId:N}";
        public static string AllUserPermissions => "teammanager:permissions:all";
        public static string TeamLabels(Guid teamId) => $"teammanager:labels:team:{teamId:N}";
        public static string TeamAuthorization(Guid teamId) => $"teammanager:auth:team:{teamId:N}";
        public static string ProjectAuthorization(Guid projectId) => $"teammanager:auth:project:{projectId:N}";
        public static string UserAuthorization(Guid userId) => $"teammanager:auth:user:{userId:N}";
    }
}