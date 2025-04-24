using DDSUnsplitter.Library;
using NUnit.Framework;
using static DDSUnsplitter.Library.Models.DdsConstants;

namespace DDSUnsplitter.Tests;

[TestFixture]
public class DDSFileCombinerTests
{
    private const string TEST_FILES_DIR = "TestFiles";
    private string _tempDir;
    private Dictionary<string, string> _copiedFiles = [];

    [SetUp]
    public void SetUp()
    {
        // Create a temporary directory for test outputs
        _tempDir = Path.Combine(Path.GetTempPath(), "DDSUnsplitterTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);

        // Copy test files to temp directory to avoid modifying originals
        var testFilesDir = Path.Combine(TestContext.CurrentContext.TestDirectory, TEST_FILES_DIR);
        foreach (var file in Directory.GetFiles(testFilesDir))
        {
            var destFile = Path.Combine(_tempDir, Path.GetFileName(file));
            File.Copy(file, destFile);
            _copiedFiles[destFile] = file;
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary files after each test
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch (IOException)
            {
                // If files are still in use, wait a bit and try again
                Thread.Sleep(100);
                Directory.Delete(_tempDir, true);
            }
        }
    }

    [Test]
    public void Combine_SCStyle_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(_tempDir, "defaultnouvs.dds");
        
        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, true);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(_tempDir, "defaultnouvs.dds*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".0"), "Header file should have been rewritten with safe extension");
        var headerFile = new FileInfo(Path.Combine(_tempDir, "defaultnouvs.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 296), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_SCStyleWithShortName_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(_tempDir, "defaultnouvs");

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(_tempDir, "defaultnouvs.dds*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".dds.0"), "Header file should have been rewritten with safe extension");
        var headerFile = new FileInfo(Path.Combine(_tempDir, "defaultnouvs.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 296), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_BaseFileHasDDS0Extension_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(_tempDir, "flat_normal_ddn.dds.0");
        
        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(_tempDir, "flat_normal_ddn*");
        Assert.That(allTestFiles.Contains(baseFileName), "Base file should be present");
        Assert.That(allTestFiles, Does.Contain(baseFileName), "Header file should have been rewritten with safe extension");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_BaseFileHasDDS0ExtensionAndShortName_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(_tempDir, "flat_normal_ddn");

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(_tempDir, "flat_normal_ddn*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".dds.0"), "Header file still has 0 extension");
        var headerFile = new FileInfo(Path.Combine(_tempDir, "flat_normal_ddn.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 464), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_BaseFileHasDDS0ExtensionAndShortNameWithDDS_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(_tempDir, "flat_normal_ddn.dds");

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(_tempDir, "flat_normal_ddn*");
        var headerFile = new FileInfo(Path.Combine(_tempDir, "flat_normal_ddn.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 464), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_CubeMap_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(_tempDir, "environmentprobeafternoon_cm.dds");

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(524412), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(_tempDir, "environmentprobeafternoon_cm*");
        Assert.That(allTestFiles, Does.Contain(baseFileName), "Base file should be present");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".0"), "Header file should have been rewritten with safe extension");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void FindMatchingFiles_NoExtension_GlossFilesReturnsAllFiles()
    {
        string baseFileName = "gloss10_ddna";
        var fileSet = new DDSFileCombiner(new RealFileSystem()).FindMatchingFiles(_tempDir, baseFileName);
        
        // Check main texture files
        Assert.That(fileSet.HeaderFile, Is.EqualTo(Path.Combine(_tempDir, "gloss10_ddna.dds")), "Main header file incorrect");
        Assert.That(fileSet.MipmapFiles.Count, Is.EqualTo(3), "Should have 3 main mipmaps");
        Assert.That(fileSet.MipmapFiles, Does.Contain(Path.Combine(_tempDir, "gloss10_ddna.dds.1")), "Missing main mipmap 1");
        Assert.That(fileSet.MipmapFiles, Does.Contain(Path.Combine(_tempDir, "gloss10_ddna.dds.2")), "Missing main mipmap 2");
        Assert.That(fileSet.MipmapFiles, Does.Contain(Path.Combine(_tempDir, "gloss10_ddna.dds.3")), "Missing main mipmap 3");

        // Check gloss texture files
        Assert.That(fileSet.GlossHeaderFile, Is.EqualTo(Path.Combine(_tempDir, "gloss10_ddna.dds.a")), "Gloss header file incorrect");
        Assert.That(fileSet.GlossMipmapFiles!.Count, Is.EqualTo(3), "Should have 3 gloss mipmaps");
        Assert.That(fileSet.GlossMipmapFiles, Does.Contain(Path.Combine(_tempDir, "gloss10_ddna.dds.1a")), "Missing gloss mipmap 1a");
        Assert.That(fileSet.GlossMipmapFiles, Does.Contain(Path.Combine(_tempDir, "gloss10_ddna.dds.2a")), "Missing gloss mipmap 2a");
        Assert.That(fileSet.GlossMipmapFiles, Does.Contain(Path.Combine(_tempDir, "gloss10_ddna.dds.3a")), "Missing gloss mipmap 3a");
    }

    [Test]
    public void FindMatchingFiles_WithExtension_GlossFilesReturnsAllFiles()
    {
        string baseFileName = Path.Combine(_tempDir, "gloss10_ddna.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        var fileSet = new DDSFileCombiner(new RealFileSystem()).FindMatchingFiles(directory, fileNameWithoutExtension);

        // Check main texture files
        Assert.That(fileSet.HeaderFile, Is.EqualTo(baseFileName), "Main header file incorrect");
        Assert.That(fileSet.MipmapFiles.Count, Is.EqualTo(3), "Should have 3 main mipmaps");
        Assert.That(fileSet.MipmapFiles, Does.Contain(baseFileName + ".1"), "Missing main mipmap 1");
        Assert.That(fileSet.MipmapFiles, Does.Contain(baseFileName + ".2"), "Missing main mipmap 2");
        Assert.That(fileSet.MipmapFiles, Does.Contain(baseFileName + ".3"), "Missing main mipmap 3");

        // Check gloss texture files
        Assert.That(fileSet.GlossHeaderFile, Is.EqualTo(baseFileName + ".a"), "Gloss header file incorrect");
        Assert.That(fileSet.GlossMipmapFiles!.Count, Is.EqualTo(3), "Should have 3 gloss mipmaps");
        Assert.That(fileSet.GlossMipmapFiles, Does.Contain(baseFileName + ".1a"), "Missing gloss mipmap 1a");
        Assert.That(fileSet.GlossMipmapFiles, Does.Contain(baseFileName + ".2a"), "Missing gloss mipmap 2a");
        Assert.That(fileSet.GlossMipmapFiles, Does.Contain(baseFileName + ".3a"), "Missing gloss mipmap 3a");
    }

    [Test]
    public void Combine_DdnaFile_CreatesNormalAndGlossFiles()
    {
        string baseFileName = Path.Combine(_tempDir, "gloss10_ddna.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        // Check normal file
        FileInfo normalFile = new FileInfo(combinedFileName);
        Assert.That(normalFile.Exists, "Normal combined file was not created");
        Assert.That(normalFile.Length, Is.EqualTo(21996), "Normal combined file size is incorrect");

        // Check gloss file
        string glossFileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(combinedFileName) + "_gloss" + Path.GetExtension(combinedFileName));
        FileInfo glossFile = new FileInfo(glossFileName);
        Assert.That(glossFile.Exists, "Gloss combined file was not created");
        Assert.That(glossFile.Length, Is.EqualTo(11076), "Gloss combined file size is incorrect");

        // Verify end markers on both files
        VerifyEndMarker(combinedFileName);
        VerifyEndMarker(glossFileName);
    }

    [Test]
    public void Combine_ShovelDdnaFile_CreatesNormalAndGlossFiles()
    {
        string baseFileName = Path.Combine(_tempDir, "shovel_ddna.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, false);

        // Check normal file
        FileInfo normalFile = new FileInfo(combinedFileName);
        Assert.That(normalFile.Exists, "Normal combined file was not created");
        Assert.That(normalFile.Length, Is.EqualTo(2796348), "Normal combined file size is incorrect");

        // Check gloss file
        string glossFileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(combinedFileName) + "_gloss" + Path.GetExtension(combinedFileName));
        FileInfo glossFile = new FileInfo(glossFileName);
        Assert.That(glossFile.Exists, "Gloss combined file was not created");
        Assert.That(glossFile.Length, Is.EqualTo(1398252), "Gloss combined file size is incorrect");

        // Verify end markers on both files
        VerifyEndMarker(combinedFileName);
        VerifyEndMarker(glossFileName);
    }

    [Test]
    public void DsConceptChild01_sdf_ShouldNotCombine()
    {
        string baseFileName = Path.Combine(_tempDir, "DSConceptChild01_sdf.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        string combinedFileName = new DDSFileCombiner(new RealFileSystem()).Combine(baseFileName, true);

        // Assert the returned filename is the same as input (no combining occurred)
        Assert.That(combinedFileName, Is.EqualTo(baseFileName));

        // Optional: Also verify no new files were created
        var filesInDir = Directory.GetFiles(directory, "DSConceptChild01*");
        Assert.That(filesInDir, Has.Length.EqualTo(1), "No additional files should be created");
    }

    private static void VerifyEndMarker(string combinedFileName)
    {
        // Verify end marker
        using var stream = File.OpenRead(combinedFileName);
        stream.Seek(-CRYENGINE_END_MARKER_SIZE, SeekOrigin.End);
        var endMarker = new byte[CRYENGINE_END_MARKER_SIZE];
        stream.Read(endMarker, 0, CRYENGINE_END_MARKER_SIZE);
        Assert.That(endMarker, Is.EqualTo(CRYENGINE_END_MARKER),
            "CryEngine end marker not found or incorrect");
    }
}
