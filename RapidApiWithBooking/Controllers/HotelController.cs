using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RapidApiWithBooking.Models;

namespace BookingOtelProje.Controllers
{
    public class HotelController : Controller
    {
        private const string APIKEY = "yourapikey";
        private const string APIHOST = "booking-com18.p.rapidapi.com";

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchReservation(string query, string checkinDate, string checkoutDate, int adults, int children)
        {
           

            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(checkinDate) || string.IsNullOrEmpty(checkoutDate))
            {
                ViewBag.ErrorMessage = "Lütfen tüm alanları doldurun.";
                return View("Index");
            }

            var locationId = await GetLocationById(query);
            if (locationId == null)
            {
                ViewBag.ErrorMessage = "Şehir bulunamadı!";
                return View("Index");
            }

            var hotels = await GetAllHotels(locationId, checkinDate, checkoutDate, adults, children);

            ViewBag.CheckinDate = checkinDate;
            ViewBag.CheckoutDate = checkoutDate;

            return View("GetAllHotels", hotels);
        }

        public async Task<IActionResult> HotelDetail(string hotelId, string checkinDate, string checkoutDate)
        {
            if (string.IsNullOrEmpty(hotelId))
                return RedirectToAction("Index");

            var detail = await GetHotelDetail(hotelId, checkinDate, checkoutDate);
            if (detail == null)
            {
                ViewBag.ErrorMessage = "Otel detayları alınamadı.";
                return View("Error");
            }

            return View("GetHotelDetail", detail);
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-rapidapi-key", APIKEY);
            client.DefaultRequestHeaders.Add("x-rapidapi-host", APIHOST);
            return client;
        }

        private async Task<string> GetLocationById(string query)
        {
            using var client = CreateClient();

            var response = await client.GetAsync($"https://{APIHOST}/stays/auto-complete?query={query}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            return data?.data?[0]?.id;
        }

        private async Task<List<HotelsViewModel>> GetAllHotels(string locationId, string checkinDate, string checkoutDate, int adults, int children)
        {
            if (!DateTime.TryParse(checkinDate, out var checkin) || !DateTime.TryParse(checkoutDate, out var checkout))
                throw new ArgumentException("Geçersiz tarih formatı.");

            string formattedCheckin = checkin.ToString("yyyy-MM-dd");
            string formattedCheckout = checkout.ToString("yyyy-MM-dd");

            using var client = CreateClient();

            var url = $"https://{APIHOST}/stays/search?locationId={locationId}&checkinDate={formattedCheckin}&checkoutDate={formattedCheckout}&adults={adults}&children={children}&units=metric&temperature=c";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode) return new List<HotelsViewModel>();

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            var hotels = new List<HotelsViewModel>();

            foreach (var item in data?.data)
            {
                var hotel = new HotelsViewModel
                {
                    HotelId = item.id,
                    HotelName = item.name,
                    ReviewScore = item.reviewScore != null ? (decimal?)item.reviewScore : null,
                    ReviewCount = item.reviewCount != null ? (int?)item.reviewCount : null,
                    ReviewScoreWord = item.reviewScoreWord != null ? (string)item.reviewScoreWord : "Yorum yok",
                    City = item.city != null ? (string)item.city : "Bilinmiyor",
                    Price = item.price != null ? (string)item.price : "Fiyat bilgisi yok",
                    Photos = new List<PhotosByHotelViewModel>()
                };

                var photos = await GetHotelPhotos(hotel.HotelId);
                if (photos?.Count > 0)
                {
                    hotel.Photos = photos;
                    hotel.CoverImageURL = photos[0].ImageUrl;
                }
                else if (item.photoUrls != null && item.photoUrls.Count > 0)
                {
                    hotel.CoverImageURL = item.photoUrls[0];
                }

                hotels.Add(hotel);
            }

            return hotels;
        }

        private async Task<List<PhotosByHotelViewModel>> GetHotelPhotos(string hotelId)
        {
            using var client = CreateClient();

            var response = await client.GetAsync($"https://{APIHOST}/stays/get-photos?hotelId={hotelId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            var photos = new List<PhotosByHotelViewModel>();

            try
            {
                var photoData = data?.data?.data?[hotelId];
                if (photoData != null && photoData.Count > 0)
                {
                    foreach (var photoItem in photoData)
                    {
                        string url = photoItem[4]?[31]?.ToString() ?? photoItem[0]?.url_max300?.ToString() ?? photoItem[0]?.url_original?.ToString();

                        if (!string.IsNullOrEmpty(url))
                        {
                            string fullUrl = $"{data?.data?.url_prefix}{url}";
                            photos.Add(new PhotosByHotelViewModel { ImageUrl = fullUrl });
                        }
                    }
                }
            }
            catch
            {
                // Hata varsa geç
            }

            return photos;
        }

        private async Task<HotelDetailViewModel> GetHotelDetail(string hotelId, string checkinDate, string checkoutDate)
        {
            if (!DateTime.TryParse(checkinDate, out var checkin) || !DateTime.TryParse(checkoutDate, out var checkout))
                throw new ArgumentException("Geçersiz tarih formatı.");

            string formattedCheckin = checkin.ToString("yyyy-MM-dd");
            string formattedCheckout = checkout.ToString("yyyy-MM-dd");

            using var client = CreateClient();

            var response = await client.GetAsync($"https://{APIHOST}/stays/detail?hotelId={hotelId}&checkinDate={formattedCheckin}&checkoutDate={formattedCheckout}&units=metric");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            System.IO.File.WriteAllText("hotel-detail.json", json); // 👈 Bunu ekliyorsun
            dynamic data = JsonConvert.DeserializeObject(json);

            var city = data?.data?.city ?? "Bilinmiyor";
            string price = null;
            string coverImageUrl = null;
            string address = data?.data?.address;
            string description = data?.data?.description;

            var grossAmount = data?.data?.composite_price_breakdown?.gross_amount;
            if (grossAmount != null)
                price = $"{grossAmount.amount_rounded} {grossAmount.currency}";

            var block = data?.data?.block;
            var photos = block?[0]?.photos;
            if (photos != null && photos.Count > 0)
                coverImageUrl = photos[0]?.url_max300 ?? photos[0]?.url_original;

            if (string.IsNullOrEmpty(coverImageUrl))
            {
                var fallbackPhotos = await GetHotelPhotos(hotelId);
                if (fallbackPhotos?.Count > 0)
                    coverImageUrl = fallbackPhotos[0].ImageUrl;
            }

            return new HotelDetailViewModel
            {
                HotelName = data?.data?.name ?? data?.data?.property?.name ?? "İsim yok",

                City = city,
                Price = price ?? "Fiyat bilgisi yok",
                CoverImageURL = coverImageUrl,
                Address = address,
                Description = description,
                ReviewScore = data?.data?.review_score,
                ReviewScoreWord = data?.data?.review_score_word,
                ReviewCount = data?.data?.review_count
            };
        }
    }
}
