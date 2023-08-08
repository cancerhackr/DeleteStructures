using System.Collections.Generic;
using VMS.TPS.Common.Model.Types;

namespace DeleteStructures_sa
{
    internal class ApprovalHistoryComparer : IComparer<StructureApprovalHistoryEntry>
    {
        public int Compare(StructureApprovalHistoryEntry x, StructureApprovalHistoryEntry y)
        {
            return x.ApprovalDateTime.CompareTo(y.ApprovalDateTime);
        }
    }
}
