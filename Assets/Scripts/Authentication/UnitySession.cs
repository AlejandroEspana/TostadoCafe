namespace ToastedVr.Authentication
{
    public static class UnitySession
    {
        public static string AccessToken { get; private set; }
        public static string TokenType { get; private set; }
        public static string ExpiresAt { get; private set; }
        public static string RefreshToken { get; private set; }

        public static void SetSession(UnityLoginResponse response)
        {
            AccessToken = response.accessToken;
            TokenType = response.tokenType;
            ExpiresAt = response.expiresAt;
            RefreshToken = response.refreshToken;
        }

        public static void Clear()
        {
            AccessToken = null;
            TokenType = null;
            ExpiresAt = null;
            RefreshToken = null;
        }
    }
}
