using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using System;

public class CSVFileReader
{

    public List<Dictionary<string, string>> GetCSVFileListofDic(string filePath)
    {    
        //try
        {
            List<Dictionary<string, string>> dataList = new List<Dictionary<string, string>>();

            TextAsset textAsset = Resources.Load<TextAsset>(filePath);

            if (textAsset == null)
            {
                Debug.LogError("File not found in Resources: " + filePath);
            }

            string[] lines = textAsset.text.Split('\n');

            if (lines.Length < 2)
            {
                Debug.LogError("Invalid CSV format or empty file.");
            }

            string[] headers = lines[0].Split(';');


            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = SplitCSVLine(lines[i]);

                Dictionary<string, string> data = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length; j++)
                {
                    
                    if (j < values.Length)
                    {
                        if (values[j] == "")
                        {
                            data[headers[j]] = "-1";
                            continue;
                        }
                        data[headers[j]] = (values[j]);
                    }
                    else
                    {
                        data[headers[j]] = "-1"; // If no value, set it as an empty string
                    }
                }

                dataList.Add(data);
            }

            return dataList;
        }
        /*catch(Exception e)
        {
            throw (e);
        }*/
    }

   

   

   

    private string[] SplitCSVLine(string line)
    {
        // Use regex to split CSV line considering quotes
        return line.Split(";");
    }
}
