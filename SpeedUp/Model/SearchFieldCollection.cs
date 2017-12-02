using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace SpeedUp.Model
{
    public class SearchFieldCollection<T> where T : SearchField
    {

        //Overview: SearchFieldCollection is a generic collection of types

        private List<SearchField> fields;
        private List<SearchField> fieldsDeleted;

        //Constructors
        public SearchFieldCollection()
        {
            Fields = new List<SearchField>();
            fieldsDeleted = new List<SearchField>();
        }

        public SearchFieldCollection(List<SearchField> DefFieldList)
        {
            Fields = DefFieldList;
            fieldsDeleted = new List<SearchField>();
        }

        public List<SearchField> Fields
        {
            get { return fields; }
            set { fields = value; }
        }

        //Methods
        //Observers
        public bool isEmpty()
        {
            if (Fields.Count == 0)
            {
                return true;
            }
            else
                return false;
        }

        public int size()
        {
            return Fields.Count;
        }

        public void clearDeleted()
        {
            fieldsDeleted.Clear();
        }

        public SearchField atIndex(int i)
        {
            return Fields[i];
        }

        //mutators
        public void addField(SearchField element)
        {
            Fields.Add(element);
        }

        public string getSystemNamefor(string Name)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {

                    return ((SearchField)this.atIndex(i)).getSystemName();
                }
            }

            return "";
        }

        public void deleteField(string Name)
        {

            SearchField temp = new SearchField();
            //bool changemade = false;
            int FieldOrder = -10;
            int FieldIndex = -10;

            //Get the position of selected field
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {
                    if (((SearchField)this.atIndex(i)).getNonSTDPos() > 0)
                    {
                        FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                        FieldIndex = i;
                    }

                }
            }

            //If not 0, move all fields with a higher order, one spot lower
            if ((FieldOrder >= 1) && (FieldIndex != -10))
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getNonSTDPos() > FieldOrder)
                    {
                        ((SearchField)this.atIndex(i)).setNonSTDPos(((SearchField)this.atIndex(i)).getNonSTDPos() - 1);
                        //-100 translates to 0 for this function
                    }
                }
                ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(-100);
                //changemade = true;
            }





                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        temp.setFLDName(Name);
                        temp.setSystemName(((SearchField)this.atIndex(i)).getSystemName());
                        temp.setFldDef(((SearchField)this.atIndex(i)).getFldDef());
                        List<List<string>> FieldScript = ((SearchField)this.atIndex(i)).getFldScript();
                        for (int m = 0; m < FieldScript.Count; m++)
                        {
                            temp.addFldScript(FieldScript[m]);
                        }
                        temp.setNonSTDFldMapping(((SearchField)this.atIndex(i)).getNonSTDFldMapping());
                        temp.setSTDFldMapping(((SearchField)this.atIndex(i)).getSTDFldMapping());
                        temp.setTCSFldMapping(((SearchField)this.atIndex(i)).getTCSFldMapping());

                        //this.deleteField(temp, i);

                        Fields.RemoveAt(i);
                        fieldsDeleted.Add(temp);
                        break;
                    }
                }
                
        }

        //observers
        public bool doesfieldExist(string Name)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName() == Name)
                {
                    return true;
                }
            }
            return false;
        }

        public bool doesSystemNameExist(string Name)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getSystemName() == Name)
                {
                    return true;
                }
            }
            return false;
        }

        //Field Property Getters

        public string getFldNameat(int i)
        {
            if (i >= this.size())
            {
                return ((SearchField)this.atIndex(i)).getFldName();
            }
            else
            {
                return ((SearchField)this.atIndex(this.size() - 1)).getFldName();
            }

        }

        public DataTable getValuesTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Position", typeof(int));
            table.Columns.Add("SearchName", typeof(string));
            table.Columns.Add("Standard Field", typeof(string));
            table.Columns.Add("TCS", typeof(string));
            table.Columns.Add("Non-Standard", typeof(string));
            table.Columns.Add("SystemName", typeof(string));

            for (int i = 0; i < this.size(); i++)
            {
                table.Rows.Add(((SearchField)this.atIndex(i)).getNonSTDPos(),
                    ((SearchField)this.atIndex(i)).getFldName(),
                    ((SearchField)this.atIndex(i)).getSTDFldMapping(),
                    ((SearchField)this.atIndex(i)).getTCSFldMapping(),
                    ((SearchField)this.atIndex(i)).getNonSTDFldMapping(),
                    ((SearchField)this.atIndex(i)).getSystemName());

            }
            return table;
        }

        public List<string> getFldDEFfor(string Name)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName() == Name)
                {
                    return ((SearchField)this.atIndex(i)).getFldDef();
                }
            }

            List<string> TempList = new List<string>();
            TempList.Add("");
            return TempList;
        }

        public List<string> getFldScriptAtfor(string Name, int index)
        {
            List<List<string>> TempList = new List<List<string>>();

            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName() == Name)
                {
                    TempList = ((SearchField)this.atIndex(i)).getFldScript();
                }
            }


            if (index < TempList.Count)
            {
                return TempList[index];
            }
            else
            {
                List<string> list = new List<string>();
                list.Add("Does Not Exist");
                return list;
            }
        }

        //mutators
        public void updateSTDFldMapfor(string Name, string STDFldMap)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {
                    ((SearchField)this.atIndex(i)).setSTDFldMapping(STDFldMap);
                    ((SearchField)this.atIndex(i)).ModifiedYes();
                    break;
                }
            }
        }

        public void updateNamefor(string Name, string NewName)
        {
            SearchField temp = new SearchField();
            List<List<string>> UpdatedScript = new List<List<string>>();
            int saveindex = -1;

            if (!doesSystemNameExist(NewName) && (NewName != ""))
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        temp.setFLDName(Name);
                        temp.setSystemName(((SearchField)this.atIndex(i)).getSystemName());
                        temp.setFldDef(((SearchField)this.atIndex(i)).getFldDef());
                        List<List<string>> FieldScript = ((SearchField)this.atIndex(i)).getFldScript();
                        for (int m = 0; m < FieldScript.Count; m++)
                        {
                            temp.addFldScript(FieldScript[m]);
                        }
                        temp.setNonSTDFldMapping(((SearchField)this.atIndex(i)).getNonSTDFldMapping());
                        temp.setSTDFldMapping(((SearchField)this.atIndex(i)).getSTDFldMapping());
                        temp.setTCSFldMapping(((SearchField)this.atIndex(i)).getTCSFldMapping());

                        //this.deleteField(temp, i);
                        fieldsDeleted.Add(temp);

                        ((SearchField)this.atIndex(i)).setFLDName(NewName);
                        ((SearchField)this.atIndex(i)).ModifiedYes();
                        UpdatedScript = ((SearchField)this.atIndex(i)).getFldScript();
                        saveindex = i;
                        break;
                    }
                }


                for (int k = 0; k < UpdatedScript.Count; k++)
                {
                    for (int j = 0; j < UpdatedScript[k].Count; j++)
                    {
                        if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("ifval")) && (UpdatedScript[k][j].ToLower().Contains("\"\\fields." + Name.ToLower() + "\\\"")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("ifnval")) && (UpdatedScript[k][j].ToLower().Contains("\"\\fields." + Name.ToLower() + "\\\"")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("ifinc")) && (UpdatedScript[k][j].ToLower().Contains("\"\\fields." + Name.ToLower() + "\\")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("ifinfield")) && (UpdatedScript[k][j].ToLower().Contains("\"\\fields." + Name.ToLower() + "\\")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("transval")) && (UpdatedScript[k][j].ToLower().Contains("\"\\fields." + Name.ToLower() + "\\")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("setfield")) && (UpdatedScript[k][j].ToLower().Contains("\"\\fields." + Name.ToLower() + "\\")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("label")) && (UpdatedScript[k][j].ToLower().Contains(Name.ToLower())))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                        else if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].ToLower().Contains("goto")) && (UpdatedScript[k][j].ToLower().Contains(Name.ToLower())))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(Name, NewName);
                        }
                    }
                }


                if ((saveindex >= 0) && (saveindex < this.size()) && (saveindex == 0))
                {
                    for (int k = 0; k < UpdatedScript.Count; k++)
                    {
                        ((SearchField)this.atIndex(saveindex)).setFldScript(UpdatedScript[k], k);
                    }
                    ((SearchField)this.atIndex(saveindex)).ModifiedYes();
                }


            }

        }

        public void updateSystemNamefor(string Name, string SystemName)
        {
            List<List<string>> UpdatedScript = new List<List<string>>();
            string oldsystemname = "";
            int saveindex = -1;

            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {
                    oldsystemname = ((SearchField)this.atIndex(i)).getSystemName();
                    ((SearchField)this.atIndex(i)).setSystemName(SystemName);
                    UpdatedScript = ((SearchField)this.atIndex(i)).getFldScript();
                    ((SearchField)this.atIndex(i)).ModifiedYes();
                    saveindex = i;
                    break;
                }
            }

            for (int k = 0; k < UpdatedScript.Count; k++)
            {
                for (int j = 0; j < UpdatedScript[k].Count; j++)
                {
                    if (oldsystemname != null)
                    {
                        if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].Contains("transmit")) && (UpdatedScript[k][j].Contains(oldsystemname)))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace(oldsystemname, SystemName);
                        }
                    }
                    else
                    {
                        if ((!UpdatedScript[k][j].StartsWith(";")) && (UpdatedScript[k][j].Contains("(=")))
                        {
                            UpdatedScript[k][j] = UpdatedScript[k][j].Replace("(=", "(" +SystemName + "=");
                        }
                    }

                }
            }


            if ((saveindex >= 0) && (saveindex < this.size()) && (saveindex == 0))
            {
                for (int k = 0; k < UpdatedScript.Count; k++)
                {
                    ((SearchField)this.atIndex(saveindex)).setFldScript(UpdatedScript[k], k);
                }
                ((SearchField)this.atIndex(saveindex)).ModifiedYes();
            }

        }

        public string updateTCSFldMapfor(string Name, string TCSFldMap)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName() == Name)
                {
                    //((SearchField)this.atIndex(i)).setTCSFldMapping(TCSFldMap);
                }
            }


            List<int> ListofNumbers = new List<int>();
            string returnname = "";

            if (TCSFldMap == "")
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getTCSFldMapping() != "")
                    {
                        //((SearchField)this.atIndex(i)).setTCSFldMapping(TCSFldMap);

                        ListofNumbers.Add(Convert.ToInt32(((SearchField)this.atIndex(i)).getTCSFldMapping().Split('_').Last()));
                    }
                }

                ListofNumbers = ListofNumbers.Distinct().ToList();
                ListofNumbers.Sort();

                for (int i = 1000; i <= 1000 + ListofNumbers.Count; i++)
                {
                    // Adding the mapping if other mappings exist already and we have space to fit in new mappings within the established order
                    if (((i - 1000) < ListofNumbers.Count()))
                    {
                        if ((ListofNumbers[i - 1000] != i))
                        {
                            returnname = "__CUST__" + i;
                            break;
                        }
                    }

                    // Adding the mapping if other mappings exist already and we have increase the order of mapping by 1
                    else if (i == (1000 + ListofNumbers.Count))
                    {
                        returnname = "__CUST__" + i;

                    }

                    // Adding the mapping if other mappings don't exist
                    else if (0 == ListofNumbers.Count)
                    {
                        returnname = "__CUST__1000";
                    }

                }

                for (int j = 0; j < this.size(); j++)
                {
                    if (((SearchField)this.atIndex(j)).getFldName().ToLower() == Name.ToLower())
                    {
                        ((SearchField)this.atIndex(j)).setTCSFldMapping(returnname);
                        ((SearchField)this.atIndex(j)).ModifiedYes();
                        break;
                    }

                }

            }
            else
            {
                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        ((SearchField)this.atIndex(i)).setTCSFldMapping("");
                        ((SearchField)this.atIndex(i)).ModifiedYes();
                        break;
                    }

                }
            }

            return returnname;
        }

        public string updateNonSTDFldMapfor(string Name, string NonSTDFldMap)
        {
            int FieldOrder = -10;
            int FieldIndex = -10;


            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {
                    FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                    FieldIndex = i;

                }
            }

            //If field order is 0 set to the highest order+1
            if ((FieldOrder == 0) && (FieldIndex != -10))
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getNonSTDPos() > FieldOrder)
                    {
                        FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                        //changemade = true;
                    }
                }
                ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(FieldOrder + 1);
            }
            else if ((FieldOrder > 0) && (FieldIndex != -10))
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getNonSTDPos() > FieldOrder)
                    {
                        ((SearchField)this.atIndex(i)).setNonSTDPos(((SearchField)this.atIndex(i)).getNonSTDPos() - 1);
                        //-100 translates to 0 for this function                            
                    }

                }
                ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(-100);
                //changemade = true;
            }



            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName() == Name)
                {
                    ((SearchField)this.atIndex(i)).setNonSTDFldMapping(NonSTDFldMap);
                }
            }


            List<int> ListofNumbers = new List<int>();
            string returnname = "";

            if (NonSTDFldMap == "")
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getNonSTDFldMapping() != "")
                    {

                        ListofNumbers.Add(Convert.ToInt32(((SearchField)this.atIndex(i)).getNonSTDFldMapping().Split('_').Last()));
                    }
                }

                ListofNumbers = ListofNumbers.Distinct().ToList();
                ListofNumbers.Sort();

                for (int i = 1; i <= 1 + ListofNumbers.Count; i++)
                {


                    // Adding the mapping if other mappings exist already and we have space to fit in new mappings within the established order
                    if ((i < ListofNumbers.Count() + 1))
                    {
                        if ((ListofNumbers[i - 1] != i))
                        {
                            returnname = "__CUST__" + i;
                            break;
                        }
                    }
                    // Adding the mapping if other mappings exist already and we have increase the order of mapping by 1
                    else if (i == ListofNumbers.Count + 1)
                    {
                        returnname = "__CUST__" + i;
                    }

                    // Adding the mapping if other mappings don't exist
                    else if (0 == ListofNumbers.Count)
                    {
                        returnname = "__CUST__1";
                    }


                }

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        //ListofNumbers.Add(Convert.ToInt32(((SearchField)this.atIndex(i)).getNonSTDFldMapping().Split('_').Last()));

                        ((SearchField)this.atIndex(i)).setNonSTDFldMapping(returnname);
                        ((SearchField)this.atIndex(i)).ModifiedYes();
                        break;
                    }
                }

            }
            else
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        ListofNumbers.Add(Convert.ToInt32(((SearchField)this.atIndex(i)).getNonSTDFldMapping().Split('_').Last()));

                        ((SearchField)this.atIndex(i)).setNonSTDFldMapping("");
                        ((SearchField)this.atIndex(i)).ModifiedYes();
                        break;
                    }
                }
            }

            return returnname;


        }

        public void updateTextFldScriptfor(string Name, List<string> BodyText, int index)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {
                    ((SearchField)this.atIndex(i)).setFldScript(BodyText, index);
                    ((SearchField)this.atIndex(i)).ModifiedYes();
                    break;
                }
            }
        }

        public void updateTextFldDeffor(string Name, List<string> BodyText)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                {
                    ((SearchField)this.atIndex(i)).setFldDef(BodyText);
                    ((SearchField)this.atIndex(i)).ModifiedYes();
                    break;
                }
            }
        }

        public bool EditFieldOrder(string Name,string Type)
        {
            bool changemade = false;
            int FieldOrder = -10;
            int FieldIndex = -10;

            if (Type == "Up")
            {
                //Get the position of selected field
                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() > 0)
                        {
                            FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                            FieldIndex = i;
                        }

                    }
                }

                //If current value is not 0, and it isn't max number move up
                if ((FieldOrder >= 0) && (FieldIndex != -10))
                {

                    for (int i = 0; i < this.size(); i++)
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() == (FieldOrder + 1))
                        {
                            ((SearchField)this.atIndex(i)).setNonSTDPos(FieldOrder);
                            ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(FieldOrder+1);
                            changemade = true;
                            break;

                        }
                    }
                }


            }
            else if (Type == "Down")
            {

                //Get the position of selected field
                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() > 0)
                        {
                            FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                            FieldIndex = i;
                        }

                    }
                }

                //If current value is not 0, and it isn't max number move up
                if ((FieldOrder > 1) && (FieldIndex != -10))
                {

                    for (int i = 0; i < this.size(); i++)
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() == (FieldOrder - 1))
                        {
                            ((SearchField)this.atIndex(i)).setNonSTDPos(FieldOrder);
                            ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(FieldOrder - 1);
                            changemade = true;
                            break;

                        }
                    }
                }
            }
            else if (Type == "Delete")
            {

                //Get the position of selected field
                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() > 0)
                        {
                            FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                            FieldIndex = i;
                        }

                    }
                }

                //If not 0, move all fields with a higher order, one spot lower
                if ((FieldOrder >= 1) && (FieldIndex != -10))
                {

                    for (int i = 0; i < this.size(); i++)
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() > FieldOrder)
                        {
                            ((SearchField)this.atIndex(i)).setNonSTDPos(((SearchField)this.atIndex(i)).getNonSTDPos() - 1);
                            //-100 translates to 0 for this function
                        }
                    }
                    ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(-100);
                    changemade = true;
                }
            }
            else if (Type == "Add")
            {

                for (int i = 0; i < this.size(); i++)
                {
                    if (((SearchField)this.atIndex(i)).getFldName().ToLower() == Name.ToLower())
                    {
                        FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                        FieldIndex = i;

                    }
                }

                //If field order is 0 set to the highest order+1
                if ((FieldOrder == 0) && (FieldIndex != -10))
                {

                    for (int i = 0; i < this.size(); i++)
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() > FieldOrder)
                        {
                            FieldOrder = ((SearchField)this.atIndex(i)).getNonSTDPos();
                            changemade = true;
                        }
                    }
                    ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(FieldOrder+1);
                }
                else if ((FieldOrder > 0) && (FieldIndex != -10))
                {

                    for (int i = 0; i < this.size(); i++)
                    {
                        if (((SearchField)this.atIndex(i)).getNonSTDPos() > FieldOrder)
                        {
                            ((SearchField)this.atIndex(i)).setNonSTDPos(((SearchField)this.atIndex(i)).getNonSTDPos() - 1);
                            //-100 translates to 0 for this function                            
                        }

                    }
                    ((SearchField)this.atIndex(FieldIndex)).setNonSTDPos(-100);
                    changemade = true;
                }


            }

            return changemade;
        }

        public bool getSearchFieldPattern(string Name)
        {
            for (int i = 0; i < this.size(); i++)
            {
                if (((SearchField)this.atIndex(i)).getFldName() == Name)
                {
                    return ((SearchField)this.atIndex(i)).getRemoveMinScriptFollowsPattern();
                }
            }
            return false;
        }

        public void updateRemoveMinSearchFieldReq()
        {

            // THIS WILL NEED TO BE REFINED LATER TO INCLUDE SPECIAL CASES AND CHECKING FOR RAPPATONI legitimate
            List<List<string>> UpdatedScript = new List<List<string>>();
            List<string> UpdatedDEF = new List<string>();
            //List<string> UpdatedScript = new List<string>();

            int FirstSet = 0;
            bool TooLate = false;
            bool FollowsExpectedPattern = false;

            for (int i = 0; i < this.size(); i++)
            {
                TooLate = false;
                UpdatedScript = ((SearchField)this.atIndex(i)).getFldScript();
                for (int n = 0; n < UpdatedScript.Count(); n++)
                {
                    for (int k = 0; k < UpdatedScript[n].Count(); k++)
                    {
                        if ((!UpdatedScript[n][k].StartsWith(";")) && UpdatedScript[n][k].Contains("set") && (UpdatedScript[n][k].Contains("\"First=")))
                        {
                            FirstSet = 1;
                            FollowsExpectedPattern = true;
                            break;
                        }
                    }


                    if (FirstSet == 0)
                    {
                        for (int j = 0; j < UpdatedScript[n].Count(); j++)
                        {
                            if ((!UpdatedScript[n][j].StartsWith(";")) && (UpdatedScript[n][j].Contains("transmit \"(")))
                            {
                                TooLate = true;
                                FollowsExpectedPattern = true;
                                break;
                            }
                            if ((!UpdatedScript[n][j].StartsWith(";")) && UpdatedScript[n][j].Contains("transmit") && UpdatedScript[n][j].Contains("\",\"") && (!TooLate))
                            {
                                UpdatedScript[n].RemoveAt(j);
                                UpdatedScript[n].Insert(j, "  ifval \"\\First\\\"");
                                UpdatedScript[n].Insert(j + 1, "     transmit \",\"");
                                UpdatedScript[n].Insert(j + 2, "  endiv \"\"");
                                UpdatedScript[n].Insert(j + 3, "  set \"First=Y\"");
                                FollowsExpectedPattern = true;
                                break;
                            }

                            else if ((!UpdatedScript[n][j].StartsWith(";")) && UpdatedScript[n][j].Contains("transmit") && UpdatedScript[n][j].Contains("\",(") && (!TooLate))
                            {
                                UpdatedScript[n][j] = UpdatedScript[n][j].Replace("transmit \",", "transmit \"");
                                UpdatedScript[n].Insert(j, "  ifval \"\\First\\\"");
                                UpdatedScript[n].Insert(j + 1, "     transmit \",\"");
                                UpdatedScript[n].Insert(j + 2, "  endiv \"\"");
                                UpdatedScript[n].Insert(j + 3, "  set \"First=Y\"");
                                FollowsExpectedPattern = true;
                                break;
                            }
                        }


                        for (int k = 0; k < UpdatedScript[n].Count(); k++)
                        {

                            if ((!UpdatedScript[n][k].StartsWith(";")) && UpdatedScript[n][k].Contains("transmit") && UpdatedScript[n][k].Contains("\",(") && (!TooLate))
                            {
                                UpdatedScript[n][k] = UpdatedScript[n][k].Replace("transmit \",", "transmit \"");
                                k++;
                                FollowsExpectedPattern = true;
                            }
                        }
                    }

                    ((SearchField)this.atIndex(i)).RemoveMinScriptFollowsPattern(FollowsExpectedPattern);
                    for (int m = 0; m < UpdatedScript.Count(); m++)
                    {
                        ((SearchField)this.atIndex(i)).setFldScript(UpdatedScript[m], m);
                    }
                    ((SearchField)this.atIndex(i)).ModifiedYes();
                    FollowsExpectedPattern = false;

                }
                FirstSet = 0;


                for (int t = 0; t < this.size(); t++)
                {
                    UpdatedDEF = ((SearchField)this.atIndex(t)).getFldDef();

                    for (int r = 0; r < UpdatedDEF.Count; r++)
                    {
                        if ((!UpdatedDEF[r].StartsWith(";")) && UpdatedDEF[r].Contains("Required=Yes"))
                        {
                            UpdatedDEF.RemoveAt(r);
                        }

                    }
                    ((SearchField)this.atIndex(t)).setFldDef(UpdatedDEF);
                }
            }


        }


        public List<SearchField> exportSearchFields()
        {
            return this.Fields;
        }

        public List<SearchField> exportDelSearchFields()
        {
            return this.fieldsDeleted;
        }
    }
}
