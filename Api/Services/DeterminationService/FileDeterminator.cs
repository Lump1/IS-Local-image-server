using System.Security.Cryptography;

namespace IS.ImageService.Api.Services.DeterminationService
{
    public class FileDeterminator : IFileDeterminator
    {
        async Task<(string sha256Hex, string tempPath)> SaveToTempAndHashAsync(
            Stream input, string tempDir, CancellationToken ct)
        {
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".upload");

            await using var fs = new FileStream(
                tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 1024 * 128, options: FileOptions.Asynchronous);

            using var sha = SHA256.Create();
            var buffer = new byte[1024 * 128];

            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                sha.TransformBlock(buffer, 0, read, null, 0);
                await fs.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var hex = Convert.ToHexString(sha.Hash!).ToLowerInvariant();

            return (hex, tempPath);
        }

        string BuildRelativePath(string sha256Hex, string extension)
        {
            extension = (extension ?? "").ToLowerInvariant();
            if (extension.Length > 10) extension = "";

            var a = sha256Hex[..2];
            var b = sha256Hex.Substring(2, 2);

            return $"{a}/{b}/{sha256Hex}{extension}";

        }

        void MoveTempToFinal(string tempPath, string storageRoot, string relativePath)
        {
            var finalPath = Path.Combine(storageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.Move(tempPath, finalPath, overwrite: false);
        }

        public async Task<(string relativePath, string sha256Hex)> SaveFileAsync(
            Stream input, string storageRoot, string? tempDir, string? extension, CancellationToken ct)
        {
            tempDir ??= Path.Combine(storageRoot, "temp");

            var (sha256Hex, tempPath) = await SaveToTempAndHashAsync(input, tempDir, ct);
            var relativePath = BuildRelativePath(sha256Hex, extension);

            try
            {
                MoveTempToFinal(tempPath, storageRoot, relativePath);
            }
            catch (IOException)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }

            return (relativePath, sha256Hex);
        }
    }
}
