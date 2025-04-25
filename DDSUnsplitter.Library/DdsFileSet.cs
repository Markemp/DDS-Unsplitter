namespace DDSUnsplitter.Library;

public class DdsFileSet
{
    public string HeaderFile { get; set; } = string.Empty;
    public List<string> MipmapFiles { get; set; } = new();
    public string? GlossHeaderFile { get; set; }
    public List<string>? GlossMipmapFiles { get; set; }
    public bool IsAlreadyCombined { get; set; }
}