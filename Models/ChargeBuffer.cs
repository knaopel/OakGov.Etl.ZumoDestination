using Microsoft.SqlServer.Dts.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OakGov.Etl.ZumoDestination.Models
{
    public class ChargeBuffer
    {
        private PipelineBuffer Buffer;
        private BufferNameMap BufferColumnMap;

        public ChargeBuffer(PipelineBuffer Buffer, BufferNameMap BufferColumnMap)
        {
            this.Buffer = Buffer;
            this.BufferColumnMap = BufferColumnMap;
        }

        public String CHARGENBR
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["CHARGE_NBR"]);
            }
        }

        public bool CHARGENBR_IsNull
        {
            get
            {
                return IsNull("CHARGE_NBR");
            }
        }

        public String INMATEID
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["INMATE_ID"]);
            }
        }
        public bool INMATEID_IsNull
        {
            get
            {
                return IsNull("INMATE_ID");
            }
        }

        public Decimal BONDAMT
        {
            get
            {
                return Buffer.GetDecimal(BufferColumnMap["BOND_AMT"]);
            }
        }
        public bool BONDAMT_IsNull
        {
            get
            {
                return IsNull("BOND_AMT");
            }
        }

        public String BONDTYPE
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["BOND_TYPE"]);
            }
        }
        public bool BONDTYPE_IsNull
        {
            get
            {
                return IsNull("BOND_TYPE");
            }
        }

        public String BONDDESC
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["BOND_DESC"]);
            }
        }
        public bool BONDDESC_IsNull
        {
            get
            {
                return IsNull("BOND_DESC");
            }
        }

        public String COURTDESC
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["COURT_DESC"]);
            }
        }
        public bool COURTDESC_IsNull
        {
            get
            {
                return IsNull("COURT_DESC");
            }
        }

        public String MICRDESC
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["MICR_DESC"]);
            }
        }
        public bool MICRDESC_IsNull
        {
            get
            {
                return IsNull("MICR_DESC");
            }
        }



        bool IsNull(string ColumnName)
        {
            return this.Buffer.IsNull(BufferColumnMap[ColumnName]);
        }
    }
}
