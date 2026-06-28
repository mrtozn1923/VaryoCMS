namespace VaryoCms.Application.DTOs.ContentField;

// Ordered list of field ids; index becomes the new sort_order.
public class ReorderFieldsRequest
{
    public List<int> FieldIds { get; set; } = new();
}
