using System;

namespace OakGov.Etl.ZumoDestination.Models
{
    /// <summary>
    /// Summary description for Inmate
    /// </summary>
    public class Inmate
    {
        public Inmate() { }

        public Inmate(InmateBuffer Row)
        {
            this.id = Row.INMATEID;
            this.bookingId = Row.BOOKNBR;
            this.lastName = Row.LASTNM;
            this.firstName = Row.FIRSTNM;
            this.middleName = Row.MIDDLENM;
            if (!Row.BOOKDT_IsNull)
            {
                this.bookedDate = Row.BOOKDT;
            }
            if (!Row.BOOKED_IsNull)
            {
                this.booked = Row.BOOKED;
            }
            if (!Row.DOB_IsNull)
                this.birthDate = Row.DOB;
            if (!Row.PHYSRLSDT_IsNull)
            {
                this.released = Row.RELEASED;
                this.releaseDate = Row.PHYSRLSDT;
            }
            this.gender = Row.GENDERCD;
            this.jailLocation = Row.FACILITYDESC;
            if (!Row.ACTIVEHOLDS_IsNull)
                this.activeHolds = Row.ACTIVEHOLDS;
        }

        public string id { get; set; }
        public string lastName { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public DateTime? booked { get; set; }
        public DateTime? bookedDate { get; set; }
        public DateTime? birthDate { get; set; }
        public DateTime? released { get; set; }
        public DateTime? releaseDate { get; set; }
        public string gender { get; set; }
        public string jailLocation { get; set; }
        public decimal? activeHolds { get; set; }
        public string inmateImage { get; set; }
        public string bookingId { get; set; }

    }
}
