using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SpeedUp.Model
{
    public class TigerLeadFileParser
    {
        public TigerLeadReportBoardCollection ListingClassesParsed()
        {
            var returnvalue = new TigerLeadReportBoardCollection();
            var reader = new StreamReader(File.OpenRead(string.Format(@"{0}\Projects\TigerLead\FinalReport\mls_listing_types(old version).csv",ConfigurationManager.AppSettings["Drive"])));
            int rowcount = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (rowcount > 0)
                {
                    
                    var values = line.Split(',');

                    returnvalue.AddClass(values[0], new TigerLeadReportListingTypeEntry
                                                        {
                                                            TigerLeadName = values[4],
                                                            ListingTable = values[2],
                                                            ListingType = values[1],
                                                            Resource = values[3]
                                                        });

                    
                }
                rowcount++;
            }

            return returnvalue;
        }

        public List<TigerLeadReportRow> ReportParsed()
        {
            var returnvalue = new List<TigerLeadReportRow>();

            var reader = new StreamReader(File.OpenRead(string.Format(@"{0}\Projects\TigerLead\FinalReport\TL-RDCMatchingSecondRoundTest.txt",ConfigurationManager.AppSettings["Drive"])));

            int rowcount = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (rowcount > 0)
                {
                    
                    var values = line.Split('\t');

                    returnvalue.Add(new TigerLeadReportRow
                                        {
                                            TigerLeadName = values[1],
                                            ModuleID = values[3],
                                            Notes = values[4],
                                            ORCAReadyExtra = values[7],
                                            ORCAReadyMatch = values[5],
                                            RDCID = values[2],
                                            TigerLeadCode = values[0],
                                            TigerLeadCodeOld = values[10],
                                            TigerLeadNameOld = values[11],
                                            TigerLeadOnly = values[9],
                                            TPOnlyMatch = values[6],
                                            TPOnlyExtra = values[8],
                                        });
                    
                }
                rowcount++;
            }

            return returnvalue;

        }
    }
}
