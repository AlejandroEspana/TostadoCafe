namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class FakeSessionTokenStore : ISessionTokenStore
    {
        public FakeSessionTokenStore(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public int SaveCount { get; private set; }

        public void Save(SessionTokens tokens)
        {
            AccessToken = tokens.accessToken;
            RefreshToken = tokens.refreshToken;
            SaveCount++;
        }
    }
}
