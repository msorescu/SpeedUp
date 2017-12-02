using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using SpeedUp.Helper;

namespace SpeedUp.Model
{
    public class StandardFieldDataDictionary
    {
        private ObservableCollection<StandardFieldFromDD> _standardFieldCollection = new ObservableCollection<StandardFieldFromDD>(); 
        
        private static StandardFieldDataDictionary _instance;

        private StandardFieldDataDictionary() { LoadDd(); }

        public static StandardFieldDataDictionary Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StandardFieldDataDictionary();
                return _instance;
            }
        }
        
        public ObservableCollection<StandardFieldFromDD> StandardFieldCollection
        {
            get { return _standardFieldCollection; }
        }

        private void LoadDd()
        {
            DataTable dt = SharepointXlsHelper.ReadStandardFieldFromExcel();
            if (dt != null && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    StandardFieldFromDD sf = new StandardFieldFromDD();
                    DataRow row = dt.Rows[i];
                    sf.FieldName = CommonUtilities.GetStringFormRow(row, "Field name");
                    sf.Required = CommonUtilities.GetStringFormRow(row, "Required");
                    sf.XmlNameResult = CommonUtilities.GetStringFormRow(row, "XML name - Results");
                    sf.DefNameResult = CommonUtilities.GetStringFormRow(row, "DEF Name - Results");
                    sf.FieldType = CommonUtilities.GetStringFormRow(row, "Result validation data type");
                    sf.DisplayName = CommonUtilities.GetStringFormRow(row, "Default Display name");
                    sf.AttributeExposure = CommonUtilities.GetStringFormRow(row, "Default Attribute Exposure");
                    sf.DisplayRule = CommonUtilities.GetStringFormRow(row, "Default Display Rule");
                    sf.Category = CommonUtilities.GetStringFormRow(row, "Default Category").Trim('\n'); //temporary fix for 198653, when DD goes to database, the issue will go.
                    sf.Comments = CommonUtilities.GetStringFormRow(row, "Description and Mapping Comments");
                    sf.Automap = CommonUtilities.GetStringFormRow(row, "Automap");
                    sf.LogicalOrder = CommonUtilities.GetStringFormRow(row, "Logical Sort");
                    sf.PromoteToBullets = CommonUtilities.GetStringFormRow(row, "Promote to bullets"); //Promote to bullets
                    StandardFieldCollection.Add(sf);
                }
            }
        }

        public string GetDefaultCategory(string defName)
        {
            var query =
                _standardFieldCollection.FirstOrDefault(x => x.DefNameResult.ToLower() == defName.ToLower() && x.DefNameResult.Length > 0);
            if (query != null) return query.Category;
            return "";
        }
    }
}
