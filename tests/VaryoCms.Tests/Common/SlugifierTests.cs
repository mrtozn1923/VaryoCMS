using VaryoCms.Application.Common;

namespace VaryoCms.Tests.Common;

public class SlugifierTests
{
    [Theory]
    [InlineData("Hello World",        "hello-world")]
    [InlineData("Merhaba Dünya",      "merhaba-dunya")]
    [InlineData("Şeker Fabrikası",    "seker-fabrikasi")]
    [InlineData("İstanbul Sokakları", "istanbul-sokaklari")]
    [InlineData("BÜYÜK HARFLER",      "buyuk-harfler")]
    [InlineData("Ğüşı Öç",           "gusi-oc")]
    public void ToSlug_maps_turkish_characters_and_lowercases(string input, string expected)
    {
        Assert.Equal(expected, Slugifier.ToSlug(input));
    }

    [Fact]
    public void ToSlug_collapses_multiple_spaces_to_single_dash()
    {
        Assert.Equal("a-b-c", Slugifier.ToSlug("a   b   c"));
    }

    [Fact]
    public void ToSlug_collapses_multiple_dashes()
    {
        Assert.Equal("a-b", Slugifier.ToSlug("a---b"));
    }

    [Fact]
    public void ToSlug_strips_special_chars()
    {
        Assert.Equal("hello-world", Slugifier.ToSlug("hello, world!"));
    }

    [Fact]
    public void ToSlug_trims_leading_trailing_whitespace()
    {
        Assert.Equal("hello", Slugifier.ToSlug("  hello  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToSlug_returns_empty_for_null_or_whitespace(string? input)
    {
        Assert.Equal(string.Empty, Slugifier.ToSlug(input!));
    }

    [Fact]
    public void ToSlug_handles_numbers()
    {
        Assert.Equal("blog-post-42", Slugifier.ToSlug("Blog Post 42"));
    }

    [Fact]
    public void ToSlug_very_long_input_does_not_throw()
    {
        string longInput = new string('a', 2000) + " test";
        string result = Slugifier.ToSlug(longInput);
        Assert.NotEmpty(result);
        Assert.DoesNotContain(" ", result);
    }

    // ToCamelCase — converts kebab-case field slugs to camelCase API keys
    [Theory]
    [InlineData("title",                "title")]
    [InlineData("hero-title",           "heroTitle")]
    [InlineData("seo-meta-description", "seoMetaDescription")]
    [InlineData("read-time",            "readTime")]
    [InlineData("cover-image",          "coverImage")]
    [InlineData("a",                    "a")]
    [InlineData("a-b-c-d",             "aBCD")]
    public void ToCamelCase_converts_kebab_to_camelCase(string slug, string expected)
    {
        Assert.Equal(expected, Slugifier.ToCamelCase(slug));
    }

    [Fact]
    public void ToCamelCase_single_word_is_unchanged()
    {
        Assert.Equal("category", Slugifier.ToCamelCase("category"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToCamelCase_returns_input_unchanged_for_null_or_whitespace(string? input)
    {
        Assert.Equal(input ?? string.Empty, Slugifier.ToCamelCase(input!));
    }

    [Fact]
    public void ToCamelCase_alias_also_normalized()
    {
        // Aliases set in admin (e.g. "body-text") must also be camelCase in the response
        Assert.Equal("bodyText", Slugifier.ToCamelCase("body-text"));
    }
}
