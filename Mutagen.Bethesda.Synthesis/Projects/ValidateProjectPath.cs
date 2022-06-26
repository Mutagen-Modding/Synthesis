using System.IO.Abstractions;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.Projects;

public interface IValidateProjectPath
{
    GetResponse<string> Validate(
        string projName, 
        GetResponse<string> sln);
}

public class ValidateProjectPath : IValidateProjectPath
{
    private readonly IFileSystem _FileSystem;

    public ValidateProjectPath(IFileSystem fileSystem)
    {
        _FileSystem = fileSystem;
    }
        
    public GetResponse<string> Validate(
        string projName, 
        GetResponse<string> sln)
    {
        if (string.IsNullOrWhiteSpace(projName)) return GetResponse<string>.Fail("Project needs a name.");
        if (!StringExt.IsViableFilename(projName)) return GetResponse<string>.Fail($"Project had invalid path characters.");
        if (projName.IndexOf(' ') != -1) return GetResponse<string>.Fail($"Project name cannot contain spaces.");

        // Just mark as success until we have one and can analyze further
        if (sln.Failed) return GetResponse<string>.Succeed(string.Empty);

        try
        {
            var projPath = Path.Combine(Path.GetDirectoryName(sln.Value)!, projName, $"{projName}.csproj");
            if (_FileSystem.File.Exists(projPath))
            {
                return GetResponse<string>.Fail($"Target project folder cannot already exist as a file: {projPath}");
            }
            if (_FileSystem.Directory.Exists(projPath)
                && (_FileSystem.Directory.EnumerateFiles(projPath).Any()
                    || _FileSystem.Directory.EnumerateDirectories(projPath).Any()))
            {
                return GetResponse<string>.Fail($"Target project folder must be empty: {projPath}");
            }
            return GetResponse<string>.Succeed(projPath);
        }
        catch (ArgumentException)
        {
            return GetResponse<string>.Fail("Improper project name. Go simpler.");
        }
    }
}