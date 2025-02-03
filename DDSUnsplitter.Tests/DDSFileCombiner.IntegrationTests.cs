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
    public void Combine_SCStyle_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "defaultnouvs.dds");
        
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "defaultnouvs.dds*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".0"), "Header file should have been rewritten with safe extension");
        var headerFile = new FileInfo(Path.Combine(TEST_FILES_DIR, "defaultnouvs.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 296), "Header file size is incorrect");
    }

    [Test]
    public void Combine_SCStyleWithShortName_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "defaultnouvs");

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "defaultnouvs.dds*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".dds.0"), "Header file should have been rewritten with safe extension");
        var headerFile = new FileInfo(Path.Combine(TEST_FILES_DIR, "defaultnouvs.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 296), "Header file size is incorrect");
    }

    [Test]
    public void Combine_BaseFileHasDDS0Extension_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "flat_normal_ddn.dds.0");
        
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "flat_normal_ddn*");
        Assert.That(allTestFiles.Contains(baseFileName), "Base file should be present");
        Assert.That(allTestFiles, Does.Contain(baseFileName), "Header file should have been rewritten with safe extension");
    }

    [Test]
    public void Combine_BaseFileHasDDS0ExtensionAndShortName_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "flat_normal_ddn");

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "flat_normal_ddn*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".dds.0"), "Header file still has 0 extension");
        var headerFile = new FileInfo(Path.Combine(TEST_FILES_DIR, "flat_normal_ddn.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 464), "Header file size is incorrect");
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
        });
    }

    //[Test]
    //public void WhenCombining_DXT10NormalMap_GlossFilesAddedProperly()
    //{
    //    string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");
    //    string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);
    //}

    [Test]
    public void FindMatchingFiles_GlossFilesReturnsAllFiles()
    {
        string baseFileName = "gloss10_ddna";
        var actualFiles = DDSFileCombiner.FindMatchingFiles(TEST_FILES_DIR, baseFileName);
        Assert.That(actualFiles.Count, Is.EqualTo(8));
        Assert.That(actualFiles.Last, Is.EqualTo("TestFiles\\gloss10_ddna.dds.a"));
        Assert.That(actualFiles.First, Is.EqualTo("TestFiles\\gloss10_ddna.dds"));
    }

    //[Test]
    //public void WhenCombining_DXT10File_SkipsAVariants()
    //{
    //    string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");

    //    string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

    //    Assert.Multiple(() =>
    //    {
    //        Assert.That(File.Exists(combinedFileName), "Combined file should exist");

    //        // Verify the combined file has correct headers
    //        var headerInfo = DdsHeader.Deserialize(combinedFileName);
    //        Assert.That(new string(headerInfo.Header.PixelFormat.FourCC), Is.EqualTo("DX10"),
    //            "Combined file should preserve DX10 FourCC");
    //        Assert.That(headerInfo.DXT10Header, Is.Not.Null, "Combined file should preserve DXT10 header");

    //        // Verify file sizes are correct (no .a variants included)
    //        var combinedFileSize = new FileInfo(combinedFileName).Length;
    //        var expectedBaseSize = new FileInfo(baseFileName).Length;
    //        var mipSizes = new[] { 1024, 4096, 16384 }; // Sizes of .1, .2, .3 files without 'a' variants
    //        var expectedTotalSize = expectedBaseSize + mipSizes.Sum() + CRYENGINE_END_MARKER_SIZE;

    //        Assert.That(combinedFileSize, Is.EqualTo(expectedTotalSize),
    //            "Combined file size should match expected size without 'a' variants");
    //    });
    //}

    [Test]
    public void FindMatchingFiles_SC_DDNAWithGloss_CombinesAllFiles()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        var matchingFiles = DDSFileCombiner.FindMatchingFiles(directory, fileNameWithoutExtension);

        Assert.That(matchingFiles, Has.Count.AtLeast(8), "Should find 4 files (base + 3 mipmaps)");

        Assert.That(matchingFiles, Contains.Item(baseFileName + ".a"), "At least one file should have .a extension");
        Assert.That(matchingFiles, Contains.Item(baseFileName), "Should include base file");
        Assert.That(matchingFiles, Contains.Item(baseFileName + ".1"), "Should include .1 mipmap");
        Assert.That(matchingFiles, Contains.Item(baseFileName + ".2"), "Should include .2 mipmap");
        Assert.That(matchingFiles, Contains.Item(baseFileName + ".3"), "Should include .3 mipmap");
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
