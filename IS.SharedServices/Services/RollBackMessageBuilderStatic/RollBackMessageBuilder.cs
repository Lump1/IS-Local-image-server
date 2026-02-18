using Contracts;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.SharedServices.Services.RollBackMessageBuilderStatic
{
    public static class RollBackMessageBuilder
    {
        public static string RollbackMessageBuild(RBQ_Queues rbq_enum, string jobId)
        {
            return rbq_enum.ToString() + ':' + jobId;
        }
    }
}
