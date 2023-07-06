using FluentValidation;
using Indice.Features.Identity.Server.Manager.Models;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Server.Manager.Validation;

/// <summary></summary>
public class UpdateUserPhoneNumberRequestValidator : AbstractValidator<UpdateUserPhoneNumberRequest>
{
    private readonly IConfiguration _configuration;

    /// <summary></summary>
    public UpdateUserPhoneNumberRequestValidator(IConfiguration configuration) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RuleFor(x => x.PhoneNumber).UserPhoneNumber(_configuration).NotEmpty();
    }
}
