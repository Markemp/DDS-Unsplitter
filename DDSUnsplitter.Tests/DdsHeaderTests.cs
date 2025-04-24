using DDSUnsplitter.Library.Models;
using NUnit.Framework;

namespace DDSUnsplitter.Tests;

[TestFixture]
public class DdsHeaderTests
{
    private const string TEST_FILES_DIR = "TestFiles";
    private string _tempDir;
    private Dictionary<string, string> _copiedFiles = [];

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
            Directory.Delete(_tempDir, true);
    }

    [Test]
    [TestCase("defaultnouvs.dds", 512, 512, 124)]
    public void WhenCombining_ValidDDSFile_VerifyHeaderProperties(string fileName, int expectedWidth,
    int expectedHeight, int expectedSize)
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, fileName);

        var headerInfo = DdsHeader.Deserialize(File.ReadAllBytes(baseFileName));

        Assert.Multiple(() =>
        {
            Assert.That(headerInfo.Header.Size, Is.EqualTo(expectedSize), "Header size mismatch");
            Assert.That(headerInfo.Header.Width, Is.EqualTo(expectedWidth), "Width mismatch");
            Assert.That(headerInfo.Header.Height, Is.EqualTo(expectedHeight), "Height mismatch");
            Assert.That(headerInfo.DXT10Header, Is.Null, "DXT10 header should not be present for this test file");
        });
    }

    [Test]
    public void WhenParsing_DXT10NormalMap_HeaderIsCorrect()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");

        var headerInfo = DdsHeader.Deserialize(File.ReadAllBytes(baseFileName));

        Assert.Multiple(() =>
        {
            // Verify DDS header indicates DXT10
            Assert.That(new string(headerInfo.Header.PixelFormat.FourCC), Is.EqualTo("DX10"), "FourCC should be DX10");

            // Verify DXT10 header is present and correct
            Assert.That(headerInfo.DXT10Header, Is.Not.Null, "DXT10 header should be present");
            Assert.That(headerInfo.DXT10Header!.DxgiFormat, Is.EqualTo(DxgiFormat.BC5_SNORM),
                "Should be BC5_SNORM format for normal maps");
            Assert.That(headerInfo.DXT10Header.ResourceDimension, Is.EqualTo(D3D10ResourceDimension.TEXTURE2D),
                "Should be a 2D texture");
            Assert.That(headerInfo.DXT10Header.ArraySize, Is.EqualTo(1), "Array size should be 1");
        });
    }
}
