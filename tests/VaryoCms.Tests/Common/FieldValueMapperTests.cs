using VaryoCms.Application.Common;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;

namespace VaryoCms.Tests.Common;

public class FieldValueMapperTests
{
    private static ContentFieldValue Map(FieldType type, string? raw)
    {
        var v = new ContentFieldValue();
        FieldValueMapper.Apply(v, type, raw);
        return v;
    }

    [Fact]
    public void Number_writes_value_number_and_round_trips()
    {
        var v = Map(FieldType.Number, "42.5");
        Assert.Equal(42.5m, v.ValueNumber);
        Assert.Null(v.ValueText);
        Assert.Equal("42.5", FieldValueMapper.ToRaw(v, FieldType.Number));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("on", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    public void Boolean_parses_truthy_tokens(string raw, bool expected)
    {
        var v = Map(FieldType.Boolean, raw);
        Assert.Equal(expected, v.ValueBool);
    }

    [Fact]
    public void Date_writes_value_date()
    {
        var v = Map(FieldType.Date, "2024-01-15");
        Assert.Equal(new DateTime(2024, 1, 15), v.ValueDate);
    }

    [Fact]
    public void Image_writes_value_media_id_and_round_trips()
    {
        var v = Map(FieldType.Image, "12");
        Assert.Equal(12, v.ValueMediaId);
        Assert.Equal("12", FieldValueMapper.ToRaw(v, FieldType.Image));
    }

    [Fact]
    public void Text_like_writes_value_text()
    {
        var v = Map(FieldType.RichText, "<p>hi</p>");
        Assert.Equal("<p>hi</p>", v.ValueText);
        Assert.Equal("<p>hi</p>", FieldValueMapper.ToRaw(v, FieldType.RichText));
    }

    [Fact]
    public void DateRange_splits_start_and_end_and_round_trips()
    {
        var v = Map(FieldType.DateRange, "2024-06-01|2024-06-30");
        Assert.Equal(new DateTime(2024, 6, 1), v.ValueDate);
        Assert.Equal(new DateTime(2024, 6, 30), v.ValueDateEnd);
        Assert.Equal("2024-06-01|2024-06-30", FieldValueMapper.ToRaw(v, FieldType.DateRange));
    }

    [Fact]
    public void DateRange_with_only_start_leaves_end_null()
    {
        var v = Map(FieldType.DateRange, "2024-06-01|");
        Assert.Equal(new DateTime(2024, 6, 1), v.ValueDate);
        Assert.Null(v.ValueDateEnd);
    }

    [Fact]
    public void Empty_text_maps_to_null()
    {
        var v = Map(FieldType.Text, "");
        Assert.Null(v.ValueText);
    }
}
