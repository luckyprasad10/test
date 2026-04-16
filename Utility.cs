using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace ServiceMonitor
{
    public class Utility
    {        
        
        //Used in multiple places for database qerying - 7th Jan 2020
        public static int GetWeekNumberOfMonth(DateTime date)
        {
            date = date.Date;
            DateTime firstMonthDay = new DateTime(date.Year, date.Month, 1);
            DateTime firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            if (firstMonthMonday > date)
            {
                firstMonthDay = firstMonthDay.AddMonths(-1);
                firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            }
            return (date - firstMonthMonday).Days / 7 + 1;
        }
        //to read Database connection string from a file
        /// <summary>
        /// MatsCode is the new benchname used earlier.
        /// This function will check in the database node table if the staion exists
        /// </summary>
        /// <param name="MatsCode"></param>
        /// <returns></returns>
        public static JObject ValidateNode(string MatsCode , JArray clients)
        {
            JObject ReturnObj = new JObject();
            try
            {
                foreach (JObject clientObject in clients)
                {
                    if (clientObject["name"].ToString() == MatsCode)
                    {
                        return clientObject;
                    }
                }
                
                return ReturnObj;
            }
            catch (Exception ex)
            {
                if (GlobalVar.IsDebugModeEnabled)
                    Console.WriteLine("Utility ValidateNode Error: {0}", ex.Message);
            }
            return ReturnObj;
        }//EO public JObject ValidateNode(string MatsCode) 

        //public static JObject ValidateNodeUsingFile(string MatsCode)
        //{
        //    JObject ReturnObj = new JObject();
        //    try
        //    {
        //        string QueryString = string.Empty;
        //        QueryString = "SELECT * FROM node WHERE mats_code ='" + MatsCode + "';";
        //        ReturnObj = GlobalVar.DBOpp.GetOne(QueryString);
        //        if (ReturnObj.Count > 0)
        //            return ReturnObj;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (GlobalVar.IsDebugModeEnabled)
        //            Console.WriteLine("Utility ValidateNode Error: {0}", ex.Message);
        //    }
        //    return ReturnObj;
        //}//EO public JObject ValidateNode(string MatsCode) 
        //public static bool ValidateUser(string Username, string Password)
        //{
        //    bool ReturnValue = false;
        //    try
        //    {
        //        if (Username.Length > 12)
        //            Username = Username.Substring(0, 8);
        //        bool PasswordStringValid = false;
        //        if (Password.Length > 12)
        //        {
        //            Password = Password.Substring(0, 8);
        //            char[] temp = Password.ToCharArray();
        //            for (int i = 0; i < Password.Length; i++)
        //            {
        //                if (!Char.IsLetterOrDigit(temp[i]))
        //                {
        //                    PasswordStringValid = true;
        //                }
        //            }
        //        }
        //        if (PasswordStringValid)
        //            return false;
        //        string QueryString = string.Empty;
        //        QueryString = "SELECT * FROM mats_user WHERE user_name ='" + Username + "' AND password='" + Password + "';";
        //        JObject ReturnObj = GlobalVar.DBOpp.GetOne(QueryString);
        //        if (ReturnObj.Count > 0)
        //            ReturnValue = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (GlobalVar.IsDebugModeEnabled)
        //            Console.WriteLine("Utility ValidateUser Error: {0}", ex.Message);
        //    }
        //    return ReturnValue;
        //}//EO public bool ValidateUser(string Username, string Password)

        public static JObject ReadDBConfigFile()
        {
            try
            {
                string text = System.IO.File.ReadAllText(@"Resources/DBConnection.json");
                JObject dbConnObject = JObject.Parse(text);
                //Console.WriteLine("Contents of file = {0}", dbConnObject.ToString());
                return dbConnObject;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to read from file : {0}", ex.Message);
            }
            return null;
        }//end of readfile

        public static string[] jSonTokeniser(string jsonData)
        {
            List<string> tokedStrings = new List<string>();
            string[] Val = Regex.Split(jsonData, @"}{");
            if (Val.Length > 1)
            {
                tokedStrings.Add(Val[0] + '}');
                for (int k = 1; k < Val.Length - 1; k++)
                {
                    tokedStrings.Add('{' + Val[k] + '}');
                }
                tokedStrings.Add('{' + Val[Val.Length - 1]);
            }
            else
            {
                tokedStrings.Add(Val[0]);
            }
            return tokedStrings.ToArray();
        }

        //public class ListtoDataTable
        //{
        //    public DataTable ToDataTable<T>(List<T> items)
        //    {
        //        DataTable dataTable = new DataTable(typeof(T).Name);
        //        //Get all the properties by using reflection   
        //        PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //        foreach (PropertyInfo prop in Props)
        //        {
        //            //Setting column names as Property names  
        //            dataTable.Columns.Add(prop.Name);
        //        }
        //        foreach (T item in items)
        //        {
        //            var values = new object[Props.Length];
        //            for (int i = 0; i < Props.Length; i++)
        //            {
        //                values[i] = Props[i].GetValue(item, null);
        //                ////if (Props[i].GetType.Equals( "DateTime"))
        //                ////    values[i] = Convert.ToDateTime(Props[i].GetValue(item, null));

        //            }
        //            dataTable.Rows.Add(values);
        //        }
        //        return dataTable;
        //    }//End of public DataTable ToDataTable<T>(List<T> items)
        //}// End of public class ListtoDataTable


        //public static string ReadSetupFile()
        //{
        //    try
        //    {
        //        string text = System.IO.File.ReadAllText(@"Resources/setup.conf");
        //        string ReportStorePath = text;
        //        return ReportStorePath;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Unable to read from file : {0}", ex.Message);
        //    }
        //    return null;
        //}//end of readfile

        //public static void WriteSetupFile(string setupFilePath, string rptFolderPath)
        //{
        //    try
        //    {
        //        if (rptFolderPath != null && rptFolderPath != string.Empty)
        //        {
        //            //Overwirtes the content of file
        //            //System.IO.File.WriteAllLines(setupFilePath, new[] { rptFolderPath });
        //            System.IO.File.WriteAllText(setupFilePath, rptFolderPath);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Exception in file writing: {0}", e.Message);
        //    }
        //}//end of writefile



    }//End of Utility class
}
