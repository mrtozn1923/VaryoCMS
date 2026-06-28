using VaryoCms.Application.Common;

namespace VaryoCms.Tests.Common;

public class RelationOptionsTests
{
    [Fact]
    public void Parses_target_and_display_field()
    {
        var o = RelationOptions.Parse("{\"target_content_type_id\":5,\"display_field_slug\":\"title\"}");
        Assert.NotNull(o);
        Assert.Equal(5, o!.TargetContentTypeId);
        Assert.Equal("title", o.DisplayFieldSlug);
    }

    [Fact]
    public void Display_field_optional()
    {
        var o = RelationOptions.Parse("{\"target_content_type_id\":3}");
        Assert.NotNull(o);
        Assert.Equal(3, o!.TargetContentTypeId);
        Assert.Null(o.DisplayFieldSlug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"display_field_slug\":\"title\"}")]   // missing target
    [InlineData("{\"target_content_type_id\":0}")]       // non-positive target
    public void Invalid_returns_null(string? json)
    {
        Assert.Null(RelationOptions.Parse(json));
    }
}
