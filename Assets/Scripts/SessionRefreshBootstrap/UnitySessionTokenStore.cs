using ToastedVr.Authentication;
using ToastedVr.SessionRefresh;

namespace ToastedVr.SessionRefreshBootstrap
{
    public sealed class UnitySessionTokenStore : ISessionTokenStore
    {
        public string AccessToken => UnitySession.AccessToken;
        public string RefreshToken => UnitySession.RefreshToken;

        // Se guarda por el mismo SetSession del login para que la sesión quede igual que recién iniciada.
        public void Save(SessionTokens tokens)
        {
            UnitySession.SetSession(new UnityLoginResponse
            {
                accessToken = tokens.accessToken,
                tokenType = tokens.tokenType,
                expiresAt = tokens.expiresAt,
                refreshToken = tokens.refreshToken
            });
        }
    }
}
