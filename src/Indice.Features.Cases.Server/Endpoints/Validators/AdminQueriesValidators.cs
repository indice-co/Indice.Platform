using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for save query request
/// </summary>
public class SaveQueryRequestValidator : AbstractValidator<SaveQueryRequest>
{
    /// <inheritdoc/>
    public SaveQueryRequestValidator() {
        RuleFor(x => x.FriendlyName).NotEmpty();
        RuleFor(x => x.Parameters).NotEmpty();
    }
}