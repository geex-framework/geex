using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using RestSharp.Extensions;


// ReSharper disable once CheckNamespace
namespace Geex.Extensions.BlobStorage.Extensions
{
    internal static class MicrosoftAspNetCoreBuilderExtension
    {
        public static void UseFileDownload(this IEndpointRouteBuilder endpoints)
        {
            endpoints.Map(endpoints.ServiceProvider.GetService<BlobStorageModuleOptions>().FileDownloadPath, async (context) =>
            {
                var response = context.Response;
                try
                {
                    if (context.Request.Query.TryGetValue("fileId", out var fileId))
                    {
                        var blobObject = context.RequestServices.GetService<IUnitOfWork>().Query<IBlobObject>().FirstOrDefault(x => x.Id == fileId);
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
                        await response.StartAsync();
                        await using var stream = await blobObject.StreamFromStorage();
                        await stream.CopyToAsync(response.Body);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                finally
                {
                    if (!response.HasStarted)
                    {
                        await response.StartAsync();
                    }
                    await response.CompleteAsync();
                }
            });
        }
    }
}
