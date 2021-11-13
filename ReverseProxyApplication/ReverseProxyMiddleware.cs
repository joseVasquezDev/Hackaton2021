using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxyApplication
{
    public class ReverseProxyMiddleware
    {
        private static readonly HttpClient _httpClient = new();
        private readonly RequestDelegate _nextMiddleware;
        private readonly MemoryCache _cache;

        public ReverseProxyMiddleware(RequestDelegate nextMiddleware, LocalMemoryCache memoryCache)
        {
            _nextMiddleware = nextMiddleware;
            _cache = memoryCache.Cache;
        }

        public async Task Invoke(HttpContext context)
        {
            var targetUri = BuildTargetUri(context.Request);

            if (targetUri != null)
            {
                //if (await CheckCachedRequestAsync(context))
                //{
                //    return;
                //}

                var targetRequestMessage = CreateTargetMessage(context, targetUri);
                var politicaReintentos = Policy.Handle<Exception>().WaitAndRetryAsync(3, intentos => TimeSpan.FromSeconds(Math.Pow(2, intentos)));

                using var responseMessage = await politicaReintentos.ExecuteAsync(async () =>
                    await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted));

                context.Response.StatusCode = (int)responseMessage.StatusCode;

                CopyFromTargetResponseHeaders(context, responseMessage);

                await ProcessResponseContent(context, responseMessage);
                //await ProcessResponseCache(context, responseMessage);

                return;
            }

            await _nextMiddleware(context);
        }

        private async Task<bool> CheckCachedRequestAsync(HttpContext context)
        {
            string cacheKey = context.Request.QueryString.Value;

            if (_cache.TryGetValue(cacheKey, out string cacheEntry))
            {
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(cacheEntry));
                return true;
            }

            return false;
        }

        private async Task ProcessResponseCache(HttpContext context, HttpResponseMessage responseMessage)
        {
            if (context.Response.StatusCode == (int)HttpStatusCode.OK)
            {
                string cacheKey = context.Request.QueryString.Value;

                var contentEntry = await responseMessage.Content.ReadFromJsonAsync<ResponseModels>();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetSize(1)
                       .SetSlidingExpiration(TimeSpan.FromSeconds(contentEntry.ValiditySeconds));

                _cache.Set(cacheKey, JsonConvert.SerializeObject(contentEntry), cacheEntryOptions);
            }
        }

        private static async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(content);
        }

        private static HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();
            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }

        private static void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in context.Request.Headers)
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private static void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            context.Response.Headers.Remove("transfer-encoding");
        }

        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }

        private static Uri BuildTargetUri(HttpRequest request)
        {
            //return new Uri($"http://api-0.hack.local/{request.QueryString.Value}");
            string hostName = Environment.GetEnvironmentVariable("SVC_API_HOSTNAME");
            string port = Environment.GetEnvironmentVariable("SVC_API_PORT");
            string urlString = string.Format("http://{0}:{1}/{2}", hostName, port, request.QueryString.Value);
            return new Uri(urlString);
        }
    }
}
