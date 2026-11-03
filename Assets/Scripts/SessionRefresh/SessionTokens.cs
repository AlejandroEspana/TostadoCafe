using System;

namespace ToastedVr.SessionRefresh
{
    [Serializable]
    public sealed class SessionRefreshRequest
    {
        public string refreshToken;
    }

    [Serializable]
    public sealed class SessionTokens
    {
        public string accessToken;
        public string tokenType;
        public string expiresAt;
        public string refreshToken;
    }

    public enum SessionRefreshOutcome
    {
        Renewed,
        Rejected,
        Unavailable
    }
}
