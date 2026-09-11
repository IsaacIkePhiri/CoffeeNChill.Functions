using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CoffeeNChill.Functions.Functions;

public class DocumentFunctions
{
    private readonly DocumentFileService _documentFileService;

    public DocumentFunctions(DocumentFileService documentFileService)
    {
        _documentFileService = documentFileService;
    }

    [Function("UploadStaffDocument")]
    public async Task<HttpResponseData> UploadStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "documents/upload")]
        HttpRequestData req)
    {
        try
        {
            if (!req.Headers.TryGetValues(
                    "Content-Type",
                    out var contentTypeValues))
            {
                var response = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Content-Type header is required.");

                return response;
            }

            var contentType = contentTypeValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(contentType))
            {
                var response = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Content-Type header is required.");

                return response;
            }

            if (!MediaTypeHeaderValue.TryParse(
                    contentType,
                    out var mediaType))
            {
                var response = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Invalid Content-Type header.");

                return response;
            }

            if (string.IsNullOrEmpty(mediaType.MediaType.Value) ||
                !mediaType.MediaType.Value.Equals(
                    "multipart/form-data",
                    StringComparison.OrdinalIgnoreCase))
            {
                var response = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Upload must use multipart/form-data.");

                return response;
            }

            var boundarySegment =
                HeaderUtilities.RemoveQuotes(mediaType.Boundary);

            var boundary = boundarySegment.ToString();

            if (string.IsNullOrWhiteSpace(boundary))
            {
                var response = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Multipart boundary is missing.");

                return response;
            }

            var reader = new MultipartReader(
                boundary,
                req.Body);

            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (!section.Headers.TryGetValue(
                        HeaderNames.ContentDisposition,
                        out var dispositionValues))
                {
                    continue;
                }

                var dispositionText =
                    dispositionValues.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(dispositionText))
                {
                    continue;
                }

                if (!ContentDispositionHeaderValue.TryParse(
                        dispositionText,
                        out var disposition))
                {
                    continue;
                }

                if (disposition == null)
                {
                    continue;
                }

                if (!disposition.DispositionType.Equals(
                        "form-data",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fieldName =
                    disposition.Name.ToString();

                if (!string.Equals(
                        fieldName,
                        "file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName =
                    disposition.FileName.ToString();

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName =
                        disposition.FileNameStar.ToString();
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    var response = req.CreateResponse(
                        HttpStatusCode.BadRequest);

                    await response.WriteStringAsync(
                        "File name is required.");

                    return response;
                }

                fileName = Path.GetFileName(fileName);

                await _documentFileService.UploadAsync(
                    fileName,
                    section.Body);

                var successResponse = req.CreateResponse(
                    HttpStatusCode.Created);

                await successResponse.WriteStringAsync(
                    $"File '{fileName}' uploaded successfully.");

                return successResponse;
            }

            var noFileResponse = req.CreateResponse(
                HttpStatusCode.BadRequest);

            await noFileResponse.WriteStringAsync(
                "No file was uploaded. Use form-data field named 'file'.");

            return noFileResponse;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error uploading document: {ex.Message}");

            return response;
        }
    }

    [Function("ListStaffDocuments")]
    public async Task<HttpResponseData> ListStaffDocuments(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "documents")]
        HttpRequestData req)
    {
        try
        {
            var files =
                await _documentFileService.ListAsync();

            var response = req.CreateResponse(
                HttpStatusCode.OK);

            await response.WriteAsJsonAsync(files);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error retrieving documents: {ex.Message}");

            return response;
        }
    }

    [Function("DownloadStaffDocument")]
    public async Task<HttpResponseData> DownloadStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "documents/download/{fileName}")]
        HttpRequestData req,
        string fileName)
    {
        try
        {
            fileName = Path.GetFileName(fileName);

            var stream =
                await _documentFileService.DownloadAsync(fileName);

            var response = req.CreateResponse(
                HttpStatusCode.OK);

            response.Headers.Add(
                "Content-Disposition",
                $"attachment; filename=\"{fileName}\"");

            response.Headers.Add(
                "Content-Type",
                "application/octet-stream");

            await stream.CopyToAsync(response.Body);

            return response;
        }
        catch (Azure.RequestFailedException ex)
            when (ex.Status == 404)
        {
            var response = req.CreateResponse(
                HttpStatusCode.NotFound);

            await response.WriteStringAsync(
                "Document not found.");

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error downloading document: {ex.Message}");

            return response;
        }
    }
}