using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.SearchService.DataModel.DTO
{
    public class SchemaUI
    {
        public List<Table> tables { get; set; }
        public List<object> columns { get; set; }

        public SchemaUI()
        {
            tables = new List<Table>();
            columns = new List<object>();
        }
    }

    public class Table{
        public object resourceDetail { get; set; }
        public List<object> linkedColumnResourceDetail { get; set; }
    }

    public class DisplayTableAndColumn
    {
        public List<TableFiled> tables { get; set; }
        public List<Filed> columns { get; set; }

        public DisplayTableAndColumn()
        {
            tables = new List<TableFiled>();
            columns = new List<Filed>();
        }
    }

    public class Filed
    {
        public string resourceId { get; set; }
        public string pidURI { get; set; }
        public List<Filed> subColumns { get; set; }

    }

    public class TableFiled : Filed
    {
        public List<Filed> linkedTableFiled { get; set; }

        public TableFiled()
        {
            linkedTableFiled = new List<Filed>();
        }
    }
}
