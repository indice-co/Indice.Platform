using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Messages.Core.Data.Models;

namespace Indice.Features.Messages.Core.Models;

public class DbIntermediateObject
{
    public DbCampaign? Campaign { get; set; }   
    public DbMessage? Message { get; set; }
}
