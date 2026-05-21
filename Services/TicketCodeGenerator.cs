using System.Security.Cryptography;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class TicketCodeGenerator : ITicketCodeGenerator
{
    // Crockford Base32: digits + uppercase letters minus I, L, O and U so the
    // printed code is unambiguous to a human reading it back from a ticket.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    // 10 symbols over a 32-char alphabet => ~50 bits of entropy per code.
    private const int PayloadLength = 10;

    public string Generate()
    {
        Span<byte> bytes = stackalloc byte[PayloadLength];
        RandomNumberGenerator.Fill(bytes);

        Span<char> chars = stackalloc char[PayloadLength];
        for (var i = 0; i < PayloadLength; i++)
        {
            // 256 is a multiple of 32, so the modulo introduces no bias.
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        // Grouped (TKT-XXXXX-XXXXX) for readability on tickets and QR payloads.
        return $"TKT-{new string(chars[..5])}-{new string(chars[5..])}";
    }
}
