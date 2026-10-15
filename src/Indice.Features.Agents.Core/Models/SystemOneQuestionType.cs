namespace Indice.Features.Agents.Core.Models;

/// <summary>The kind of decision a <see cref="SystemOneQuestion"/> asks the system-one model for.</summary>
public enum SystemOneQuestionType
{
    /// <summary>A yes/no condition; the answer is the probability (0..1) that it holds.</summary>
    Noul = 0,

    /// <summary>One option out of a fixed set; the answer is the chosen option and the probability of each option.</summary>
    Choice = 1
}
