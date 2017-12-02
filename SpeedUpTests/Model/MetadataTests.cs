using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeedUp.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace SpeedUp.Model.Tests
{
    [TestClass()]
    public class MetadataTests
    {
        [TestMethod()]
        public void ParseMetadataTest()
        {
            var md = new Metadata(@"C:\Work\rootR9\TCDEF\TCDEF_a.1_Main\2736\metadata.xml", "A");
            md.ParseMetadata(@"C:\Work\rootR9\TCDEF\TCDEF_a.1_Main\2736\metadata.xml");
        }

        [TestMethod()]
        public void GetMlsMetadataClassListTest()
        {
            var md = new Metadata(@"C:\Work\rootR9\TCDEF\TCDEF_a.1_Main\2736\metadata.xml", "A");
            md.ParseMetadata(@"C:\Work\rootR9\TCDEF\TCDEF_a.1_Main\2736\metadata.xml");
            HashSet<string> classList = md.GetMlsMetadataClassList("Property");
        }
    }
}
