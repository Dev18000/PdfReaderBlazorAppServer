using PdfReaderBlazorAppServer.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PdfReaderBlazorAppServer.Services;

public class PdfProcessingService
{
    private readonly HttpClient _httpClient;

    public PdfProcessingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ParsedDocumentResult?> UploadFileAsync(Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("api/FileUpload/upload", content);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ParsedDocumentResult>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return null;
    }
}

public class ParsedDocumentResult
{
    public string Text { get; set; }
    public ParsedDocumentData Data { get; set; }
}
