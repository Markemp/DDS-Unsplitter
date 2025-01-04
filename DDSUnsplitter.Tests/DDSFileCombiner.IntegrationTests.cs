using DDSUnsplitter.Library;
using DDSUnsplitter.Library.Models;
using NUnit.Framework;
using static DDSUnsplitter.Library.Models.DdsConstants;

namespace DDSUnsplitter.Tests;

[TestFixture]
public class DDSFileCombinerTests
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

        var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(File.ReadAllBytes(baseFileName));

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
    [TestCase(@"d:\depot\sc3.24\data\textures\defaults\defaultnouvs")]
    [TestCase(@"D:\depot\ArmoredWarfare\textures\defaults\flat_normal_ddn")]
    public void WhenCombining_WithJustPath_HandlesPathCorrectly(string baseFileName)
    {
        // Skip test if path doesn't exist
        Assume.That(Directory.Exists(Path.GetDirectoryName(baseFileName)),
            "Test directory not available - skipping test");

        string tempPath = CopyFilesToTemp(baseFileName);

        string combinedFileName = DDSFileCombiner.Combine(tempPath, true);

        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        // Verify the combined file was created in our temp directory
        Assert.That(combinedFileName, Does.StartWith(_tempDir),
            "Combined file was created outside temp directory");
    }

    [Test]
    public void WhenCombining_WithMetadataEnding_HandlesFileCorrectly()
    {
        string baseFileName = @"D:\depot\ArmoredWarfare\textures\defaults\flat_normal_ddn.dds.0";

        Assume.That(File.Exists(baseFileName), "Test file not available - skipping test");

        string tempPath = CopyFilesToTemp(baseFileName);
        string combinedFileName = DDSFileCombiner.Combine(tempPath, true);

        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(combinedFileName, Does.StartWith(_tempDir),
            "Combined file was created outside temp directory");
    }

    [Test]
    public void Combine_SCWithCreatesCombinedFile()
    {
        string baseFileName = $@"TestFiles\defaultnouvs.dds";
        var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(File.ReadAllBytes(baseFileName));

        Assert.Multiple(() =>
        {
            Assert.That(header.Size, Is.EqualTo(124), "Header size mismatch");
            Assert.That(header.Width, Is.EqualTo(512), "Width mismatch");
            Assert.That(header.Height, Is.EqualTo(512), "Height mismatch");
            Assert.That(dxt10Header, Is.Null, "DXT10 header should not be present for this test file");
        });

        // Don't overwrite test files
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        });
    }

    [Test]
    public void WhenParsing_DXT10NormalMap_HeaderIsCorrect()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");

        var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(File.ReadAllBytes(baseFileName));

        Assert.Multiple(() =>
        {
            // Verify DDS header indicates DXT10
            Assert.That(new string(header.PixelFormat.FourCC), Is.EqualTo("DX10"), "FourCC should be DX10");

            // Verify DXT10 header is present and correct
            Assert.That(dxt10Header, Is.Not.Null, "DXT10 header should be present");
            Assert.That(dxt10Header!.DxgiFormat, Is.EqualTo(DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM),
                "Should be BC5_SNORM format for normal maps");
            Assert.That(dxt10Header.ResourceDimension, Is.EqualTo(D3D10_RESOURCE_DIMENSION.D3D10_RESOURCE_DIMENSION_TEXTURE2D),
                "Should be a 2D texture");
            Assert.That(dxt10Header.ArraySize, Is.EqualTo(1), "Array size should be 1");
        });
    }

    [Test]
    public void WhenCombining_DXT10File_SkipsAVariants()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(combinedFileName), "Combined file should exist");

            // Verify the combined file has correct headers
            var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(File.ReadAllBytes(combinedFileName));
            Assert.That(new string(header.PixelFormat.FourCC), Is.EqualTo("DX10"),
                "Combined file should preserve DX10 FourCC");
            Assert.That(dxt10Header, Is.Not.Null, "Combined file should preserve DXT10 header");

            // Verify file sizes are correct (no .a variants included)
            var combinedFileSize = new FileInfo(combinedFileName).Length;
            var expectedBaseSize = new FileInfo(baseFileName).Length;
            var mipSizes = new[] { 1024, 4096, 16384 }; // Sizes of .1, .2, .3 files without 'a' variants
            var expectedTotalSize = expectedBaseSize + mipSizes.Sum() + CRYENGINE_END_MARKER_SIZE;

            Assert.That(combinedFileSize, Is.EqualTo(expectedTotalSize),
                "Combined file size should match expected size without 'a' variants");
        });
    }

    [Test]
    public void WhenListing_DXT10Files_ExcludesAVariants()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        var matchingFiles = DDSFileCombiner.FindMatchingFiles(directory, fileNameWithoutExtension);

        Assert.Multiple(() =>
        {
            // Should include base file and non-'a' mipmap files
            Assert.That(matchingFiles, Has.Count.EqualTo(4), "Should find 4 files (base + 3 mipmaps)");

            // Verify no 'a' variant files are included
            Assert.That(matchingFiles, Has.All.Not.Contains(".a"),
                "No files should have .a extension");

            // Verify expected files are included
            Assert.That(matchingFiles, Contains.Item(baseFileName), "Should include base file");
            Assert.That(matchingFiles, Contains.Item(baseFileName + ".1"), "Should include .1 mipmap");
            Assert.That(matchingFiles, Contains.Item(baseFileName + ".2"), "Should include .2 mipmap");
            Assert.That(matchingFiles, Contains.Item(baseFileName + ".3"), "Should include .3 mipmap");
        });
    }

    private string CopyFilesToTemp(string sourcePath)
    {
        // If we've already copied this source, return the cached path
        if (_copiedFiles.ContainsKey(sourcePath))
            return _copiedFiles[sourcePath];

        string sourceDir = Path.GetDirectoryName(sourcePath);
        string baseFileName = Path.GetFileName(sourcePath);

        // If no directory was provided, use current directory
        if (string.IsNullOrEmpty(sourceDir))
            sourceDir = Directory.GetCurrentDirectory();

        // Create a unique subdirectory for this test's files
        string testSubDir = Path.Combine(_tempDir, Path.GetRandomFileName());
        Directory.CreateDirectory(testSubDir);

        if (Directory.Exists(sourceDir))
        {
            // Always treat the last part as a file name pattern, regardless of extension
            string searchPattern = baseFileName + "*";
            var matchingFiles = Directory.GetFiles(sourceDir, searchPattern);

            if (!matchingFiles.Any())
                throw new FileNotFoundException($"No files found matching pattern: {searchPattern} in directory: {sourceDir}");

            foreach (string file in matchingFiles)
            {
                string destFile = Path.Combine(testSubDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
        }
        else
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        // Store the mapping of original to temp path
        // For consistency, we'll map to the base file (typically the .dds file)
        string tempPath = Path.Combine(testSubDir, baseFileName);
        _copiedFiles[sourcePath] = tempPath;

        return tempPath;
    }
}
