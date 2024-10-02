using System.Net;
using Geex.Common;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api;
using Geex.Common.BlobStorage.Api.Abstractions;
using MediatR;
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
                    response.ContentType = blobObject.MimeType;
                    response.Headers.ContentDisposition = $"Attachment;FileName*=utf-8''{blobObject.FileName.UrlEncode()}";
                    await stream.CopyToAsync(response.Body);
                    await response.CompleteAsync();
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            });
        }
    }
}
