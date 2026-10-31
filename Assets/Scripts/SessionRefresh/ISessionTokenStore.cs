namespace ToastedVr.SessionRefresh
{
    public interface ISessionTokenStore
    {
        string AccessToken { get; }
        string RefreshToken { get; }
        void Save(SessionTokens tokens);
    }
}
