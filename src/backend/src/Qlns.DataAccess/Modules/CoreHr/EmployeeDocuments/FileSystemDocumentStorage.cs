using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

namespace Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// DEVELOPMENT-ONLY <see cref="IDocumentStorage"/> that keeps objects on the local file system under
/// <see cref="DocumentStorageOptions.StorageRoot"/> and issues HMAC-signed URLs served by the
/// development content endpoint. Replace with a private object-store adapter (S3/MinIO/Azure Blob
/// pre-signed URLs) before production.
/// </summary>
public sealed class FileSystemDocumentStorage(DocumentStorageOptions options) : IDocumentStorage
{
    public const string ContentPath = "/dev/document-content";

    public async Task StoreAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var path = ResolvePath(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Uri> CreateSignedDownloadUrlAsync(
        string objectKey,
        string downloadFileName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        ResolvePath(objectKey);

        var expires = expiresAt.ToUnixTimeSeconds();
        var signature = Sign(options, objectKey, downloadFileName, expires);
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{options.PublicBaseUrl.TrimEnd('/')}{ContentPath}?key={Uri.EscapeDataString(objectKey)}&name={Uri.EscapeDataString(downloadFileName)}&expires={expires}&sig={signature}");

        return Task.FromResult(new Uri(url, UriKind.Absolute));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var path = ResolvePath(objectKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies a development download link: the HMAC over <c>key|name|expires</c> must match and the
    /// link must not have expired at <paramref name="now"/>. Comparison is constant-time.
    /// </summary>
    public static bool TryValidateSignature(
        DocumentStorageOptions options,
        string? key,
        string? name,
        long expires,
        string? sig,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(sig))
        {
            return false;
        }

        if (now.ToUnixTimeSeconds() > expires)
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(Sign(options, key, name, expires));
        var provided = Encoding.ASCII.GetBytes(sig);
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    /// <summary>Maps an object key to a file below the storage root, refusing any traversal attempt.</summary>
    public static bool TryResolvePhysicalPath(DocumentStorageOptions options, string? objectKey, out string fullPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(objectKey) ||
            objectKey.Contains("..", StringComparison.Ordinal) ||
            objectKey.Contains('\\', StringComparison.Ordinal) ||
            objectKey.StartsWith('/') ||
            Path.IsPathRooted(objectKey))
        {
            return false;
        }

        var root = Path.GetFullPath(options.StorageRoot);
        var candidate = Path.GetFullPath(Path.Combine(root, objectKey));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private string ResolvePath(string objectKey) =>
        TryResolvePhysicalPath(options, objectKey, out var path)
            ? path
            : throw new ArgumentException("Object key is not a valid relative storage path.", nameof(objectKey));

    private static string Sign(DocumentStorageOptions options, string key, string name, long expires)
    {
        var payload = Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{key}|{name}|{expires}"));
        var secret = Encoding.UTF8.GetBytes(options.SigningKey);
        return Base64Url.EncodeToString(HMACSHA256.HashData(secret, payload));
    }
}
