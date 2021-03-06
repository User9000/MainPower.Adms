﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MainPower.Adms.Enricher
{
    public abstract class TableDataSource : DataSource
    {
        protected DataTable Data;

        public string IndexColumn { get; set; }

        public override T RequestRecord<T>(string id)
        {
            return RequestRecord<T>(IndexColumn, id);
        }

        public override T RequestRecord<T>(string columnName, string id, SearchMode searchMode = SearchMode.Exact)
        {
            try
            {
                //TODO: have a bunch of match types               
                var s = Data.Columns[columnName].DataType == typeof(string) ? "'" : "";
                //only string searches can be not exact
                if (s == "" && searchMode != SearchMode.Exact)
                {
                    Warn("Search mode for non string data must be SearchMode.Exact");
                    searchMode = SearchMode.Exact;
                }

                DataRow[] result;
                switch (searchMode)
                {
                    case SearchMode.Exact:
                        result = Data.Select($"[{columnName}] = {s}{id}{s}");
                        break;
                    case SearchMode.EndsWith:
                        result = Data.Select($"[{columnName}] LIKE '*{id}'");
                        break;
                    case SearchMode.StartsWith:
                        result = Data.Select($"[{columnName}] LIKE '{id}*'");
                        break;
                    case SearchMode.Contains:
                        result = Data.Select($"[{columnName}] LIKE '*{id}*'");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (result.Length == 0)
                {
                    Debug($"Record Not found with {columnName}:{id}");
                    return null;
                }
                else if (result.Length > 1)
                {
                    Warn($"More than one record found with {columnName}:{id}");
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
    }
}
