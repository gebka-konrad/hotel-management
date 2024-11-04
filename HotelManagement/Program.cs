using HotelManagement.Models;
using Newtonsoft.Json;

namespace HotelManagement
{
    public class Program
    {
        static List<Hotel>? Hotels;
        static List<Booking>? Bookings;

        static void Main(string[] args)
        {
            // Parse command-line arguments to get file paths
            string? hotelsFilePath = null;
            string? bookingsFilePath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--hotels")
                    hotelsFilePath = args[i + 1];
                else if (args[i] == "--bookings")
                    bookingsFilePath = args[i + 1];
            }

            if (string.IsNullOrEmpty(hotelsFilePath) || string.IsNullOrEmpty(bookingsFilePath))
            {
                Console.WriteLine("Please specify the paths to hotels and bookings files using --hotels and --bookings.");
                return;
            }

            // Load hotels and bookings from JSON files
            Hotels = LoadHotels(hotelsFilePath);
            Bookings = LoadBookings(bookingsFilePath);

            // Main loop for user commands
            while (true)
            {
                Console.Write("Enter command: ");
                var input = Console.ReadLine()!;
                if (string.IsNullOrWhiteSpace(input))
                    break;

                if (input.StartsWith("Availability"))
                    HandleAvailability(input);
                else if (input.StartsWith("Search"))
                    HandleSearch(input);
                else
                    Console.WriteLine("Invalid command.");
            }
        }

        public static List<Hotel> LoadHotels(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Hotel>>(json)!;
        }

        public static List<Booking> LoadBookings(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Booking>>(json)!;
        }

        public static void HandleAvailability(string input)
        {
            // Parse input, example: Availability(H1, 20240901, SGL) or Availability(H1, 20240901-20240903, DBL)
            var parts = input.Trim(')').Split(new char[] { '(', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                Console.WriteLine("Invalid Availability command format.");
                return;
            }

            var hotelId = parts[1];
            var dateRange = parts[2];
            var roomType = parts[3];

            DateTime startDate, endDate;

            if (dateRange.Contains("-"))
            {
                var dates = dateRange.Split('-');
                startDate = DateTime.ParseExact(dates[0], "yyyyMMdd", null);
                endDate = DateTime.ParseExact(dates[1], "yyyyMMdd", null);
            }
            else
            {
                startDate = DateTime.ParseExact(dateRange, "yyyyMMdd", null);
                endDate = startDate;
            }

            var availability = CalculateAvailability(hotelId, startDate, endDate, roomType);
            Console.WriteLine($"Availability: {availability}");
        }

        public static void HandleSearch(string input)
        {
            // Parse input, example: Search(H1, 365, SGL)
            var parts = input.Trim(')').Split(new char[] { '(', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                Console.WriteLine("Invalid Search command format.");
                return;
            }

            var hotelId = parts[1];
            var daysAhead = int.Parse(parts[2]);
            var roomType = parts[3];

            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(daysAhead);

            var availabilityRanges = FindAvailabilityRanges(hotelId, startDate, endDate, roomType);
            if (availabilityRanges.Any())
            {
                Console.WriteLine(string.Join(", ", availabilityRanges));
            }
            else
            {
                Console.WriteLine("No availability.");
            }
        }

        public static int CalculateAvailability(string hotelId, DateTime startDate, DateTime endDate, string roomType, List<Hotel>? hotels = null, List<Booking>? bookings = null)
        {
            if (hotels != null)
            {
                Hotels = hotels;
            }
            if (bookings != null)
            {
                Bookings = bookings;
            }

            var hotel = Hotels!.FirstOrDefault(h => h.Id == hotelId);
            if (hotel == null) return 0;

            var totalRooms = hotel.Rooms.Count(r => r.RoomType == roomType);

            var totalRoomsAvailable = totalRooms;

            // Loop through each booking for the given hotel and room type
            foreach (var booking in Bookings!.Where(b => b.HotelId == hotelId && b.RoomType == roomType))
            {
                var bookingStart = DateTime.ParseExact(booking.Arrival, "yyyyMMdd", null);
                var bookingEnd = DateTime.ParseExact(booking.Departure, "yyyyMMdd", null);

                // Check if the booking overlaps with the requested date range
                if (bookingEnd >= startDate && bookingStart <= endDate)
                {
                    // Decrement by 1 for each overlapping booking
                    totalRoomsAvailable -= 1;
                }
            }

            return totalRoomsAvailable;
        }

        public static List<string> FindAvailabilityRanges(string hotelId, DateTime startDate, DateTime endDate, string roomType, List<Hotel>? hotels = null, List<Booking>? bookings = null)
        {
            if (hotels != null)
            {
                Hotels = hotels;
            }
            if (bookings != null)
            {
                Bookings = bookings;
            }

            var hotel = Hotels!.FirstOrDefault(h => h.Id == hotelId);
            if (hotel == null) return new List<string>();

            var totalRooms = hotel.Rooms.Count(r => r.RoomType == roomType);
            var availabilityRanges = new List<string>();

            var currentStart = startDate;
            var availableRooms = totalRooms;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dailyAvailability = CalculateAvailability(hotelId, date, date, roomType);

                if (dailyAvailability > 0)
                {
                    if (currentStart == date)
                        availableRooms = dailyAvailability;
                    else if (availableRooms != dailyAvailability)
                    {
                        availabilityRanges.Add($"({currentStart:yyyyMMdd}-{date.AddDays(-1):yyyyMMdd}, {availableRooms})");
                        currentStart = date;
                        availableRooms = dailyAvailability;
                    }
                }
                else if (currentStart != date)
                {
                    if (dailyAvailability > 0) // Only add range if there are rooms available
                    {
                        availabilityRanges.Add($"({currentStart:yyyyMMdd}-{date.AddDays(-1):yyyyMMdd}, {availableRooms})");
                    }
                    currentStart = date.AddDays(1);
                }
            }

            if (currentStart <= endDate && availableRooms > 0)
            {
                availabilityRanges.Add($"({currentStart:yyyyMMdd}-{endDate:yyyyMMdd}, {availableRooms})");
            }

            return availabilityRanges;
        }
    }
}
