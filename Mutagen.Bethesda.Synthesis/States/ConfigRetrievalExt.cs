using System.Diagnostics.CodeAnalysis;

namespace Mutagen.Bethesda.Synthesis;

public static class ConfigRetrievalExt
{
    /// <summary>
    /// Attempts to locate and confirm the existence of a config file from either the user data folder
    /// or the default data folder as it exists in the source repository.
    /// </summary>
    /// <param name="state">Patcher state to refer to</param>
    /// <param name="relativePath">Path to the config file, relative to the data folder.</param>
    /// <param name="resolvedPath">Located config file that exists</param>
    /// <returns>True if config file was located that exists</returns>
    public static bool TryRetrieveConfigFile(this IPatcherState state, string relativePath, [MaybeNullWhen(false)] out string resolvedPath)
    {
        if (state.ExtraSettingsDataPath != null)
        {
            var userPath = Path.Combine(state.ExtraSettingsDataPath, relativePath);
            if (File.Exists(userPath))
            {
                resolvedPath = userPath;
                return true;
            }
        }

        if (state.DefaultSettingsDataPath != null)
        {
            var defPath = Path.Combine(state.DefaultSettingsDataPath, relativePath);
            if (File.Exists(defPath))
            {
                resolvedPath = defPath;
                return true;
            }
        }

        resolvedPath = null;
        return false;
    }

    /// <summary>
    /// Locate and confirm the existence of a config file from either the user data folder
    /// or the default data folder as it exists in the source repository.
    /// </summary>
    /// <param name="state">Patcher state to refer to</param>
    /// <param name="relativePath">Path to the config file, relative to the data folder.</param>
    /// <exception cref="FileNotFoundException">If a config file could not be located that exists in either location.</exception>
    /// <returns>Located config file that exists</returns>
    public static string RetrieveConfigFile(this IPatcherState state, string relativePath)
    {
        if (TryRetrieveConfigFile(state, relativePath, out var resolved))
        {
            return resolved;
        }

        throw new FileNotFoundException($"Could not locate config file: {relativePath}");
    }
        
    /// <summary>
    /// Attempts to locate and confirm the existence of an internal file.
    /// </summary>
    /// <param name="state">Patcher state to refer to</param>
    /// <param name="relativePath">Path to the internal file, relative to the internal data folder.</param>
    /// <param name="resolvedPath">Located file that exists</param>
    /// <returns>True if file was located that exists</returns>
    public static bool TryRetrieveInternalFile(this IPatcherState state, string relativePath, [MaybeNullWhen(false)] out string resolvedPath)
    {
        if (state.InternalDataPath == null)
        {
            resolvedPath = null;
            return false;
        }
            
        var userPath = Path.Combine(state.InternalDataPath, relativePath);
        if (File.Exists(userPath))
        {
            resolvedPath = userPath;
            return true;
        }

        resolvedPath = null;
        return false;
    }

    /// <summary>
    /// Attempts to locate and confirm the existence of an internal file.
    /// </summary>
    /// <param name="state">Patcher state to refer to</param>
    /// <param name="relativePath">Path to the internal file, relative to the internal data folder.</param>
    /// <exception cref="FileNotFoundException">If a file could not be located that exists in either location.</exception>
    /// <returns>Located file that exists</returns>
    public static string RetrieveInternalFile(this IPatcherState state, string relativePath)
    {
        if (TryRetrieveInternalFile(state, relativePath, out var resolved))
        {
            return resolved;
        }

        throw new FileNotFoundException($"Could not locate internal file: {relativePath}");
    }
}