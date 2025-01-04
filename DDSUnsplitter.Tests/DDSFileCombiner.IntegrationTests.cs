using DDSUnsplitter.Library;
using DDSUnsplitter.Library.Models;
using NUnit.Framework;

namespace DDSUnsplitter.Tests;

[TestFixture]
public class DDSFileCombinerTests
{
    private const string TEST_FILES_DIR = "TestFiles";
    private string _tempDir;

    [OneTimeSetUp]
    public void SetUp()
    {
        // Create a temporary directory for test outputs
        _tempDir = Path.Combine(Path.GetTempPath(), "DDSUnsplitterTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        // Clean up temporary files after tests
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    [TestCase("defaultnouvs.dds", 512, 512, 124)]
    public void WhenCombining_ValidDDSFile_VerifyHeaderProperties(string fileName, int expectedWidth,
    int expectedHeight, int expectedSize)
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, fileName);

        DdsHeader header = DdsHeaderDeserializer.Deserialize(File.ReadAllBytes(baseFileName));

        Assert.Multiple(() =>
        {
            Assert.That(header.Size, Is.EqualTo(expectedSize), "Header size mismatch");
            Assert.That(header.Width, Is.EqualTo(expectedWidth), "Width mismatch");
            Assert.That(header.Height, Is.EqualTo(expectedHeight), "Height mismatch");
        });
    }

    [Test]
    [TestCase("defaultnouvs.dds")]
    public void WhenCombining_WithSafeName_CreatesCombinedFile(string fileName)
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, fileName);
        string expectedExtension = ".combined.dds";

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(combinedFileName), "Combined file was not created");
            Assert.That(combinedFileName, Does.EndWith(expectedExtension),
                "Combined file does not have expected extension");
            Assert.That(new FileInfo(combinedFileName).Length, Is.GreaterThan(0),
                "Combined file is empty");
        });
    }

    [Test]
    public void Combine_SCWithCreatesCombinedFile()
    {
        string baseFileName = $@"TestFiles\defaultnouvs.dds";
        DdsHeader header = DdsHeaderDeserializer.Deserialize(File.ReadAllBytes(baseFileName));
        Assert.That(header.Size, Is.EqualTo(124));
        Assert.That(header.Width, Is.EqualTo(512));
        Assert.That(header.Height, Is.EqualTo(512));

        // Don't overwrite test files
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        Assert.That(File.Exists(combinedFileName));
        Assert.That(true);
    }

    [Test]
    public void Combine_SCWithJustFileNameFile()
    {
        string baseFileName = $@"d:\depot\sc3.24\data\textures\defaults\defaultnouvs";

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        Assert.That(File.Exists(combinedFileName));
        Assert.That(true);
    }

    [Test]
    public void Combine_MetadataEndsdWith0()
    {
        string baseFileName = $@"D:\depot\ArmoredWarfare\textures\defaults\flat_normal_ddn.dds.0";
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);
    }

    [Test]
    public void Combine_AWWithOnlyFileNameProvided()
    {
        string baseFileName = $@"D:\depot\ArmoredWarfare\textures\defaults\flat_normal_ddn";
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);
    }

    [Test]
    public void Combine_SimpleFileName()
    {
        string baseFileName = $"ddsfile";
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);
    }
}
