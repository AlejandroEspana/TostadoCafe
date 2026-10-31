using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class TokenLogWatcher : IDisposable
    {
        private readonly List<string> messages = new List<string>();

        public TokenLogWatcher()
        {
            Application.logMessageReceived += Capture;
        }

        public void AssertNeverLogged(params string[] secrets)
        {
            foreach (var message in messages)
            {
                foreach (var secret in secrets)
                {
                    StringAssert.DoesNotContain(secret, message);
                }
            }
        }

        public void Dispose()
        {
            Application.logMessageReceived -= Capture;
        }

        private void Capture(string condition, string stackTrace, LogType type)
        {
            messages.Add(condition + stackTrace);
        }
    }
}
