namespace RapidApiWithBooking.Models
{
    public class HotelDetailViewModel
    {
        public string HotelName { get; set; }
        public string City { get; set; }
        public string Price { get; set; }
        public string CoverImageURL { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public decimal? ReviewScore { get; set; }
        public string ReviewScoreWord { get; set; }
        public int? ReviewCount { get; set; }
    }
}
