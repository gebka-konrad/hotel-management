using FluentAssertions;
using HotelManagement.Models;

namespace HotelManagement.Tests
{
    public class HotelAvailabilityTests
    {
        private readonly List<Hotel> _hotels;
        private readonly List<Booking> _bookings;

        public HotelAvailabilityTests()
        {
            _hotels = new List<Hotel>
            {
                new Hotel
                {
                    Id = "H1",
                    Name = "Hotel California",
                    RoomTypes = new List<RoomType>
                    {
                        new RoomType { Code = "SGL", Description = "Single Room" },
                        new RoomType { Code = "DBL", Description = "Double Room" }
                    },
                    Rooms = new List<Room>
                    {
                        new Room { RoomType = "SGL", RoomId = "101" },
                        new Room { RoomType = "SGL", RoomId = "102" },
                        new Room { RoomType = "DBL", RoomId = "201" },
                        new Room { RoomType = "DBL", RoomId = "202" }
                    }
                }
            };

            _bookings = new List<Booking>
            {
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240903", RoomType = "DBL" },
                new Booking { HotelId = "H1", Arrival = "20240902", Departure = "20240905", RoomType = "SGL" }
            };
        }

        [Fact]
        public void CalculateAvailability_ShouldReturnTotalRoomCount_WhenNoBookingsExist()
        {
            var emptyBookings = new List<Booking>();
            var result = Program.CalculateAvailability("H1", DateTime.Parse("2024-09-01"), DateTime.Parse("2024-09-02"), "SGL", _hotels, emptyBookings);

            result.Should().Be(2); // Expect both single rooms to be available
        }

        [Fact]
        public void CalculateAvailability_ShouldReturnCorrectCount_WhenBookingExists()
        {
            var result = Program.CalculateAvailability("H1", DateTime.Parse("2024-09-02"), DateTime.Parse("2024-09-03"), "SGL", _hotels, _bookings);

            result.Should().Be(1); // Expect 0 available single rooms as both are booked for the requested dates
        }

        [Fact]
        public void CalculateAvailability_ShouldReturnNegativeCount_WhenOverbooked()
        {
            var overbookedBookings = new List<Booking>
            {
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240902", RoomType = "SGL" },
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240902", RoomType = "SGL" },
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240902", RoomType = "SGL" }
            };

            var result = Program.CalculateAvailability("H1", DateTime.Parse("2024-09-01"), DateTime.Parse("2024-09-02"), "SGL", _hotels, overbookedBookings);

            result.Should().Be(-1); // Expect -1 due to overbooking
        }

        [Fact]
        public void Search_ShouldReturnAvailabilityRanges_WhenAvailable()
        {
            var result = Program.FindAvailabilityRanges("H1", DateTime.Parse("2024-09-01"), DateTime.Parse("2024-09-10"), "SGL", _hotels, _bookings);

            result.Should().BeEquivalentTo(new List<string> { "(20240901-20240901, 2)", "(20240902-20240905, 1)", "(20240906-20240910, 2)" });
        }

        [Fact]
        public void Search_ShouldReturnMultipleRanges_WhenAvailabilityVaries()
        {
            var variedBookings = new List<Booking>
            {
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240903", RoomType = "SGL" },
                new Booking { HotelId = "H1", Arrival = "20240905", Departure = "20240906", RoomType = "SGL" }
            };
            var result = Program.FindAvailabilityRanges("H1", DateTime.Parse("2024-09-01"), DateTime.Parse("2024-09-10"), "SGL", _hotels, variedBookings);

            result.Should().BeEquivalentTo(new List<string> { "(20240901-20240903, 1)", "(20240904-20240904, 2)", "(20240905-20240906, 1)", "(20240907-20240910, 2)" });
        }

        [Fact]
        public void Search_ShouldReturnEmptyList_WhenNoAvailability()
        {
            var fullyBooked = new List<Booking>
            {
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240910", RoomType = "SGL" },
                new Booking { HotelId = "H1", Arrival = "20240901", Departure = "20240915", RoomType = "SGL" }
            };
            var result = Program.FindAvailabilityRanges("H1", DateTime.Parse("2024-09-01"), DateTime.Parse("2024-09-10"), "SGL", _hotels, fullyBooked);

            result.Should().BeEmpty(); // Expect no availability in the entire range
        }
    }
}