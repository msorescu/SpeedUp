using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    class ModuleEntity
    {
        public short ModuleId { get; set; }
        public short BoardId { get; set; }
        public string ModuleName { get; set; }
        public string DatasourceId { get; set; }
    }
}
