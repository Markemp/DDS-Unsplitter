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
    [TestCase("defaultnouvs", new[] { 296, 512, 2048, 8192, 32768, 131072 }, false, 8, "", false)]
    [TestCase("flat_normal_ddn", new[] { 464, 1024, 4096, 16384 }, false, 6, "ATI2", true)]
    public void WhenCombining_WithJustPath_HandlesPathCorrectly(
    string fileName,
    int[] expectedSizes,
    bool hasDxt10Header,
    int expectedMipmapCount,
    string expectedFourCC,
    bool hasNumberedHeader)
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, fileName);

        // Skip test if files don't exist
        string headerPattern = hasNumberedHeader ? ".dds.0" : ".dds";
        Assume.That(File.Exists(baseFileName + headerPattern),
            "Test files not available - skipping test");

        string tempPath = CopyFilesToTemp(baseFileName);

        string combinedFileName = DDSFileCombiner.Combine(tempPath, true);

        Assert.Multiple(() =>
        {
            // Verify file existence and location
            Assert.That(File.Exists(combinedFileName), "Combined file was not created");
            Assert.That(combinedFileName, Does.StartWith(_tempDir),
                "Combined file was created outside temp directory");

            // Verify headers
            var headerInfo = DdsHeader.Deserialize(baseFileName + headerPattern);

            // Check header format
            if (hasDxt10Header)
            {
                Assert.That(new string(headerInfo.Header.PixelFormat.FourCC), Is.EqualTo("DX10"),
                    "File should have DX10 FourCC");
                Assert.That(headerInfo.DXT10Header, Is.Not.Null, "DXT10 header should be present");
            }
            else if (!string.IsNullOrEmpty(expectedFourCC))
            {
                Assert.That(new string(headerInfo.Header.PixelFormat.FourCC), Is.EqualTo(expectedFourCC),
                    "File should have expected FourCC");
                Assert.That(headerInfo.DXT10Header, Is.Null, "DXT10 header should not be present");
            }

            // Verify file size
            var combinedFileSize = new FileInfo(combinedFileName).Length;
            var expectedTotalSize = expectedSizes.Sum() + CRYENGINE_END_MARKER_SIZE;

            Assert.That(combinedFileSize, Is.EqualTo(expectedTotalSize),
                "Combined file size doesn't match expected size");

            // Verify end marker
            using var stream = File.OpenRead(combinedFileName);
            stream.Seek(-CRYENGINE_END_MARKER_SIZE, SeekOrigin.End);
            var endMarker = new byte[CRYENGINE_END_MARKER_SIZE];
            stream.Read(endMarker, 0, CRYENGINE_END_MARKER_SIZE);
            Assert.That(endMarker, Is.EqualTo(CRYENGINE_END_MARKER),
                "CryEngine end marker not found or incorrect");

            // Verify mipmap count in header matches expected
            Assert.That(headerInfo.Header.MipMapCount, Is.EqualTo(expectedMipmapCount),
                "MipMap count in header doesn't match expected count");

            // Verify number of data files matches expected sizes array length minus header
            var searchPattern = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(tempPath)) + ".dds.*";
            var dataFiles = Directory.GetFiles(Path.GetDirectoryName(tempPath)!, searchPattern)
                .Where(f => !f.EndsWith("a", StringComparison.OrdinalIgnoreCase) &&
                           Path.GetExtension(f).Length > 1 &&
                           Path.GetExtension(f).TrimStart('.').All(char.IsDigit))
                .ToList();

            var expectedFileCount = hasNumberedHeader ? expectedSizes.Length : expectedSizes.Length - 1;
            Assert.That(dataFiles.Count, Is.EqualTo(expectedFileCount),
                $"Number of data files doesn't match expected count. Search pattern was: {searchPattern}");
        });
    }

    [Test]
    public void Combine_SCWithCreatesCombinedFile()
    {
        string baseFileName = Path.Combine("TestFiles", "defaultnouvs.dds");

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        FileInfo fileInfo = new(combinedFileName);
        Assert.That(fileInfo.Length, Is.GreaterThan(0), "Combined file is empty");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles("TestFiles", "defaultnouvs.dds*");
        Assert.That(allTestFiles.Contains(baseFileName), "Base file should be present");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".0"), "Header file should have been rewritten with safe extension");
    }

    [Test]
    public void WhenParsing_DXT10NormalMap_HeaderIsCorrect()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");

        var headerInfo = DdsHeader.Deserialize(baseFileName);

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

    [Test]
    public void WhenCombining_DXT10NormalMap_GlossFilesAddedProperly()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

    }

    [Test]
    public void FindMatchingFiles_GlossFilesReturnsAllFiles()
    {
        string baseFileName = "gloss10_ddna";
        var actualFiles = DDSFileCombiner.FindMatchingFiles(TEST_FILES_DIR, baseFileName);
        Assert.That(actualFiles.Count, Is.EqualTo(8));
        Assert.That(actualFiles[7], Is.EqualTo("TestFiles\\gloss10_ddna.dds.a"));
        Assert.That(actualFiles[0], Is.EqualTo("TestFiles\\gloss10_ddna.dds"));
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
            var headerInfo = DdsHeader.Deserialize(combinedFileName);
            Assert.That(new string(headerInfo.Header.PixelFormat.FourCC), Is.EqualTo("DX10"),
                "Combined file should preserve DX10 FourCC");
            Assert.That(headerInfo.DXT10Header, Is.Not.Null, "Combined file should preserve DXT10 header");

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
