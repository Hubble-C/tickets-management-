using FluentValidation;
using tickets_management.Models;

namespace tickets_management.Validators;

public class LoginValidator : AbstractValidator<AspNetUsers>
{
    public LoginValidator()
    {
        RuleFor(login => login.Password)
            .NotEmpty().WithMessage("La Password es requerida");
        
        RuleFor(login => login.Username)
            .NotEmpty().WithMessage("El código de operador (Username) no puede estar vacío.")
            .MinimumLength(3).WithMessage("El nombre de usuario es demasiado corto para los estándares.");
    }
}