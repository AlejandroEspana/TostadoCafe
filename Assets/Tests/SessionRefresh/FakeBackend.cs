using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class FakeBackend : IDisposable
    {
        public sealed class ReceivedRequest
        {
            public string Method;
            public string Path;
            public string Query;
            public string Authorization;
            public string ContentType;
            public string IfNoneMatch;
            public string Body;
            public byte[] BodyBytes;
        }

        private readonly HttpListener listener = new HttpListener();
        private readonly Func<ReceivedRequest, (int status, string body)> respond;
        private readonly Thread thread;

        public FakeBackend(Func<ReceivedRequest, (int status, string body)> respond)
        {
            this.respond = respond;
            BaseUrl = $"http://127.0.0.1:{FindFreePort()}";
            listener.Prefixes.Add(BaseUrl + "/");
            listener.Start();
            thread = new Thread(Serve) { IsBackground = true };
            thread.Start();
        }

        public string BaseUrl { get; }

        public ConcurrentQueue<ReceivedRequest> Requests { get; } = new ConcurrentQueue<ReceivedRequest>();

        public ConcurrentDictionary<string, string> ResponseHeaders { get; } = new ConcurrentDictionary<string, string>();

        public void Dispose()
        {
            listener.Close();
        }

        private void Serve()
        {
            while (listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = listener.GetContext();
                }
                catch (Exception)
                {
                    return;
                }

                byte[] bodyBytes;
                using (var buffer = new MemoryStream())
                {
                    context.Request.InputStream.CopyTo(buffer);
                    bodyBytes = buffer.ToArray();
                }

                var received = new ReceivedRequest
                {
                    Method = context.Request.HttpMethod,
                    Path = context.Request.Url.AbsolutePath,
                    Query = context.Request.Url.Query,
                    Authorization = context.Request.Headers["Authorization"],
                    ContentType = context.Request.ContentType,
                    IfNoneMatch = context.Request.Headers["If-None-Match"],
                    Body = Encoding.UTF8.GetString(bodyBytes),
                    BodyBytes = bodyBytes
                };
                Requests.Enqueue(received);

                var (status, responseBody) = respond(received);
                var bytes = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                foreach (var header in ResponseHeaders)
                {
                    context.Response.AddHeader(header.Key, header.Value);
                }

                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.OutputStream.Close();
            }
        }

        private static int FindFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }
    }
}
