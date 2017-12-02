using System.Collections.Generic;

namespace SpeedUp.Model
{
    public class SearchField
    {
        //Overview: SearchField is a mutable datatype that represents a Search Field from a DEF file
        //and it's attributes.

        //NOT DONE!!!!!!!!!!!!!!!!!!!!!!!!!!!

        //Attributes
        private string FldName;
        private string STDFldMapping;
        private string TCSFldMapping;
        private string NonSTDFldMapping;
        private string SystemName;
        private List<string> FldDef = new List<string>();
        private List<List<string>> FldScript = new List<List<string>>();
        private bool Modified;
        private bool ExpectedPattern;
        private bool NewField;
        private int GlobalPosition;
        private int NonStdPosition;

        //Constructor
        public SearchField()
        {
            //REQUIRES: Nothing
            //MODIFIES: this.FldName, this.STDFldMapping, this.TCSFldMapping, this.NonSTDFldMapping
            //EFFECTS: Creates a mutable object of custom datatype SearchField, all attributes are intialized to blank

            //NOT DONE!!!!!!!!!!!!!!!!!!!!!!!!!!!

            FldName = "";
            STDFldMapping = "";
            TCSFldMapping = "";
            NonSTDFldMapping = "";
            Modified = false;
            ExpectedPattern = true;

        }

        //Methods
        //Observers
        public string getFldName()
        {
            //EFFECTS: Returns the name of the search field(this.FldName)

            string nameOut = this.FldName;
            return nameOut;
        }

        public string getSTDFldMapping()
        {
            //EFFECTS: Returns the search field mapping to a standard field(this.STDFldMapping)

            string nameOut = this.STDFldMapping;
            return nameOut;
        }

        public string getTCSFldMapping()
        {
            //EFFECTS: Returns the search field mapping to a TCS field(this.TCSFldMapping)

            string nameOut = this.TCSFldMapping;
            return nameOut;
        }

        public string getNonSTDFldMapping()
        {
            //EFFECTS: Returns the search field mapping to a Non standard field(this.NonSTDFldMapping)

            string nameOut = this.NonSTDFldMapping;
            return nameOut;
        }

        public string getSystemName()
        {
            //EFFECTS: Returns the system name of the field which is searched(this.SystemName)

            string nameOut = this.SystemName;
            return nameOut;
        }

        public List<string> getFldDef()
        {
            //EFFECTS: Returns the search field definition(this.FldDef)

            List<string> nameOut = this.FldDef;
            return nameOut;
        }

        public List<List<string>> getFldScript()
        {
            //EFFECTS: Returns the search field scripting(this.FldScript)

            List<List<string>> nameOut = this.FldScript;
            return nameOut;
        }

        public bool ModifiedFlag()
        {
            //EFFECTS: Returns the true if this field has been modifed, false if it hasn't

            bool flagOut = this.Modified;
            return flagOut;
        }

        public int getNonSTDPos()
        {
            //EFFECTS: Returns the the NonStandard Field positions(this.NonStdPosition)

            int posOut = this.NonStdPosition;
            return posOut;
        }


        //Mutators
        public void setFLDName(string nameIn)
        {
            //REQUIRES: nameIn != (null || "")
            //MODIFIES: this.FldName
            //EFFECTS: Set the value of this.FldName to nameIn

            if ((nameIn != null) || (nameIn != ""))
            {
                FldName = nameIn;
            }
        }

        public void setSTDFldMapping(string nameIn)
        {
            //REQUIRES: nameIn != (null || "")
            //MODIFIES: this.STDFldMapping
            //EFFECTS: Set the value of this.STDFldMapping to nameIn

            if ((nameIn != null) || (nameIn != ""))
            {
                STDFldMapping = nameIn;
            }
        }

        public void addSTDFldMapping(string nameIn)
        {
            //REQUIRES: nameIn != (null || "")
            //MODIFIES: this.STDFldMapping
            //EFFECTS: Append ",nameIn" to the value of STDFldMapping

            if ((nameIn != null) || (nameIn != ""))
            {
                STDFldMapping = STDFldMapping + "," + nameIn;
            }
        }

        public void setTCSFldMapping(string nameIn)
        {
            //REQUIRES: nameIn != (null || "")
            //MODIFIES: this.TCSFldMapping
            //EFFECTS: Set the value of this.TCSFldMapping to nameIn

            if ((nameIn != null) || (nameIn != ""))
            {
                TCSFldMapping = nameIn;
            }
        }

        public void setNonSTDFldMapping(string nameIn)
        {
            //REQUIRES: nameIn != (null || "")
            //MODIFIES: this.NonSTDFldMapping
            //EFFECTS: Set the value of this.NonSTDFldMapping to nameIn

            if ((nameIn != null) || (nameIn != ""))
            {
                NonSTDFldMapping = nameIn;
            }
        }

        public void setSystemName(string nameIn)
        {
            //REQUIRES: nameIn != (null || "")
            //MODIFIES: this.SystemName
            //EFFECTS: Set the value of this.SystemName to nameIn

            if ((nameIn != null) || (nameIn != ""))
            {
                SystemName = nameIn;
            }
        }

        public void setFldDef(List<string> defIn)
        {
            //REQUIRES: defIn != null
            //MODIFIES: this.FldDef
            //EFFECTS: Set the value of this.FldDef to nameIn

            if (defIn != null)
            {
                FldDef = defIn;
            }
        }

        public void setNonSTDPos(int posIn)
        {
            //REQUIRES: posIn > 0
            //MODIFIES: this.NonStdPosition
            //EFFECTS: Set the value of this.NonStdPosition to posIn

            if (posIn > 0)
            {
                NonStdPosition = posIn;
            }
            else if (posIn == -100)
            {
                NonStdPosition = 0;
            }
        }

        public void setFldScript(List<string> defIn, int index)
        {
            //REQUIRES: defIn != null
            //MODIFIES: this.FldScript
            //EFFECTS: Set the value of this.FldScript[index] to nameIn

            if (defIn != null)
            {
                if (index > FldScript.Count - 1)
                {
                    FldScript.Add(defIn);
                }
                else
                {
                    FldScript[index] = defIn;
                }
            }
        }

        public void addFldDef(string defIn)
        {

            if (defIn != null)
            {
                FldDef.Add(defIn);
            }
        }

        public void addFldScript(List<string> defIn)
        {
            if (defIn != null)
            {
                List<string> newList = new List<string>();
                foreach (string i in defIn)
                {
                    newList.Add(i);
                }
                FldScript.Add(newList);
            }
        }

        public void ModifiedYes()
        {
            //REQUIRES: nothing
            //MODIFIES: this.Modified
            //EFFECTS: Set the value of this.Modified to True

                this.Modified = true;
        }

        public void RemoveMinScriptFollowsPattern(bool value)
        {
            //REQUIRES: value = true || value = false
            //MODIFIES: this.ExpectedPattern
            //EFFECTS: Set the value of this.ExpectedPattern to value

            this.ExpectedPattern = value;
        }

        public bool getRemoveMinScriptFollowsPattern()
        {
            //REQUIRES: value = true || value = false
            //MODIFIES: this.ExpectedPattern
            //EFFECTS: Set the value of this.ExpectedPattern to value

            return this.ExpectedPattern;
        }


    }
}
