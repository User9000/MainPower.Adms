﻿using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace MainPower.Osi.Enricher
{
    public abstract class DataSource : ErrorReporter
    {
        public string Name { get; set; }
        public bool InitializeFailIsFatal { get; set; }

        internal bool Initialize()
        {
            Info($"Initializing DataSource {Name}...");
            return OnInitialize();
        }

        protected abstract bool OnInitialize();
        internal abstract T RequestRecordByIndex<T>(string resourceIndexName, string resourceIndexValue, string id) where T : DataType, new();
        internal abstract T RequestRecordByColumn<T>(string resourceIndexName, string resourceIndexValue, string id, bool exact) where T : DataType, new();
        internal abstract bool SetVale<T>(object indexValue, string columnName, object val) where T : DataType, new();
        internal abstract bool Save<T>() where T : DataType, new();

    }

    internal class CsvSource : DataSource
    {
        private DataTable Data;

        public string FileName { get; set; }

        public string IndexColumn { get; set; }

        protected override bool OnInitialize() 
        {
            try
            {
                Data = Util.GetDataTableFromCsv(Path.Combine(Enricher.I.Options.DataPath, FileName), true);
                //speed 'select'
                if (!string.IsNullOrWhiteSpace(IndexColumn))
                {
                    Data.PrimaryKey = new DataColumn[1] { Data.Columns[IndexColumn] };
                    DataView dv = new DataView(Data)
                    {
                        Sort = IndexColumn
                    };
                }
                return true;
            }
            catch (Exception ex)
            {
                Fatal(ex.Message);
                return false;
            }
        }

        internal override T RequestRecordByIndex<T>(string table, string columnName, string id)
        {
            try
            {
                var s = Data.Columns[columnName].DataType == typeof(string) ? "'" : "";

                var result = Data.Select($"[{columnName}] = {s}{id}{s}");
                if (result.Length == 0)
                {
                    Debug($"{table}: Not found with {columnName}:{id}");
                    return null;
                }
                else if (result.Length > 1)
                {
                    Warn($"{table}: More than one {table} found with {columnName}:{id}");
                }
                var obj = new T();
                obj.FromDataRow(result[0]);
                return obj;
            }
            catch (Exception ex)
            {
                Fatal(ex.Message);
                return null;
            }
        }

        internal override T RequestRecordByColumn<T>(string table, string columnName, string id, bool exact)
        {
            try
            {
                //var s = Data.Columns[columnName].DataType == typeof(string) ? "'" : "";
                //TODO handle non string datatypes
                //TODO handle exact
                var result = Data.Select($"[{columnName}] LIKE '* {id}'");
                if (result.Length == 0)
                {
                    Debug($"{table}: Not found with {columnName}:{id}");
                    return null;
                }
                else if (result.Length > 1)
                {
                    Warn($"{table}: More than one {table} found with {columnName}:{id}");
                }
                var obj = new T();
                obj.FromDataRow(result[0]);
                return obj;
            }
            catch (Exception ex)
            {
                Fatal(ex.Message);
                return null;
            }
        }

        
        internal override bool SetVale<T>(object indexValue, string columnName, object val)
        {
            try
            {
                string c = Data.Columns[IndexColumn].DataType == typeof(string) ? "'" : "";

                var result = Data.Select($"[{IndexColumn}] = {c}{indexValue.ToString()}{c}");
                foreach (var r in result)
                {
                    r[columnName] = val;
                }
                return true;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return false;
            }

        }

        internal override bool Save<T>()
        {
            try
            {
                Data.AcceptChanges();
                Util.ExportDatatable(Data, Path.Combine(Enricher.I.Options.DataPath, FileName));
                return true;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return false;
            }


        }
    }
    /*
    internal class SqlSource : DataSource
    {

    }
    internal class SqliteSource : DataSource
    {

    }
    internal class OsiDatabaseSource : DataSource
    {

    }
    internal class MsSqlSource : DataSource
    {


    }
    */
}