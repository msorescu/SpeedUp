using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions; 

namespace SpeedUp.Helper
{
    public class StaticRules
    {
        static List<string> RecNameForCMAPagesRec;
        static List<string> RecNameForCMAFeatureRec;
        static List<string> RecNameForNotes;

        static List<string> xMARTFields;
        static public List<string> XMARTFields
        {
            get
            {
                if (xMARTFields != null)
                    return xMARTFields;
                else
                {
                    xMARTFields = new List<string>();
                    xMARTFields.Add("SRCHCarport");
                    xMARTFields.Add("SRCHLaundryRoom");
                    xMARTFields.Add("SRCHDiningRoom");
                    xMARTFields.Add("SRCHGameRoom");
                    xMARTFields.Add("SRCHFamilyRoom");
                    xMARTFields.Add("SRCHDen");
                    xMARTFields.Add("SRCHOffice");
                    xMARTFields.Add("SRCHBasement");
                    xMARTFields.Add("SRCHCentralAir");
                    xMARTFields.Add("SRCHCentralHeat");
                    xMARTFields.Add("SRCHForcedAir");
                    xMARTFields.Add("SRCHHardwoodFloors");
                    xMARTFields.Add("SRCHFireplace");
                    xMARTFields.Add("SRCHSwimmingPool");
                    xMARTFields.Add("SRCHRVBoatParking");
                    xMARTFields.Add("SRCHSpaHotTub");
                    xMARTFields.Add("SRCHHorseFacilities");
                    xMARTFields.Add("SRCHTennisCourts");
                    xMARTFields.Add("SRCHDisabilityFeatures");
                    xMARTFields.Add("SRCHPetsAllowed");
                    xMARTFields.Add("SRCHEnergyEfficientHome");
                    xMARTFields.Add("SRCHOceanView");
                    xMARTFields.Add("SRCHAnyView");
                    xMARTFields.Add("SRCHWaterView");
                    xMARTFields.Add("SRCHCommunitySwimmingPool");
                    xMARTFields.Add("SRCHLakeView");
                    xMARTFields.Add("SRCHGolfCourseView");
                    xMARTFields.Add("SRCHCommunitySecurityFeatures");
                    xMARTFields.Add("SRCHSeniorCommunity");
                    xMARTFields.Add("SRCHGolfCourseLotorFrontage");
                    xMARTFields.Add("SRCHCuldesac");
                    xMARTFields.Add("SRCHCityView");
                    xMARTFields.Add("SRCHHillMountainView");
                    xMARTFields.Add("SRCHRiverView");
                    xMARTFields.Add("SRCHCommunitySpaHotTub");
                    xMARTFields.Add("SRCHCommunityClubhouse");
                    xMARTFields.Add("SRCHCommunityRecreationFacilities");
                    xMARTFields.Add("SRCHCommunityTennisCourts");
                    xMARTFields.Add("SRCHCornerLot");
                    xMARTFields.Add("SRCHCommunityGolf");
                    xMARTFields.Add("SRCHCommunityHorseFacilities");
                    xMARTFields.Add("SRCHCommunityBoatFacilities");
                    xMARTFields.Add("SRCHCommunityPark");
                    xMARTFields.Add("SRCHLeaseOption");
                    return xMARTFields;
                }
            }
        }

        public StaticRules()
        {
        }
        public static void InitiParam()
        {
            RecNameForCMAPagesRec = new List<string>();
            RecNameForCMAPagesRec.Add("CMAIdentifier");
            RecNameForCMAPagesRec.Add("CMAHouseNo");
            RecNameForCMAPagesRec.Add("CMAStreetName");
            RecNameForCMAPagesRec.Add("CMASuiteNo");
            RecNameForCMAPagesRec.Add("CMABedrooms");
            RecNameForCMAPagesRec.Add("CMABathrooms");
            RecNameForCMAPagesRec.Add("CMALotSize");
            RecNameForCMAPagesRec.Add("CMASquareFeet");
            RecNameForCMAPagesRec.Add("CMAStories");
            RecNameForCMAPagesRec.Add("CMAHouseStyle");
            RecNameForCMAPagesRec.Add("CMAGarage");
            RecNameForCMAPagesRec.Add("CMAArea");
            RecNameForCMAPagesRec.Add("CMAAge");
            RecNameForCMAPagesRec.Add("CMATaxAmount");
            RecNameForCMAPagesRec.Add("CMATaxYear");
            RecNameForCMAPagesRec.Add("CMAAssessment");
            RecNameForCMAPagesRec.Add("CMAListingDate");
            RecNameForCMAPagesRec.Add("CMADaysOnMarket");
            RecNameForCMAPagesRec.Add("CMAListingPrice");
            RecNameForCMAPagesRec.Add("CMASaleDate");
            RecNameForCMAPagesRec.Add("CMASalePrice");

            RecNameForCMAFeatureRec = new List<string>();
            RecNameForCMAFeatureRec.Add("CMAFeature");

            RecNameForNotes = new List<string>();
            RecNameForNotes.Add("Notes");

            xMARTFields = null;
        }
        public static string GetRecName(string name)
        {
            return "";

            //if (RecNameForCMAPagesRec == null)
            //    InitiParam();

            //if (RecNameForNotes.Contains(name))
            //    return "Notes";
            //else if (RecNameForCMAFeatureRec.Contains(name))
            //    return "CMAFeatureRec";
            //if (RecNameForNotes.Contains(name))
            //    return "Notes";
            //else if (RecNameForCMAPagesRec.Contains(name))
            //    return "CMAPagesRec";
            //else
            //    return "";     
        }
        public static Dictionary<string, string> GetTypeLen(string sDefName, string type, string len, string padding)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string sType = type;
            string sLen = len;

            if (string.IsNullOrEmpty(sType))
                sType = "I";
            if (string.IsNullOrEmpty(sLen))
                sLen = "50";
            else
            {
                try
                {
                    sLen = (Convert.ToInt16(sLen) + Convert.ToInt16(padding)).ToString();
                }
                catch
                {
                }
            }

            if (sDefName.Equals("CMAListingPrice") ||
                sDefName.Equals("CMASalePrice") ||
                sDefName.Equals("STDFSearchPrice") ||
                sDefName.Equals("CMATaxAmount") ||
                sDefName.Equals("CMAAssessment"))
            {
                sType = "L";
                sLen = "50";
            }
            else if (sDefName.Equals("CMAListingDate") ||
                sDefName.Equals("CMASaleDate") ||
                sDefName.Equals("STDFStatusDate") ||
                sDefName.Equals("STDFPendingDate") ||
                sDefName.Equals("STDFExpiredDate") ||
                sDefName.Equals("STDFInactiveDate"))
            {
                sType = "D";
                sLen = "10";
            } 
            else
            {
                string temp = type.ToLower();
                if (temp.Contains("char") || temp.Contains("string"))
                {
                    sType = "S";
                }
                else if (temp.Contains("time")) //time or datetime
                {
                    sType = "DT";
                }
                else if (temp.Equals("date"))
                {
                    sType = "D";
                }
                else if (temp.Contains("int") || temp.Equals("long") || temp.Equals("tiny") || temp.Equals("small"))
                {
                    sType = "I";
                }
                else if (temp.Equals("decimal"))
                {
                    sType = "I";
                }
                else if (temp.Equals("boolean") || temp.Equals("bool"))
                {
                    sType = "B";
                    sLen = len; //as metadata
                }
                else
                {
                    sType = "S";
                }
            }
            result.Add("type", sType);
            result.Add("len", sLen);
            return result;
        }
        public static string GetDefClassByFileName(string fileName)
        {
            fileName=fileName.ToLower();
            if (Regex.IsMatch(fileName,@"(\.def|\.sql)$"))
            {
                fileName=fileName.Substring(0,fileName.Length-4);
            }
            string[] classes=new string[]{"ag","bu","cl","cm","la","mf","mh","of","oh","re","rl"};
            for(int i=0; i<classes.Length;i++)
            {
                if (Regex.IsMatch(fileName,classes[i]+"$"))
                    return classes[i];
            }
            return "re";
        }
        public static string GetVendorByFileName(string fileName)
        {
            fileName = fileName.ToLower();
            if (Regex.IsMatch(fileName, @"\w{3}prc"))
            {
                return "prc";
            }
            string[] vendors = new string[] { "cti", "pro", "bm", "ri", "fb", "rs", "b", "m", "o", "q"};
            for (int i = 0; i < vendors.Length; i++)
            {
                if (Regex.IsMatch(fileName, "^"+vendors[i]))
                    return vendors[i];
            }
            return "";
        }
        public static List<string> Get2LetterStateAcronym()
        {
            List<string> result = new List<string>();
            result.Add("AL");
            result.Add("AK");
            result.Add("AZ");
            result.Add("AR");
            result.Add("CA");
            result.Add("CO");
            result.Add("CT");
            result.Add("DE");
            result.Add("DC");
            result.Add("FL");

            result.Add("GA");
            result.Add("GU");
            result.Add("HI");
            result.Add("ID");
            result.Add("IL");
            result.Add("IN");
            result.Add("IA");
            result.Add("KS");
            result.Add("KY");
            result.Add("LA");

            result.Add("ME");
            result.Add("MD");
            result.Add("MA");
            result.Add("MI");
            result.Add("MN");
            result.Add("MS");
            result.Add("MO");
            result.Add("MT");
            result.Add("NE");
            result.Add("NV");

            result.Add("NH");
            result.Add("NJ");
            result.Add("NM");
            result.Add("NY");
            result.Add("ND");
            result.Add("NC");
            result.Add("OH");
            result.Add("OK");
            result.Add("OR");
            result.Add("PA");
            result.Add("RI");

            result.Add("SC");
            result.Add("SD");
            result.Add("TN");
            result.Add("TX");
            result.Add("UT");
            result.Add("VT");
            result.Add("VA");
            result.Add("WA");
            result.Add("WV");
            result.Add("WI");
            result.Add("WY");

            return result;
        }
        public static string GetDEFFieldTypeByDD(string v)
        {
            if (v.Equals("Num", StringComparison.CurrentCultureIgnoreCase))
                return "I";
            if (v.Equals("Text", StringComparison.CurrentCultureIgnoreCase))
                return "S";
            if (v.Equals("Date", StringComparison.CurrentCultureIgnoreCase))
                return "D";
            if (v.Equals("Date:Time", StringComparison.CurrentCultureIgnoreCase))
                return "DT";
            if (v.Equals("Integer", StringComparison.CurrentCultureIgnoreCase))
                return "I";

            return "S";
        }
    }
}


