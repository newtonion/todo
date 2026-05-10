using System;

namespace Api.Validators;

public interface IEntityValidator<T>    
    where T: class
{
    public Task ValidateAsync(T entity);
}
