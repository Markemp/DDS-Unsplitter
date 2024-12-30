using DDSUnsplitter.Library;
using NUnit.Framework;

namespace DDSUnsplitter.Tests;

public class DDSFileCombinerTests
{
    [Test]
    public void Combine_SCWithCreatesCombinedFile()
    {
        string baseFileName = $@"TestFiles\defaultnouvs.dds";

        string combinedFileName = DDSFileCombiner.Combine(baseFileName);

        Assert.That(File.Exists(combinedFileName));
        Assert.That(true);
    }

    [Test]
    public void Combine_SCWithJustFileNameFile()
    {
        string baseFileName = $@"d:\depot\sc3.24\data\textures\defaults\defaultnouvs";

        string combinedFileName = DDSFileCombiner.Combine(baseFileName);

        Assert.That(File.Exists(combinedFileName));
        Assert.That(true);
    }

    [Test]
    public void Combine_MetadataEndsdWith0()
    {
        string baseFileName = $@"D:\depot\ArmoredWarfare\textures\defaults\flat_normal_ddn.dds.0";
        string combinedFileName = DDSFileCombiner.Combine(baseFileName);
    }

    [Test]
    public void Combine_AWWithOnlyFileNameProvided()
    {
        string baseFileName = $@"D:\depot\ArmoredWarfare\textures\defaults\flat_normal_ddn";
        string combinedFileName = DDSFileCombiner.Combine(baseFileName);
    }
}
