using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace DeleteStructures_sa
{
    internal static class WorkClass
    {
        public static IEnumerable<object> Items { get; set; }
        public static StructureSet StructureSet { get; set; }
        private static IEnumerable<Structure> Structures => StructureSet.Structures;

        public static void DoWork()
        {
            foreach (var item in Items)
            {
                Structure s = Structures.FirstOrDefault(str => str.Id == item.ToString());
                StructureSet.RemoveStructure(s);
            }
        }
    }
}
