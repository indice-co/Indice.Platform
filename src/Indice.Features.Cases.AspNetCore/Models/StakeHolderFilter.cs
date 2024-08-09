using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models;
/// <summary>
/// Stakeholder Filter record
/// </summary>
/// <param name="Id">StakeHolder Id</param>
/// <param name="Type">StakeHolder  Type</param>
public record StakeHolderFilter(string Id, byte Type);
