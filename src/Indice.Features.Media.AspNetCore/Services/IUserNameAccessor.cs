﻿namespace Indice.Features.Media.AspNetCore.Services;

/// <summary>Contains a method to resolve username in various environments.</summary>
public interface IUserNameAccessor
{
    /// <summary>Resolves the username.</summary>
    string Resolve();
}
