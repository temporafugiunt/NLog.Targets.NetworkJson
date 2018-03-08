using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDNetworkJSONService.Exceptions
{
    public class DeadLetterException : Exception
    {
        public int ArchiveReasonId { get; }
        public DeadLetterException(int archiveReasonId)
        {
            ArchiveReasonId = archiveReasonId;
        }
    }
}
