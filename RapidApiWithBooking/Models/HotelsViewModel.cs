using RapidApiWithBooking.Models;
using System.Collections.Generic;

namespace RapidApiWithBooking.Models
{
    public class HotelsViewModel
    {
        public string HotelId { get; set; }
        public string HotelName { get; set; }
        public string CoverImageURL { get; set; }
        public decimal? ReviewScore { get; set; }
        public string ReviewScoreWord { get; set; }
        public int? ReviewCount { get; set; }
        public List<PhotosByHotelViewModel> Photos { get; set; } = new List<PhotosByHotelViewModel>();
        public string City { get; set; }
        public string Price { get; set; }
    }
}
