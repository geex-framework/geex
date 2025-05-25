using System.Linq;
using System.Net;

using Geex.Common;
using Geex.Abstractions.Entities;
using Geex.Common.BlobStorage;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using RestSharp.Extensions;

// ReSharper disable once CheckNamespace
namespace Geex.Common.BlobStorage.Extensions
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
                    var blobObject = context.RequestServices.GetService<IUnitOfWork>().Query<IBlobObject>().FirstOrDefault(x=>x.Id == fileId);
                    if (blobObject == null)
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }
                    var mimeType = blobObject.MimeType;
                    response.ContentType = mimeType;
                    response.Headers.ContentDisposition = $"Attachment;FileName*=utf-8''{blobObject.FileName.UrlEncode()}";
                    response.Headers.Append("Cache-Control", "public,max-age=86400");//缓存1天
                    response.Headers.Append("ETag", blobObject.Md5);
                    var stream = await blobObject.StreamFromStorage();
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
