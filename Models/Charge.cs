using System;
namespace OakGov.Etl.ZumoDestination.Models
{
    /// <summary>
    /// Summary description for Charge
    /// </summary>
    public class Charge
    {
        public Charge() { }

        public Charge(ChargeBuffer Row)
        {
            this.id = Row.CHARGENBR;
            this.inmateId = Row.INMATEID;
            if (!Row.BONDAMT_IsNull)
                this.bondAmount = Row.BONDAMT;
            this.bondType = Row.BONDTYPE;
            this.bondDescription = Row.BONDDESC;
            this.court = Row.COURTDESC;
            this.charge = Row.MICRDESC_IsNull ? null : Row.MICRDESC.Replace('¿', ' ');
        }

        public string id { get; set; }
        public string inmateId { get; set; }
        public decimal? bondAmount { get; set; }
        public string bondType { get; set; }
        public string bondDescription { get; set; }
        public string court { get; set; }
        public string charge { get; set; }
    }
}