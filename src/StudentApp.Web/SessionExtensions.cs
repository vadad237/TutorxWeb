namespace StudentApp.Web;

public static class SessionExtensions
{
    public static void SetActiveGroup(this ISession session, int groupId)
        => session.SetInt32("ActiveGroupId", groupId);

    public static int? GetActiveGroup(this ISession session)
        => session.GetInt32("ActiveGroupId");
}
