using System;
using System.Collections;

namespace ToastedVr.SessionRefresh
{
    public interface ISessionRefreshApi
    {
        IEnumerator Refresh(string refreshToken, Action<SessionRefreshOutcome, SessionTokens> onCompleted);
    }
}
