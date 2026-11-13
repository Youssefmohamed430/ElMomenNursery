namespace ElMomenNursery.Application.IServices;

public interface ISessionManager
{
    UserSession? CurrentUser { get; }
    bool IsAuthenticated { get; }
    void Login(UserSession session);
    void Logout();
}

public sealed class SessionManager : ISessionManager
{
    public UserSession? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public void Login(UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (CurrentUser?.UserId == session.UserId)
            CurrentUser.RefreshAuthorization(session.Username, session.FullName, session.EmployeeId, session.Roles, session.Permissions);
        else CurrentUser = session;
    }
    public void Logout() => CurrentUser = null;
}
