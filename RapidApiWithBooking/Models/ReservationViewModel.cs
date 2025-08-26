namespace RapidApiWithBooking.Models
{
    public class ReservationViewModel
    {
        public string CheckinDate { get; set; }
        public string CheckoutDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
    }
}
