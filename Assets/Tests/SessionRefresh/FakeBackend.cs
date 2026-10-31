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
            public string Body;
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

                string body;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    body = reader.ReadToEnd();
                }

                var received = new ReceivedRequest
                {
                    Method = context.Request.HttpMethod,
                    Path = context.Request.Url.AbsolutePath,
                    Query = context.Request.Url.Query,
                    Authorization = context.Request.Headers["Authorization"],
                    Body = body
                };
                Requests.Enqueue(received);

                var (status, responseBody) = respond(received);
                var bytes = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
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
