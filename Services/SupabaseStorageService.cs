using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using client.Models;

namespace client.Services;
public class SupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    public SupabaseStorageService( IHttpClientFactory httpClientFactory, IOptions<SupabaseSettings> options)
    {
        _httpClient = httpClientFactory.CreateClient("Supabase");
        _settings = options.Value;
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0) throw new Exception("No file selected.");
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
       
        var response = await _httpClient.PostAsync( $"object/{_settings.Bucket}/{fileName}", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
        return $"{_settings.Url}/storage/v1/object/public/{_settings.Bucket}/{fileName}";
    }

    public async Task DeleteImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;
        var fileName = Path.GetFileName(imageUrl);
        var response = await _httpClient.DeleteAsync( $"object/{_settings.Bucket}/{fileName}");
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
        }
    }
}