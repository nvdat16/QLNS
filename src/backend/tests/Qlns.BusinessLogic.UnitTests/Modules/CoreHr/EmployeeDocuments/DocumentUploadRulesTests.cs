using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.EmployeeDocuments;

public sealed class DocumentUploadRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 17);

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("IMAGE/PNG")]
    [InlineData("image/png; charset=binary")]
    public void Validate_ValidInput_DoesNotThrow(string contentType)
    {
        DocumentUploadRules.Validate("degree.pdf", contentType, 1024, Today, Today);
        DocumentUploadRules.Validate("degree.pdf", contentType, DocumentUploadRules.MaxSizeBytes, null, Today);
    }

    [Theory]
    [InlineData("application/zip")]
    [InlineData("text/html")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_DisallowedContentType_FailsOnFile(string? contentType)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate("degree.pdf", contentType, 1024, null, Today));

        Assert.Contains("file", exception.Errors.Keys);
        Assert.DoesNotContain("retentionUntil", exception.Errors.Keys);
    }

    [Fact]
    public void Validate_ZeroSize_FailsOnFile()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate("degree.pdf", "application/pdf", 0, null, Today));

        Assert.Contains("The file is empty.", exception.Errors["file"]);
    }

    [Fact]
    public void Validate_Oversize_FailsOnFile()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate("degree.pdf", "application/pdf", DocumentUploadRules.MaxSizeBytes + 1, null, Today));

        Assert.Single(exception.Errors);
        Assert.Contains(exception.Errors["file"], message => message.Contains("maximum size", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_PastRetention_FailsOnRetentionUntil()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate("degree.pdf", "application/pdf", 1024, Today.AddDays(-1), Today));

        Assert.Single(exception.Errors);
        Assert.Contains("retentionUntil", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("../degree.pdf")]
    [InlineData("folder/degree.pdf")]
    [InlineData("folder\\degree.pdf")]
    public void Validate_PathSeparatorInName_FailsOnFile(string fileName)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate(fileName, "application/pdf", 1024, null, Today));

        Assert.Contains("File name must not contain path separators.", exception.Errors["file"]);
    }

    [Fact]
    public void Validate_MissingOrTooLongName_FailsOnFile()
    {
        Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate("", "application/pdf", 1024, null, Today));

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate(new string('a', 256), "application/pdf", 1024, null, Today));

        Assert.Contains(exception.Errors["file"], message => message.Contains("255", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MultipleFailures_AccumulatesAllFields()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            DocumentUploadRules.Validate("a/b", "text/plain", 0, Today.AddDays(-10), Today));

        Assert.Equal(3, exception.Errors["file"].Length);
        Assert.Single(exception.Errors["retentionUntil"]);
    }

    [Fact]
    public void IsAllowedContentType_NormalizesCaseAndParameters()
    {
        Assert.True(DocumentUploadRules.IsAllowedContentType("Application/PDF"));
        Assert.True(DocumentUploadRules.IsAllowedContentType(" image/jpeg ; q=1"));
        Assert.False(DocumentUploadRules.IsAllowedContentType("application/pdf+xml"));
        Assert.Equal("image/png", DocumentUploadRules.NormalizeContentType("IMAGE/PNG; x=y"));
        Assert.Null(DocumentUploadRules.NormalizeContentType("  "));
    }
}
