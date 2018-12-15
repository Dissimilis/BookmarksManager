using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookmarksManager.Firefox
{
    public class DynamicReader : DynamicObject
    {
        readonly SQLiteDataReader _baseReader;

        public DynamicReader(SQLiteDataReader baseReader)
        {
            _baseReader = baseReader;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            if (HasColumn(_baseReader, binder.Name))
            {
                result = _baseReader[binder.Name];
                if (result == DBNull.Value)
                    result = null;
            }
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            switch (binder.Name)
            {
                case "Read":
                    result = _baseReader.Read();
                    break;
                case "Close":
                    _baseReader.Close();
                    break;
                case "Dispose()":
                    _baseReader.Dispose();
                    break;
            }
            return true;
        }

        private bool HasColumn(IDataRecord dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
