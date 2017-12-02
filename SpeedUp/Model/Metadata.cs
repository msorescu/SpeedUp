using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Data;
using System.Reflection;

namespace SpeedUp.Model
{
    public class Metadata
    {
        //Overview: Metadata is non-mutable class which represents a parsed
        //standard format RETS metadata XML file

        //Attributes
        private XElement metadata;
        private string _className;
        private string _metaDataVersion;
        public ObservableCollection<MlsMetadataField> MlsMetadataFieldCollection
        { get { return _mlsMetadataFieldCollection; } }

        readonly ObservableCollection<MlsMetadataField> _mlsMetadataFieldCollection =
        new ObservableCollection<MlsMetadataField>();

        public ObservableCollection<MlsMetadataLookup> MlsMetadataLookupCollection
        { get { return _mlsMetadataLookupCollection; } }

        public string MetaDataVersion
        {
            get { return _metaDataVersion; }
            set { _metaDataVersion = value; }
        }

        public string MlsClassName
        {
            get { return _className; }
            set { _className = value; }
        }

        readonly ObservableCollection<MlsMetadataLookup> _mlsMetadataLookupCollection =
        new ObservableCollection<MlsMetadataLookup>();
        private string p;


        //Constructor
        public Metadata(string filePath, string classNameIn)
        {
            metadata = XElement.Load(filePath);
            this.MlsClassName = classNameIn;
            ParseMetadata(filePath);
        }

        public Metadata(string filePath)
        {
            metadata = XElement.Load(filePath);
            ParseMetadata(filePath);
        }

        public void ParseMetadata(string filePath)
        {
            var md = XElement.Load(filePath);
            var qVersion = md.Descendants("METADATA-SYSTEM").Select(x => new {Version = x.Attribute("Version").Value}).FirstOrDefault();
            if (qVersion != null) MetaDataVersion = qVersion.Version;
            var dataSet = from mlsField in md.Descendants("Field")
                          select new
                          {
                              MetadataRLNCol = mlsField.Element("LongName").Value,
                              MetadataClassCol = (string)mlsField.Parent.Parent.Element("ClassName"),
                              MetadataClassvisibleNameCol = (string)mlsField.Parent.Parent.Element("VisibleName"),
                              MetadataClassDescriptionCol = (string)mlsField.Parent.Parent.Element("Description"),
                              MetadataSystemCol = (string)mlsField.Parent.Attribute("System"),
                              MetadataResourceCol = (string)mlsField.Parent.Attribute("Resource"),
                              MetadataSystemNameCol = mlsField.Element("SystemName").Value,
                              MetadataMappedCol = "",
                              MetadataDatatypeCol = mlsField.Element("DataType").Value,
                              MetadataSearchableCol = mlsField.Element("Searchable").Value,
                              MetadataLookupNameCol = mlsField.Element("LookupName").Value,
                              DEFRecordsExCol = ""
                          };
            foreach (var obj in dataSet)
            {
                var field = new MlsMetadataField
                    {
                        LongName = obj.MetadataRLNCol,
                        SystemName = obj.MetadataSystemNameCol,
                        Class = obj.MetadataClassCol,
                        ClassVisibleName = obj.MetadataClassvisibleNameCol
                    };

                if (string.IsNullOrEmpty(field.ClassVisibleName))
                    field.ClassVisibleName = obj.MetadataClassDescriptionCol;
                field.System = obj.MetadataSystemCol;
                field.DataType = obj.MetadataDatatypeCol;
                field.LookupName = obj.MetadataLookupNameCol;
                field.Resource = obj.MetadataResourceCol;

                _mlsMetadataFieldCollection.Add(field);
            }

            var lookupTableValues = from lookupTable in md.Descendants("Lookup")
                                    let xElement5 = lookupTable.Element("Value")
                                    where xElement5 != null
                                    let element3 = xElement5
                                    let xElement4 = lookupTable.Element("LongValue")
                                    where xElement4 != null
                                    let xElement3 = xElement4
                                    let parent1 = lookupTable.Parent
                                    //where ((((string)LookupTable.Parent.Parent.Element("LookupName")) == lookupname) && ((((string)LookupTable.Parent.Attribute("Resource")) == "Listing") || ((string)LookupTable.Parent.Attribute("Resource") != "Property")) && ((((string)LookupTable.Element("LongValue").Value) != "") && ((string)LookupTable.Element("Value").Value != "")))
                                    //where ((((string)LookupTable.Parent.Parent.Element("LookupName")) == lookupname) && ((((string)LookupTable.Parent.Attribute("Resource")) == "Listing") || ((string)LookupTable.Parent.Attribute("Resource") != "Property")))
                                    where parent1 != null
                                    where element3 != null && (xElement3 != null && (parent1 != null && ( (((string)parent1.Attribute("Resource") == "Listing") || ((string)parent1.Attribute("Resource") == "Property")) && (((string)xElement3.Value) != "") && (((string)element3.Value) != ""))))
                                    let parent2 = parent1.Parent
                                    where parent2 != null
                                    let element4 = lookupTable.Element("ShortValue")
                                    where element4 != null
                                    select new
                                    {
                                        Resource = (string)parent1.Attribute("Resource"),
                                        LookupName = (string)parent2.Element("LookupName"),
                                        LongValueCol = xElement4.Value,
                                        ShortValueCol = element4.Value,
                                        ValueCol = xElement5.Value
                                    };

            var lookupTableValuesRappatoni = from lookupTable in metadata.Descendants("LookupType")
                                             let element2 = lookupTable.Element("Value")
                                             where element2 != null
                                             let element = element2
                                             let xElement1 = lookupTable.Element("LongValue")
                                             where xElement1 != null
                                             let xElement = xElement1
                                             let parent = lookupTable.Parent
                                             //where (((string)LookupTable.Parent.Parent.Element("LookupName")) == lookupname && ((((string)LookupTable.Parent.Attribute("Resource")) == "Listing") || (((string)LookupTable.Parent.Attribute("Resource")) == "Property")) && ((((string)LookupTable.Element("LongValue").Value) != "") && ((string)LookupTable.Element("Value").Value != "")))
                                             //where ((((string)LookupTable.Parent.Parent.Element("LookupName")) == lookupname) && ((((string)LookupTable.Parent.Attribute("Resource")) == "Listing") || ((string)LookupTable.Parent.Attribute("Resource") != "Property")))
                                             //where ((string)LookupTable.Parent.Parent.Element("LookupName") == lookupname && ((((string)LookupTable.Parent.Attribute("Resource")) == "Listing") || ((string)LookupTable.Parent.Attribute("Resource") == "Property")))
                                             where parent != null
                                             where element != null && (xElement != null && (parent != null && ((((string)parent.Attribute("Resource") == "Listing") || ((string)parent.Attribute("Resource") == "Property")) && (((string)xElement.Value) != "") && (((string)element.Value) != ""))))
                                             let element1 = parent.Parent
                                             where element1 != null
                                             let xElement2 = lookupTable.Element("ShortValue")
                                             where xElement2 != null
                                             select new
                                             {
                                                 Resource = (string)parent.Attribute("Resource"),
                                                 LookupName = (string)element1.Element("LookupName"),
                                                 LongValueCol = xElement1.Value,
                                                 ShortValueCol = xElement2.Value,
                                                 ValueCol = element2.Value
                                             };
            var any = lookupTableValues.Any();
            if (any)
            {
                foreach (var obj in lookupTableValues)
                {
                    var fd = new MlsMetadataLookup
                        {
                            Lookup = obj.LookupName,
                            LongValue = obj.LongValueCol,
                            ShortValue = obj.ShortValueCol,
                            Value = obj.ValueCol,
                            Resource = obj.Resource
                        };

                    _mlsMetadataLookupCollection.Add(fd);
                }
            }
            else
            {
                foreach (var obj in lookupTableValuesRappatoni)
                {
                    var fd = new MlsMetadataLookup
                    {
                        Lookup = obj.LookupName,
                        LongValue = obj.LongValueCol,
                        ShortValue = obj.ShortValueCol,
                        Value = obj.ValueCol,
                        Resource = obj.Resource
                    };

                    _mlsMetadataLookupCollection.Add(fd);
                } 
            }
        }

        public HashSet<string> GetMlsMetadataClassList(string resourceName)
        {
            var result = new HashSet<string>();
            if (_mlsMetadataFieldCollection != null)
            {
                var query =
                    _mlsMetadataFieldCollection.Where(x => x.Resource.ToLower() == resourceName.ToLower())
                                               .Select(x => new {x.Class})
                                               .Distinct();
                foreach(var item in query)
                    result.Add(item.Class);
            }
            return result;
        }

        public string GetMlsMetadataClassVisibleName(string propertyClassName)
        {
            string result = "";
            if (_mlsMetadataFieldCollection != null)
            {
                var query =
                    _mlsMetadataFieldCollection.Where(x => x.Class.ToLower() == propertyClassName.ToLower())
                                               .Select(x => new { x.ClassVisibleName })
                                               .Distinct().FirstOrDefault();
                if (query != null) result = query.ClassVisibleName;
            }
            return result;
        }

        public List<MlsMetadataField> GetAllFieldsByClassName(string name)
        {
            return _mlsMetadataFieldCollection.Where(x => x.Class.ToLower() == name.ToLower()).ToList();
        }

        public string GetResourceNameByClassName(string name)
        {
            var orDefault = _mlsMetadataFieldCollection.Where(x => x.Class.ToLower() == name.ToLower()).Select(x => new {x.Resource}).Distinct().FirstOrDefault();
            if (orDefault != null)
            {
                var firstOrDefault = orDefault.Resource;
                return firstOrDefault != null ? firstOrDefault.ToString(CultureInfo.InvariantCulture) : "";
            }

            return "";
        }

        public IEnumerable<dynamic> GetLookUpValues(string systemName, string className)
        {
            string lookupName = GetLookupName(systemName,className);
            var lookupTableValues = MlsMetadataLookupCollection.Where(x => x.Lookup.ToLower() == lookupName.ToLower()).Select(x => new 
                                    {
                                        LongValueCol = x.LongValue,
                                        ShortValueCol = x.ShortValue,
                                        ValueCol = x.Value,
                                    });

            return lookupTableValues;
        }

        internal string GetLookupName(string systemName,string className)
        {
            string s = "";
            foreach (var result in MlsMetadataFieldCollection.Where(
                x => x.Class.ToLower() == className.ToLower() && x.SystemName.ToLower() == systemName.ToLower())
                                      .Select(x => new { Col = x.LookupName }))
            {
                s = result.Col;
                break;
            }
            if (string.IsNullOrEmpty(s))
                s = "";
            return s;
        }

        internal string GetRetsLongName(string systemName,string className)
        {
            string s = "";
            foreach (var result in MlsMetadataFieldCollection.Where(
                x => x.Class.ToLower() == className.ToLower() && x.SystemName.ToLower() == systemName.ToLower())
                                      .Select(x => new { Col = x.LongName }))
            {
                s = result.Col;
                break;
            }
            if (string.IsNullOrEmpty(s))
                s = "";
            return s;
        }

        internal string GetDataType(string systemName, string className)
        {
            string s = "";
            foreach (var result in MlsMetadataFieldCollection.Where(
                x => x.Class.ToLower() == className.ToLower() && x.SystemName.ToLower() == systemName.ToLower())
                                      .Select(x => new {DataTypeCol = x.DataType}))
            {
                s = result.DataTypeCol;
                break;
            }
            if (string.IsNullOrEmpty(s))
                s = "";
            return s;

        }
    }
}
