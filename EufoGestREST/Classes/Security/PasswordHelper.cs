using System.Security.Cryptography;

namespace EufoGestREST.Classes.Security;

public static class PasswordHelper
{
    // 16 byte convertiti in Base64 diventano 24 caratteri
    private const int SaltSize = 16;
    // 32 byte convertiti in Base64 diventano 44 caratteri
    private const int KeySize = 32;
    // 100.000 iterazioni sono lo standard OWASP attuale per bilanciare sicurezza e performance
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;

    public static (string Hash, string Salt) GeneraHash(string passwordInChiaro)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordInChiaro, saltBytes, Iterations, _hashAlgorithmName, KeySize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool VerificaPassword(string passwordInserita, string hashSalvato, string saltSalvato)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(saltSalvato);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordInserita, saltBytes, Iterations, _hashAlgorithmName, KeySize);
            var hashCalcolato = Convert.ToBase64String(hashBytes);

            // FixedTimeEquals per prevenire gli attacchi di tipo timing
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hashCalcolato),
                Convert.FromBase64String(hashSalvato)
            );
        }
        catch
        {
            // Se le stringhe Base64 sono malformate, la verifica fallisce
            return false;
        }
    }
}
