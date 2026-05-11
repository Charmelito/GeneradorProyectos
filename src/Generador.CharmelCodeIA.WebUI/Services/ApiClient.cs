using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Generador.CharmelCodeIA.WebUI.Models;

namespace Generador.CharmelCodeIA.WebUI.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ConnectionTestResult?> TestConnectionAsync(
        string connectionString, string provider)
    {
        var response = await _http.PostAsJsonAsync("api/database/test-connection",
            new { connectionString, provider }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConnectionTestResult>(JsonOptions);
    }

    public async Task<SchemaResult?> ReadSchemaAsync(
        string connectionString, string provider)
    {
        var response = await _http.PostAsJsonAsync("api/database/read-schema",
            new { connectionString, provider }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SchemaResult>(JsonOptions);
    }

    public async Task<GenerationResult?> GenerateFullAsync(FullGenRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/generation/full", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GenerationResult>(JsonOptions);
    }

    public async Task<IncrementalResult?> GenerateIncrementalAsync(IncGenRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/generation/incremental", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IncrementalResult>(JsonOptions);
    }

    public async Task<List<PromptDto>?> GetPromptsAsync(string? category = null)
    {
        var url = "api/prompts" + (category != null ? $"?category={category}" : "");
        return await _http.GetFromJsonAsync<List<PromptDto>>(url, JsonOptions);
    }

    public async Task<bool> SavePromptAsync(PromptDto prompt)
    {
        var response = await _http.PostAsJsonAsync("api/prompts", prompt, JsonOptions);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePromptAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/prompts/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<DiffResult?> AnalyzeDifferentialAsync(string projectPath, Dictionary<string, string> files)
    {
        var response = await _http.PostAsJsonAsync("api/differential/analyze",
            new { projectPath, proposedFiles = files }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiffResult>(JsonOptions);
    }

    public async Task<ConfirmResult?> ConfirmGenerationAsync(ConfirmRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/differential/confirm", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConfirmResult>(JsonOptions);
    }

    public async Task<ProjectFilesResult?> ListProjectFilesAsync(string projectPath)
    {
        var response = await _http.PostAsJsonAsync("api/project/files",
            new { projectPath }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectFilesResult>(JsonOptions);
    }

    public async Task<FileContentResult?> ReadFileAsync(string projectPath, string relativePath)
    {
        var response = await _http.PostAsJsonAsync("api/project/read",
            new { projectPath, relativePath }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FileContentResult>(JsonOptions);
    }

    public async Task<byte[]?> DownloadZipAsync(string projectPath)
    {
        var response = await _http.PostAsJsonAsync("api/project/download",
            new { projectPath }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
