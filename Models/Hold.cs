using System;

namespace OakGov.Etl.ZumoDestination.Models
{
    /// <summary>
    /// Summary description for Hold
    /// </summary>
    public class Hold
    {
        public Hold() { }

        public Hold(HoldBuffer Row)
        {
            this.id = string.Format("{0}-{1}", Row.ID, Row.WARRANTNUM);
            this.inmateId = Row.ID;
            this.bondType = Row.BONDTYPE;
            this.charge = Row.CHARGE;
            this.comment = Row.Comment;
            this.holdAgency = Row.HOLDAGENCY;
            this.warrantNum = Row.WARRANTNUM;
            if (!Row.BONDAMOUNT_IsNull)
                this.bondAmount = Row.BONDAMOUNT;
        }

        public string id { get; set; }
        public string inmateId { get; set; }
        public string bondType { get; set; }
        public string charge { get; set; }
        public string comment { get; set; }
        public string holdAgency { get; set; }
        public string warrantNum { get; set; }
        public decimal? bondAmount { get; set; }
    }
}
