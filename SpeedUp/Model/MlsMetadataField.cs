using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class MlsMetadataField
    {
        public string System { get; set; }
        public string Resource { get; set; }
        public string Class { get; set; }
        public string ClassVisibleName { get; set; }
        public string LongName { get; set; }
        public string SystemName { get; set; }
        public string DataType { get; set; }
        public string Searchable { get; set; }
        public string LookupName { get; set; }
        public string StandardName { get; set; }
        public string MetadataEntryId { get; set; }
        public string DbName { get; set; }
        public string ShortName { get; set; }
        public string MaximumLength { get; set; }
    }
}
