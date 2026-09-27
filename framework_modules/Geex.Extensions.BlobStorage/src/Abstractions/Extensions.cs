using System.Linq;
using System.Net;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RestSharp.Extensions;


// ReSharper disable once CheckNamespace
namespace Geex.Extensions.BlobStorage.Extensions
{
    internal static class MicrosoftAspNetCoreBuilderExtension
    {
        public static void UseFileDownload(this IEndpointRouteBuilder endpoints)
        {
            endpoints.Map(endpoints.ServiceProvider.GetRequiredService<BlobStorageModuleOptions>().FileDownloadPath,
                context => DownloadAsync(context, id => context.RequestServices.GetRequiredService<IUnitOfWork>()
                    .Query<IBlobObject>().FirstOrDefault(x => x.Id == id)));
        }

        internal static async Task DownloadAsync(HttpContext context, Func<string, IBlobObject?> findBlob)
        {
            var response = context.Response;
            try
            {
                if (!context.Request.Query.TryGetValue("fileId", out var fileId) || string.IsNullOrWhiteSpace(fileId.ToString()))
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
                var blobObject = findBlob(fileId.ToString());
                if (blobObject == null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                await using var stream = await blobObject.StreamFromStorage(context.RequestAborted);
                context.RequestAborted.ThrowIfCancellationRequested();
                response.ContentType = blobObject.MimeType;
                response.ContentLength = blobObject.FileSize;
                response.Headers.ContentDisposition = $"Attachment;FileName*=utf-8''{blobObject.FileName.UrlEncode()}";
                response.Headers.Append("Cache-Control", "public,max-age=86400");
                response.Headers.Append("ETag", blobObject.Md5);
                // Keep headers uncommitted until the first successful read and write.
                await stream.CopyToAsync(response.Body, context.RequestAborted);
                await response.CompleteAsync();
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Abort();
            }
            catch (Exception exception)
            {
                context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("Geex.BlobStorage.Download")
                    .LogWarning(exception, "File download failed.");
                if (response.HasStarted || context.RequestAborted.IsCancellationRequested)
                {
                    context.Abort();
                    return;
                }
                response.Clear();
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.ContentType = "text/plain; charset=utf-8";
                await response.WriteAsync("File download failed.", context.RequestAborted);
            }
        }
    }
}
