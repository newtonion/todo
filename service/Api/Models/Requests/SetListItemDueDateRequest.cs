using System;

namespace Api.Models.Requests;

public class SetListItemDueDateRequest
{
    public DateTime? DueDate { get; set; }
}
