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
    private readonly ImageMetadataService _imageMetadataService = new();

    public async Task SaveAsync(ProjectState state, string filePath)
    {
        await using var fs = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fs, state, JsonOptions);

        string extendedPropertiesPath = BuildExtendedPropertiesPath(filePath);
        ExtendedImageProperties extendedProperties = _imageMetadataService.ExtractExtendedProperties(state.InputFilePath);
        await using var extendedFs = File.Create(extendedPropertiesPath);
        await JsonSerializer.SerializeAsync(extendedFs, extendedProperties, JsonOptions);
    }

    public async Task<ProjectState?> LoadAsync(string filePath)
    {
        await using var fs = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<ProjectState>(fs, JsonOptions);
    }

    private static string BuildExtendedPropertiesPath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        return Path.Combine(directory, $"{fileNameWithoutExtension}_extendedProperties.json");
    }
}
