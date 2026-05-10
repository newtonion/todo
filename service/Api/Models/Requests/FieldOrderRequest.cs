using System;

namespace Api.Models.Requests;

public class FieldOrderRequest
{
    public string Field { get; set; } = default!;
    public bool Ascending { get; set; } = true;

}
