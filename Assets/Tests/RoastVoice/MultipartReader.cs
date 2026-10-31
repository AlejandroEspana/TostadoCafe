using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToastedVr.RoastVoice.Tests
{
    // Lee el multipart/form-data que recibe el backend falso, para comprobar que las partes salen como las espera Spring.
    public static class MultipartReader
    {
        public sealed class Part
        {
            public string Name;
            public string FileName;
            public string ContentType;
            public byte[] Body;
        }

        public static Dictionary<string, Part> Read(string contentType, byte[] body)
        {
            var boundary = contentType.Split(';')
                .Select(piece => piece.Trim())
                .First(piece => piece.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
                .Substring("boundary=".Length)
                .Trim('"');

            // Un carácter por byte, para poder cortar el binario sin alterarlo.
            var raw = new string(body.Select(b => (char)b).ToArray());
            var parts = new Dictionary<string, Part>();
            foreach (var chunk in raw.Split(new[] { "--" + boundary }, StringSplitOptions.None))
            {
                var headerEnd = chunk.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd < 0)
                {
                    continue;
                }

                var headers = chunk.Substring(0, headerEnd);
                var content = chunk.Substring(headerEnd + 4);
                if (content.EndsWith("\r\n", StringComparison.Ordinal))
                {
                    content = content.Substring(0, content.Length - 2);
                }

                var part = new Part
                {
                    Name = HeaderValue(headers, "name"),
                    FileName = HeaderValue(headers, "filename"),
                    ContentType = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => line.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                        .Select(line => line.Substring("Content-Type:".Length).Trim())
                        .FirstOrDefault(),
                    Body = content.Select(c => (byte)c).ToArray()
                };
                parts[part.Name] = part;
            }

            return parts;
        }

        public static string Text(Part part) => Encoding.UTF8.GetString(part.Body);

        private static string HeaderValue(string headers, string key)
        {
            var marker = key + "=\"";
            var start = headers.IndexOf(" " + marker, StringComparison.Ordinal);
            start = start < 0 ? headers.IndexOf(";" + marker, StringComparison.Ordinal) : start;
            if (start < 0)
            {
                return null;
            }

            start += marker.Length + 1;
            return headers.Substring(start, headers.IndexOf('"', start) - start);
        }
    }
}
