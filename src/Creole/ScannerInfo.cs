namespace Creole;

/// <summary>
/// What a custom scanner declares about itself, for analysis and error messages.
/// </summary>
/// <param name="Nullable">Whether the scanner can succeed without consuming input.</param>
/// <param name="First">
/// The characters the scanner can start with, or <see langword="null"/> if it can start with any item.
/// Give it only for a scanner over <see cref="char"/>; leave it null for a scanner over tokens.
/// </param>
/// <param name="Name">
/// What error messages call the token, such as <c>"raw string"</c>.
/// If it is null, a failed scanner expects the characters of <paramref name="First"/> instead.
/// </param>
/// <remarks>
/// Analysis trusts the declaration. A scanner with no <paramref name="First"/> is opaque, like <c>Satisfy</c>: analysis treats it as matching any item.
/// </remarks>
public sealed record ScannerInfo(bool Nullable, CharSet? First = null, string? Name = null);
