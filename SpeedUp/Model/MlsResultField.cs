using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class MlsResultField
    {
        public string HeaderName { get; set; }
        public string FieldType { get; set; }
        public string LongName { get; set; }
        public string SystemName { get; set; }
        public string Searchable { get; set; }
        public string TraceName { get; set; }
        public string InpLength { get; set; }
        public string CutBy { get; set; }
        public string DefName { get; set; }
        public string IsStdf { get; set; }
        public string DisplayMask { get; set; }
        public string BooleanFlag { get; set; }
        public string Variables { get; set; }
        public string ColumnCaption { get; set; }
        public string TrimSpaces { get; set; }
        public string CharsTransf { get; set; }
        public string DisplayName { get; set; }
        public string Prefix { get; set; }
        public string Delimeter { get; set; }
        public string CharStrip { get; set; }
        public string SubstTable { get; set; }
        public string DisplayBehaviorBitmask { get; set; }
        public string DisplayRule { get; set; }
        public string AttributeExposure { get; set; }
        public string Category { get; set; }
    }
}
