using System;

namespace Jellyfin.Plugin.AnimeThemes.Exceptions;

/// <summary>
/// Exception that is thrown when the converison using FFMPEG fails.
/// </summary>
public class ConversionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class.
    /// </summary>
    /// <param name="exitCode">Exit code of the process.</param>
    /// <param name="error">Stderr output.</param>
    public ConversionException(int exitCode, string error) : base($"Conversion failed with exit code {exitCode}. Error: {error}")
    {
    }
}
