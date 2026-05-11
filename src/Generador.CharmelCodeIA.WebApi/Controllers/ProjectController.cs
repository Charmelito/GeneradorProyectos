using Microsoft.AspNetCore.Mvc;

namespace Generador.CharmelCodeIA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProjectController : ControllerBase
{
    [HttpPost("files")]
    public IActionResult ListFiles([FromBody] ProjectFilesRequest request)
    {
        if (!Directory.Exists(request.ProjectPath))
            return NotFound(new { error = "Project path not found" });

        var files = Directory.GetFiles(request.ProjectPath, request.Pattern ?? "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") && !f.Contains("\\.git\\"))
            .Select(f => new
            {
                relativePath = Path.GetRelativePath(request.ProjectPath, f),
                fullPath = f,
                size = new FileInfo(f).Length,
                lastModified = System.IO.File.GetLastWriteTimeUtc(f)
            })
            .ToList();

        return Ok(new { projectPath = request.ProjectPath, files });
    }

    [HttpPost("read")]
    public IActionResult ReadFile([FromBody] ReadFileRequest request)
    {
        var fullPath = Path.Combine(request.ProjectPath, request.RelativePath);
        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { error = "File not found" });

        var content = System.IO.File.ReadAllText(fullPath);
        return Ok(new
        {
            relativePath = request.RelativePath,
            content,
            size = new FileInfo(fullPath).Length,
            lastModified = System.IO.File.GetLastWriteTimeUtc(fullPath)
        });
    }

    [HttpPost("write")]
    public async Task<IActionResult> WriteFile([FromBody] WriteFileRequest request)
    {
        var fullPath = Path.Combine(request.ProjectPath, request.RelativePath);
        var dir = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await System.IO.File.WriteAllTextAsync(fullPath, request.Content);
        return Ok(new { relativePath = request.RelativePath });
    }

    [HttpPost("download")]
    public IActionResult DownloadZip([FromBody] DownloadRequest request)
    {
        if (!Directory.Exists(request.ProjectPath))
            return NotFound(new { error = "Project path not found" });

        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(request.ProjectPath, zipPath);
            var bytes = System.IO.File.ReadAllBytes(zipPath);
            var projectName = Path.GetFileName(request.ProjectPath.TrimEnd('\\', '/'));
            return File(bytes, "application/zip", $"{projectName}.zip");
        }
        finally
        {
            if (System.IO.File.Exists(zipPath))
                System.IO.File.Delete(zipPath);
        }
    }
}

public sealed class ProjectFilesRequest
{
    public string ProjectPath { get; set; } = string.Empty;
    public string? Pattern { get; set; }
}

public sealed class ReadFileRequest
{
    public string ProjectPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
}

public sealed class WriteFileRequest
{
    public string ProjectPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class DownloadRequest
{
    public string ProjectPath { get; set; } = string.Empty;
}
