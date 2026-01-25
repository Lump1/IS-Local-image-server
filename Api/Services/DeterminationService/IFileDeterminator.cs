namespace IS.ImageService.Api.Services.DeterminationService
{
    public interface IFileDeterminator
    {
        Task<(string relativePath, string sha256Hex)> SaveFileAsync(Stream input, string storageRoot, string? tempDir, string? extension, CancellationToken ct);
    }
}
