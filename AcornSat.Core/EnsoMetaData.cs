using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Core
{
    public class EnsoMetaData
    {
        public EnsoIndex Index { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public short ElNinoOrientation { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; }
    }
}
