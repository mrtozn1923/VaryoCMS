using VaryoCms.Application.Common.FieldOptions;

namespace VaryoCms.Tests.Common;

public class TextOptionsTests
{
    [Fact]
    public void Parse_null_returns_defaults()
    {
        var opts = TextOptions.Parse(null);
        Assert.Null(opts.MaxLength);
        Assert.Null(opts.Placeholder);
    }

    [Fact]
    public void Parse_empty_json_returns_defaults()
    {
        var opts = TextOptions.Parse("{}");
        Assert.Null(opts.MaxLength);
        Assert.Null(opts.Placeholder);
    }

    [Fact]
    public void Parse_invalid_json_returns_defaults()
    {
        var opts = TextOptions.Parse("not-json");
        Assert.Null(opts.MaxLength);
        Assert.Null(opts.Placeholder);
    }

    [Fact]
    public void Parse_extracts_max_length_and_placeholder()
    {
        var opts = TextOptions.Parse("""{"max_length": 250, "placeholder": "Enter title..."}""");
        Assert.Equal(250, opts.MaxLength);
        Assert.Equal("Enter title...", opts.Placeholder);
    }

    [Fact]
    public void Parse_ignores_zero_or_negative_max_length()
    {
        var opts = TextOptions.Parse("""{"max_length": 0}""");
        Assert.Null(opts.MaxLength);
    }
}

public class NumberOptionsTests
{
    [Fact]
    public void Parse_null_returns_defaults()
    {
        var opts = NumberOptions.Parse(null);
        Assert.Null(opts.Min);
        Assert.Null(opts.Max);
        Assert.Null(opts.Decimals);
    }

    [Fact]
    public void Parse_extracts_min_max_decimals()
    {
        var opts = NumberOptions.Parse("""{"min": 1, "max": 999, "decimals": 2}""");
        Assert.Equal(1m, opts.Min);
        Assert.Equal(999m, opts.Max);
        Assert.Equal(2, opts.Decimals);
    }

    [Fact]
    public void Parse_handles_zero_min()
    {
        var opts = NumberOptions.Parse("""{"min": 0}""");
        Assert.Equal(0m, opts.Min);
    }

    [Fact]
    public void Parse_invalid_json_returns_defaults()
    {
        var opts = NumberOptions.Parse("{bad}");
        Assert.Null(opts.Min);
    }
}

public class SelectOptionsTests
{
    [Fact]
    public void Parse_null_returns_empty_choices()
    {
        var opts = SelectOptions.Parse(null);
        Assert.Empty(opts.Choices);
    }

    [Fact]
    public void Parse_extracts_choices_list()
    {
        var opts = SelectOptions.Parse("""{"choices": ["Option A", "Option B", "Option C"]}""");
        Assert.Equal(3, opts.Choices.Count);
        Assert.Contains("Option A", opts.Choices);
        Assert.Contains("Option C", opts.Choices);
    }

    [Fact]
    public void Parse_invalid_json_returns_empty_choices()
    {
        var opts = SelectOptions.Parse("bad-json");
        Assert.Empty(opts.Choices);
    }
}
