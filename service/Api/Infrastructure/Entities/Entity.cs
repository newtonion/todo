using System.ComponentModel.DataAnnotations;

namespace Api.Infrastructure.Entities;

public class Entity
{
    // Note: This identifier will be index friendly on most database types but is not univsersal
    // This will not work nicely with MSFT SQL Server. If MSFT SQL needed there are some options:
    //  *   store this as a as a binary(16)
    //  *   have the database generate and return the Id
    //  *   use one of the other COMB algorithms
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();
}
