using Microsoft.SqlServer.Dts.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OakGov.Etl.ZumoDestination.Models
{
    public class HoldBuffer
    {
        private PipelineBuffer Buffer;
        private BufferNameMap BufferColumnMap;

        public HoldBuffer(PipelineBuffer Buffer, BufferNameMap BufferColumnMap)
        {
            this.Buffer = Buffer;
            this.BufferColumnMap = BufferColumnMap;
        }

        public String ID
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["ID"]);
            }
        }
        public bool ID_IsNull
        {
            get
            {
                return IsNull("ID");
            }
        }

        public String BONDTYPE
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["BONDTYPE"]);
            }
        }
        public bool BONDTYPE_IsNull
        {
            get
            {
                return IsNull("BONDTYPE");
            }
        }

        public String CHARGE
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["CHARGE"]);
            }
        }
        public bool CHARGE_IsNull
        {
            get
            {
                return IsNull("CHARGE");
            }
        }

        public String Comment
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["Comment"]);
            }
        }
        public bool Comment_IsNull
        {
            get
            {
                return IsNull("Comment");
            }
        }

        public String HOLDAGENCY
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["HOLDAGENCY"]);
            }
        }
        public bool HOLDAGENCY_IsNull
        {
            get
            {
                return IsNull("HOLDAGENCY");
            }
        }

        public String WARRANTNUM
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["WARRANTNUM"]);
            }
        }
        public bool WARRANTNUM_IsNull
        {
            get
            {
                return IsNull("WARRANTNUM");
            }
        }

        public Decimal BONDAMOUNT
        {
            get
            {
                return Buffer.GetDecimal(BufferColumnMap["BONDAMOUNT"]);
            }
        }
        public bool BONDAMOUNT_IsNull
        {
            get
            {
                return IsNull("BONDAMOUNT");
            }
        }

        bool IsNull(string ColumnName)
        {
            return Buffer.IsNull(BufferColumnMap[ColumnName]);
        }
    }
}
