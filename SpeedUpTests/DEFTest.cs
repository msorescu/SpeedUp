using SpeedUp.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SpeedUpTests
{
    
    
    /// <summary>
    ///This is a test class for DEFTest and is intended
    ///to contain all DEFTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DEFTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for DEF Constructor
        ///</summary>
        [TestMethod()]
        public void DEFConstructorTest()
        {
            string filePath = string.Empty; // TODO: Initialize to an appropriate value
            DEF target = new DEF(@"C:\Work\rootR9\TCDEF\TCDEF_a.1_Main\2396\ffarazre - Copy (2).def");
            SearchFieldCollection<SearchField> sf = new SearchFieldCollection<SearchField>(target.getOrigSearchFields());
            sf.deleteField("City");
            target.SaveDefChanges(sf.exportSearchFields(),sf.exportDelSearchFields());
            //target.SaveDefFile();
        }
    }
}
