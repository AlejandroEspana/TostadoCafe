using System;

namespace ToastedVr.Authentication
{
    [Serializable]
    public sealed class UnityLoginRequest
    {
        public string code;
    }

    [Serializable]
    public sealed class UnityLoginResponse
    {
        public string accessToken;
        public string tokenType;
        public string expiresAt;
        public string refreshToken;
    }
}
