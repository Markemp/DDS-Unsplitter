namespace DDSUnsplitter.Library;

public interface IFileSystem
{
    IEnumerable<string> EnumerateFiles(string path, string searchPattern);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    byte[] ReadAllBytes(string path);
}

#pragma warning disable RS0030
public class RealFileSystem : IFileSystem
{
    public Stream OpenRead(string path) => File.OpenRead(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);
    public Stream OpenWrite(string path) => File.OpenWrite(path);
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => Directory.EnumerateFiles(path, searchPattern);
    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
}
#pragma warning restore RS0030
