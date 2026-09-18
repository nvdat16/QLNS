namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>Outcome of verifying a candidate password against a stored hash.</summary>
public enum PasswordVerification
{
    /// <summary>Wrong password, or a stored hash this adapter cannot read.</summary>
    Failed,

    Succeeded,

    /// <summary>Correct password stored with outdated parameters; the caller should re-hash it.</summary>
    SucceededNeedsRehash
}

/// <summary>
/// Port for password hashing. The adapter owns the algorithm and its parameters and encodes them into
/// the hash string (user_credentials.password_hash), so parameters can be raised without a migration.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Algorithm identifier stored in user_credentials.password_algorithm.</summary>
    string Algorithm { get; }

    string Hash(string password);

    PasswordVerification Verify(string encodedHash, string password);

    /// <summary>
    /// A valid hash of a random secret. Verified against when the e-mail is unknown or has no credential,
    /// so that a failing sign-in costs the same time as a succeeding one and cannot enumerate accounts.
    /// </summary>
    string DummyHash { get; }
}
