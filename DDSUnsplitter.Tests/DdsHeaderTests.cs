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

        var headerInfo = DdsHeader.Deserialize(baseFileName);

        Assert.Multiple(() =>
        {
            Assert.That(headerInfo.Header.Size, Is.EqualTo(expectedSize), "Header size mismatch");
            Assert.That(headerInfo.Header.Width, Is.EqualTo(expectedWidth), "Width mismatch");
            Assert.That(headerInfo.Header.Height, Is.EqualTo(expectedHeight), "Height mismatch");
            Assert.That(headerInfo.DXT10Header, Is.Null, "DXT10 header should not be present for this test file");
        });
    }
}
