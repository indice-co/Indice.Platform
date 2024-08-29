using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models;
/// <summary>
/// CaseMeber Filter record
/// </summary>
/// <param name="Id">CaseMeber Id</param>
/// <param name="Type">CaseMeber Type</param>
public record CaseMemberFilter(string Id, byte Type);
