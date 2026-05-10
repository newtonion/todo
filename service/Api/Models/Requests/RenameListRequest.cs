using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class RenameListRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "List name must be between 1 and 200 characters")]
    public required string Name { get; set; }
}
