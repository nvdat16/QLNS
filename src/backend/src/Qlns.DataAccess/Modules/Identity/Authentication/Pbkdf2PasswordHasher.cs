using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Qlns.BusinessLogic.Modules.Identity.Authentication;

namespace Qlns.DataAccess.Modules.Identity.Authentication;

/// <summary>
/// <see cref="IPasswordHasher"/> adapter over PBKDF2-HMAC-SHA512 with a per-password 128-bit salt and
/// <see cref="Iterations"/> iterations (OWASP Password Storage Cheat Sheet, 2023 figure for SHA-512).
/// <para>
/// The encoded form <c>pbkdf2-sha512$&lt;iterations&gt;$&lt;salt&gt;$&lt;hash&gt;</c> carries its own
/// parameters, so the work factor can be raised at any time: verification keeps accepting the stored
/// iteration count and reports <see cref="PasswordVerification.SucceededNeedsRehash"/> when it is below the
/// current one. Comparison is constant-time, and a malformed stored value fails instead of throwing —
/// a corrupt row must not turn into a 500 that tells the caller the account exists.
/// </para>
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const string AlgorithmName = "pbkdf2-sha512";
    public const int Iterations = 210_000;
    public const int SaltBytes = 16;
    public const int HashBytes = 64;

    private static readonly HashAlgorithmName Prf = HashAlgorithmName.SHA512;

    /// <summary>Hash of a random secret nobody knows; see <see cref="DummyHash"/>.</summary>
    private readonly Lazy<string> _dummyHash;

    public Pbkdf2PasswordHasher() =>
        _dummyHash = new Lazy<string>(() => Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));

    public string Algorithm => AlgorithmName;

    public string DummyHash => _dummyHash.Value;

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, Iterations);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{AlgorithmName}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    public PasswordVerification Verify(string encodedHash, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (!TryParse(encodedHash, out var iterations, out var salt, out var expected))
        {
            return PasswordVerification.Failed;
        }

        var actual = Derive(password, salt, iterations);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            return PasswordVerification.Failed;
        }

        return iterations < Iterations || salt.Length < SaltBytes
            ? PasswordVerification.SucceededNeedsRehash
            : PasswordVerification.Succeeded;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, Prf, HashBytes);

    private static bool TryParse(string? encodedHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        if (string.IsNullOrEmpty(encodedHash))
        {
            return false;
        }

        var parts = encodedHash.Split('$');
        if (parts.Length != 4 ||
            !string.Equals(parts[0], AlgorithmName, StringComparison.Ordinal) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) ||
            iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length == HashBytes;
    }
}
