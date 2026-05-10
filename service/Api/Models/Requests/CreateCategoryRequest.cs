using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class CreateCategoryRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 100 characters")]
    public required string Name { get; set; }
}
