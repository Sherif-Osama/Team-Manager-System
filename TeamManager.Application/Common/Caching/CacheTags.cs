namespace TeamManager.Application.Common.Caching
{
    public static class CacheTags
    {
        public static string UserPermissions(Guid userId) => $"teammanager:permissions:user:{userId:N}";
        public static string AllUserPermissions => "teammanager:permissions:all";
        public static string TeamLabels(Guid teamId) => $"teammanager:labels:team:{teamId:N}";
    }
}