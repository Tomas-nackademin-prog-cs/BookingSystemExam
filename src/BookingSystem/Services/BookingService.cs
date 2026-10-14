using BookingSystem.Models;
using BookingSystem.Repositories;

namespace BookingSystem.Services;

public class BookingService(IBookingRepository repository)
{
    private static readonly IReadOnlyDictionary<string, Room> Rooms =
        new Dictionary<string, Room>(StringComparer.OrdinalIgnoreCase)
        {
            ["Aspen"] = new("Aspen", 4),
            ["Birken"] = new("Birken", 8),
            ["Eken"] = new("Eken", 12)
        };

    public IReadOnlyList<Booking> GetActiveBookings()
    {
        /* 
            This method has not been completed.
            ---------------------------------------------------
            Lägg till att sortera listan först efter datum,
            sedan efter starttid. Sortera för om bokningar är avbokade eller ej.
            "There are currently no bookings."
            Lägg till kontroll om det finns data i repository?
            Finns redan kontroll i BookingConsole
        */

        var sortedtBookings = repository.GetAll()
            .Where(booking => !booking.IsCancelled)
            .OrderBy(booking => booking.Date)
            .ThenBy(booking => booking.StartTime)
            .ToList();

        return sortedtBookings;
    }

    public IReadOnlyList<Booking> Search(string searchTerm)
    {
        /*
            Fel #3 Sökning är skriftkänsligt och inte fullständig,
            Lägg till ..., StringComparison.OrdinalIgnoreCase
            
        */

        return repository.GetAll()
            .Where(booking =>
                !booking.IsCancelled &&
                booking.CustomerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public BookingResult Create(Booking booking)
    {
        var validationResult = Validate(booking);

        if (!validationResult.IsSuccessful)
        {
            return validationResult;
        }

        booking.Id = repository.GetAll().Select(item => item.Id).DefaultIfEmpty(1000).Max() + 1;

        //Behövs inte egentligen då standardvärde är false, men som extra säkerhet.
        booking.IsCancelled = false;
        repository.Add(booking);
        repository.Save();

        return BookingResult.Success($"Booking {booking.Id} was created.");
    }

    public BookingResult Cancel(int bookingId)
    {
        //Lägg till funktion Cancel

        var allBookings = repository.GetAll();

        var booking = allBookings.FirstOrDefault(b => b.Id == bookingId);

        if (booking is null)
            return BookingResult.Failure($"Booking with id '{bookingId}' was not found.");

        if (booking.IsCancelled)
            return BookingResult.Failure($"Booking with id '{bookingId}' is already canceled.");

        booking.IsCancelled = true;

        repository.Save();

        return BookingResult.Success($"Booking with id '{bookingId}' was canceled succesfully!");
            
    }

    private BookingResult Validate(Booking booking)
    {
        
        if (string.IsNullOrWhiteSpace(booking.CustomerName))
        {
            return BookingResult.Failure("Customer name is required.");
        }

        if (!Rooms.TryGetValue(booking.RoomName, out var room))
        {
            return BookingResult.Failure("The selected room does not exist.");
        }
        /*
            Problem #1 Felaktig Tidskontroll, kontrollerar bara att start tiden och sluttiden inte är detsamma
            Kolla vilka med vilkor som behövs lägga till med, borde kontrollera att det inte sker innan ">="
             < > || && "booking.StartTime >= booking.EndTime
        */
        if (booking.StartTime >= booking.EndTime)
        {
            return BookingResult.Failure("The start time must be earlier than the end time.");
        }

        if (booking.Participants < 1)
        {
            return BookingResult.Failure("The number of participants must be at least 1.");
        }

        if (booking.Participants > room.Capacity)
        {
            return BookingResult.Failure($"{room.Name} has a maximum capacity of {room.Capacity}.");
        }
        /*
            Problem #2 Felaktig kontroll av överlappande bokningar,
            Kontrollerar bara vissa överlappningar,
            kontrollerar bokning som kommer in är samma eller senare än den som redan finns,
            Kontrollerar bokning som kommer in är mindre än den som redan finns
            Vill att mötet ska kunna börja samma tid som en annan slutar,
            Räcker med att kontrollera nya bokning start tid < befintlig boknings slut tid?
            Räcker med att kontrollera nya bokning slut tid > befintlig boknings slut tid?
        */
        var hasOverlap = repository.GetAll().Any(existing =>
            !existing.IsCancelled &&
            existing.Date == booking.Date &&
            existing.RoomName.Equals(booking.RoomName, StringComparison.OrdinalIgnoreCase) &&
            booking.StartTime < existing.EndTime &&
            booking.EndTime > existing.StartTime);

        if (hasOverlap)
        {
            return BookingResult.Failure("The room is already booked during the selected time.");
        }

        return BookingResult.Success("The booking is valid.");
    }
}
