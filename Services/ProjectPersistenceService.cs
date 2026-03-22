using System.IO;
using System.Threading.Tasks;

using System.Text.Json;
using MotionPhotoWorkbench.Models;

namespace MotionPhotoWorkbench.Services;

public sealed class ProjectPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(ProjectState state, string filePath)
    {
        await using var fs = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fs, state, JsonOptions);
    }

    public async Task<ProjectState?> LoadAsync(string filePath)
    {
        await using var fs = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<ProjectState>(fs, JsonOptions);
    }
}
