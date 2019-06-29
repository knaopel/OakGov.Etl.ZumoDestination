using Microsoft.SqlServer.Dts.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OakGov.Etl.ZumoDestination.Models
{
    public class InmateBuffer
    {
        private PipelineBuffer Buffer;
        private BufferNameMap BufferColumnMap;

        public InmateBuffer(PipelineBuffer Buffer, BufferNameMap BufferColumnMap)
        {
            this.Buffer = Buffer;
            this.BufferColumnMap = BufferColumnMap;
        }

        public String LASTNM
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["LAST_NM"]);
            }
        }

        public bool LASTNM_IsNull
        {
            get
            {
                return IsNull("LAST_NM");
            }
        }

        public String FIRSTNM
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["FIRST_NM"]);
            }
        }
        public bool FIRSTNM_IsNull
        {
            get
            {
                return IsNull("FIRST_NM");
            }
        }

        public String MIDDLENM
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["MIDDLE_NM"]);
            }
        }
        public bool MIDDLENM_IsNull
        {
            get
            {
                return IsNull("MIDDLE_NM");
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

        public DateTime DOB
        {
            get
            {
                return Buffer.GetDateTime(BufferColumnMap["DOB"]);
            }
        }
        public bool DOB_IsNull
        {
            get
            {
                return IsNull("DOB");
            }
        }

        public String GENDERCD
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["GENDER_CD"]);
            }
        }
        public bool GENDERCD_IsNull
        {
            get
            {
                return IsNull("GENDER_CD");
            }
        }

        public String FACILITYDESC
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["FACILITY_DESC"]);
            }
        }
        public bool FACILITYDESC_IsNull
        {
            get
            {
                return IsNull("FACILITY_DESC");
            }
        }

        public DateTime PHYSRLSDT
        {
            get
            {
                return Buffer.GetDateTime(BufferColumnMap["PHYS_RLS_DT"]);
            }
        }
        public bool PHYSRLSDT_IsNull
        {
            get
            {
                return IsNull("PHYS_RLS_DT");
            }
        }

        public DateTime BOOKDT
        {
            get
            {
                return Buffer.GetDateTime(BufferColumnMap["BOOK_DT"]);
            }
        }
        public bool BOOKDT_IsNull
        {
            get
            {
                return IsNull("BOOK_DT");
            }
        }

        public Decimal ACTIVEHOLDS
        {
            get
            {
                return Buffer.GetDecimal(BufferColumnMap["ACTIVE_HOLDS"]);
            }
        }
        public bool ACTIVEHOLDS_IsNull
        {
            get
            {
                return IsNull("ACTIVE_HOLDS");
            }
        }

        public String BOOKNBR
        {
            get
            {
                return Buffer.GetString(BufferColumnMap["BOOK_NBR"]);
            }
        }
        public bool BOOKNBR_IsNull
        {
            get
            {
                return IsNull("BOOK_NBR");
            }
        }

        public DateTime RELEASED
        {
            get
            {
                return Buffer.GetDateTime(BufferColumnMap["RELEASED"]);
            }
        }
        public bool RELEASED_IsNull
        {
            get
            {
                return IsNull("RELEASED");
            }
        }

        public DateTime BOOKED
        {
            get
            {
                return Buffer.GetDateTime(BufferColumnMap["BOOKED"]);
            }
        }
        public bool BOOKED_IsNull
        {
            get
            {
                return IsNull("BOOKED");
            }
        }

        bool IsNull(string ColumnName)
        {
            return this.Buffer.IsNull(BufferColumnMap[ColumnName]);
        }
    }
}
