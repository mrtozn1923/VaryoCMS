using VaryoCms.Application.Common;

namespace VaryoCms.Tests.Common;

public class MediaAllowedTypesTests
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    [InlineData("video/mp4")]
    [InlineData("video/webm")]
    [InlineData("audio/mpeg")]
    [InlineData("audio/wav")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("text/csv")]
    [InlineData("application/json")]
    [InlineData("application/zip")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void IsAllowed_returns_true_for_known_types(string mimeType)
    {
        Assert.True(MediaAllowedTypes.IsAllowed(mimeType));
    }

    [Theory]
    [InlineData("application/x-msdownload")]   // .exe
    [InlineData("application/x-sh")]           // shell script
    [InlineData("text/html")]
    [InlineData("application/javascript")]
    [InlineData("application/x-php")]
    public void IsAllowed_returns_false_for_dangerous_types(string mimeType)
    {
        Assert.False(MediaAllowedTypes.IsAllowed(mimeType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAllowed_returns_false_for_null_or_empty(string? mimeType)
    {
        Assert.False(MediaAllowedTypes.IsAllowed(mimeType));
    }

    [Fact]
    public void IsAllowed_is_case_insensitive()
    {
        Assert.True(MediaAllowedTypes.IsAllowed("IMAGE/JPEG"));
        Assert.True(MediaAllowedTypes.IsAllowed("Image/Png"));
    }

    [Fact]
    public void AcceptAttribute_is_not_empty()
    {
        Assert.NotEmpty(MediaAllowedTypes.AcceptAttribute);
    }
}
