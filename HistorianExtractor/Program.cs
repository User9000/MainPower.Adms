using CommandLine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace MainPower.Adms.HistorianExtractor
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('s', "server", Required = true, HelpText = "MSSQL Server Address.")]
        public string Server { get; set; }

        [Option('a', "startdate", Required = true, HelpText = "Start date for export data in format yyyymmdd.", Default ="20181001")]
        public string StartDate { get; set; }

        [Option('z', "enddate", Required = true, HelpText = "End date for export data  in format yyyymmdd.", Default ="20181005")]
        public string EndDate { get; set; }

        [Option('o', "output", HelpText = "output location.", Default = @"..\..\..\output\historian\")]
        public string Output { get; set; }

        [Option('r', "rename", HelpText = "Rename and filter tags based on export_allpoints.csv file from ItDbExporter.", Default = @"..\..\..\output\export_allpoints.csv")]
        public string Rename { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       DateTime start = DateTime.Now;
                       SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder
                       {
                           DataSource = o.Server,
                           InitialCatalog = "Runtime",
                           IntegratedSecurity = true
                       };

                       using (SqlConnection c = new SqlConnection(csb.ConnectionString))
                       {
                           c.Open();
                           ExportAnalogTags(c, o);
                           ExportDiscreteTags(c, o);
                       }
                       Console.WriteLine("Completed in " + (DateTime.Now - start).TotalMinutes + "minutes");
                       Console.WriteLine("Press any key to exit...");
                       Console.ReadKey();

                   });
        }

        private static void ExportAnalogTags(SqlConnection c, Options o)
        {
            if (o.Verbose)
                Console.WriteLine("Exporting analog tags...");
            var sq1 = "SELECT TagName from Runtime.dbo.AnalogTag";
            SqlCommand q1 = new SqlCommand(sq1, c);
            var rq1 = q1.ExecuteReader();
            List<string> tags = new List<string>();
            try
            {
                while (rq1.Read())
                {
                    if (o.Verbose)
                        Console.WriteLine($"{rq1["TagName"]}");
                    tags.Add(rq1["TagName"] as string);
                }
            }
            finally
            {
                rq1.Close();
            }
            //in case it doesn't already exist
            Directory.CreateDirectory("output");

            foreach (var tag in tags)
            {
                //TODO: filter output
                if (o.Verbose)
                    Console.WriteLine($"Exporting CSV for tag {tag}");
                var sq2 = @"SELECT (CONVERT(varchar(35), DateTime, 127) + 'Z')  AS Time, Value, QualityDetail, PercentGood
FROM Runtime.dbo.History WHERE
wwRetrievalMode = 'Average'
AND wwResolution = 300000
AND wwInterpolationType = 'Linear'
AND wwVersion = 'Latest'
AND wwTimeZone = 'UTC'
AND DateTime >= @StartDateTime
AND DateTime <= @EndDateTime
AND TagName = @p
order by DateTime desc;";
                SqlCommand q2 = new SqlCommand(sq2, c);
                q2.Parameters.AddWithValue("@p", tag);
                q2.Parameters.AddWithValue("@StartDateTime", o.StartDate);
                q2.Parameters.AddWithValue("@EndDateTime", o.EndDate);
                var rq2 = q2.ExecuteReader();

                try
                {
                    if (o.Verbose)
                        Console.WriteLine($"{tag}");

                    CreateCsvFile(rq2, File.CreateText($"{o.Output}{MakeValidFileName(tag)}.csv"));
                }
                finally
                {
                    // Always call Close when done reading.
                    rq2.Close();
                }
            }
        }

        private static void ExportDiscreteTags(SqlConnection c, Options o)
        {
            if (o.Verbose)
                Console.WriteLine("Exporting analog tags...");
            var sq1 = "SELECT TagName from Runtime.dbo.DiscreteTag";
            SqlCommand q1 = new SqlCommand(sq1, c);
            var rq1 = q1.ExecuteReader();
            List<string> tags = new List<string>();
            try
            {
                while (rq1.Read())
                {
                    if (o.Verbose)
                        Console.WriteLine($"{rq1["TagName"]}");
                    tags.Add(rq1["TagName"] as string);
                }
            }
            finally
            {
                // Always call Close when done reading.
                rq1.Close();
            }
            //in case it doesn't already exist
            Directory.CreateDirectory("output");

            foreach (var tag in tags)
            {
                //TODO: filter output
                if (o.Verbose)
                    Console.WriteLine($"Exporting CSV for tag {tag}");
                var sq2 = @"SELECT (CONVERT(varchar(35), DateTime, 127) + 'Z') AS Time, Value, QualityDetail
FROM Runtime.dbo.History WHERE
wwRetrievalMode = 'Delta'
AND wwVersion = 'Latest'
AND wwTimeZone = 'UTC'
AND DateTime >= @StartDateTime
AND DateTime <= @EndDateTime
AND TagName = @p
order by DateTime desc;";
                SqlCommand q2 = new SqlCommand(sq2, c);
                q2.Parameters.AddWithValue("@p", tag);
                q2.Parameters.AddWithValue("@StartDateTime", o.StartDate);
                q2.Parameters.AddWithValue("@EndDateTime", o.EndDate);
                var rq2 = q2.ExecuteReader();

                try
                {
                    if (o.Verbose)
                        Console.WriteLine($"{tag}");

                    CreateCsvFile(rq2, File.CreateText(o.Output + (MakeValidFileName(tag) + ".csv")));
                }
                finally
                {
                    rq2.Close();
                }
            }
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        public static void CreateCsvFile(IDataReader reader, StreamWriter writer)
        {
            string Delimiter = "\"";
            string Separator = ",";

            // write header row
            for (int columnCounter = 0; columnCounter < reader.FieldCount; columnCounter++)
            {
                if (columnCounter > 0)
                {
                    writer.Write(Separator);
                }
                writer.Write(Delimiter + reader.GetName(columnCounter) + Delimiter);
            }
            writer.WriteLine(string.Empty);

            // data loop
            while (reader.Read())
            {
                // column loop
                for (int columnCounter = 0; columnCounter < reader.FieldCount; columnCounter++)
                {
                    if (columnCounter > 0)
                    {
                        writer.Write(Separator);
                    }
                    writer.Write(Delimiter + reader.GetValue(columnCounter).ToString().Replace('"', '\'') + Delimiter);
                }   // end of column loop
                writer.WriteLine(string.Empty);
            }   // data loop

            writer.Flush();
        }
    }
}
