using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class TigerLeadReportBoardCollection
    {
        //key = board code
        private Dictionary<string, List<TigerLeadReportListingTypeEntry>> BoardCollection; 
        public TigerLeadReportBoardCollection()
        {
            BoardCollection = new Dictionary<string, List<TigerLeadReportListingTypeEntry>>();
        }

        public void AddClass(string BoardCode, TigerLeadReportListingTypeEntry Entry)
        {
            if(BoardCollection.ContainsKey(BoardCode))
            {
                var tempCollection = BoardCollection[BoardCode];
                tempCollection.Add(Entry);
                BoardCollection[BoardCode] = tempCollection;
            }
            else
            {
                if (Entry.ListingType.ToLower() == "images")
                {}
                else
                {
                    BoardCollection.Add(BoardCode, new List<TigerLeadReportListingTypeEntry> { Entry });
                }
                
            }
        }

        public bool DoesBoardExist(string BoardCode)
        {
            return BoardCollection.ContainsKey(BoardCode);
        }

            public List<TigerLeadReportListingTypeEntry> ReturnClasses(string BoardCode)
            {
                return BoardCollection[BoardCode];
            }
    }


}
