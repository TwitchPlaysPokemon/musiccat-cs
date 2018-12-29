using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ApiListener
{
    // TODO: Command namespaces?
    public static class ApiListener
    {
        private static HttpListener HttpListener = new HttpListener();

        public static void Start(int port)
        {
            if (!HttpListener.IsSupported)
            {
                throw new Exception("HTTPListener is not supported on this system.");
            }
            var listenTo = $"http://localhost:{port}/";
            if (!HttpListener.Prefixes.Contains(listenTo))
            {
                HttpListener.Prefixes.Add(listenTo);
            }
            if (!HttpListener.IsListening)
            {
                HttpListener.Start();
                Listen();
            }
        }

        public static void Stop() => HttpListener.Stop();

        public static void HardStop() => HttpListener.Abort();

        private static void Listen() => HttpListener.BeginGetContext(HttpListenHandler, HttpListener);

        private static void HttpListenHandler(IAsyncResult result)
        {
            try
            {
                var context = HttpListener.EndGetContext(result);

                string body = null;
                if (context.Request.HasEntityBody)
                {
                    using (System.IO.Stream bodyStream = context.Request.InputStream)
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(bodyStream, context.Request.ContentEncoding))
                        {
                            body = reader.ReadToEnd();
                        }
                    }
                }

                byte[] responseBuffer;

                var response = "ok";
                string command = "Unknown Command";
                try
                {
                    var urlParams = new List<string>(context.Request.RawUrl.Split(new char[] { '/' })).Select(us => Uri.UnescapeDataString(us)).ToList();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        urlParams.Add(body);
                    }
                    if (string.IsNullOrWhiteSpace(urlParams.FirstOrDefault()))
                    {
                        urlParams.RemoveAt(0);
                    }
                    if (string.IsNullOrWhiteSpace(urlParams.LastOrDefault()) && urlParams.Any())
                    {
                        urlParams.RemoveAt(urlParams.Count - 1);
                    }

                    if (!urlParams.Any())
                    {
                        response = Documentation.Function(null);
                    }
                    else
                    {
                        command = urlParams[0];
                        urlParams.RemoveAt(0);

                        if (!Commands.ContainsKey(command))
                        {
                            throw new ApiError($"Invalid Command \"{command}\"");
                        }

                        command = Commands[command]?.Name; //normalize name for display during errors
                        response = Commands[command].Function(urlParams) ?? response;
                    }
                }
                catch (Exception e)
                {
                    response = $"{command}: {e.Message}";
                    context.Response.StatusCode = e is ApiError ? 400 : 500;
                }
                responseBuffer = Encoding.UTF8.GetBytes(response);
                context.Response.ContentType = "text/plain";
                // Get a response stream and write the response to it.
                context.Response.ContentLength64 = responseBuffer.Length;
                using (System.IO.Stream output = context.Response.OutputStream)
                {
                    output.Write(responseBuffer, 0, responseBuffer.Length);
                    output.Close();
                }
            }
            catch { }
            finally
            {
                Listen();
            }
        }

        private static IEnumerable<ApiProvider> apiProviders;

        public static Dictionary<string, ApiCommand> Commands = new Dictionary<string, ApiCommand>(StringComparer.OrdinalIgnoreCase);

        private static ApiCommand Documentation;

        static ApiListener()
        {
            apiProviders = ReflectiveEnumerator.GetEnumerableOfType<ApiProvider>();
            Documentation = new ApiCommand("Help", args => string.Join("\n", Commands.Select(c => c.Value.BuildDocString())), new List<ApiParameter>(), "Lists available commands (This message)");
            Commands["Help"] = Documentation;
            foreach (var provider in apiProviders)
            {
                foreach (var command in provider.Commands)
                {
                    Commands.Add(command.Name, command);
                }
            }
        }

    }
}
