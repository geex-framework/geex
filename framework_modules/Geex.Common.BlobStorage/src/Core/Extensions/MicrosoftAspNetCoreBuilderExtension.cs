using System.Net;

using Geex.Common;
using Geex.Common.BlobStorage.Api;
using Geex.Common.BlobStorage.Api.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using RestSharp.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    internal static class MicrosoftAspNetCoreBuilderExtension
    {
        public static void UseFileDownload(this IEndpointRouteBuilder endpoints)
        {
            endpoints.Map(endpoints.ServiceProvider.GetService<BlobStorageModuleOptions>().FileDownloadPath, async (context) =>
            {
                var response = context.Response;
                if (context.Request.Query.TryGetValue("fileId", out var fileId))
                {
                    var (blobObject, stream) = await context.RequestServices.GetService<IUnitOfWork>().Request(new DownloadFileRequest(fileId));
                    var mimeType = blobObject.MimeType;
                    response.ContentType = mimeType;
                    response.Headers.ContentDisposition = $"Attachment;FileName*=utf-8''{blobObject.FileName.UrlEncode()}";
                    response.Headers.Append("Cache-Control", "public,max-age=86400");//缓存1天
                    response.Headers.Append("ETag", blobObject.Md5);
                    await response.StartAsync();
                    await stream.CopyToAsync(response.Body).ConfigureAwait(false);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            });
        }
    }
}
