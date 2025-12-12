using System.Text;
using System.Text.Json;

namespace CinemaBookingSystem
{
    // ---------- PAYMENT STATUS ----------
    internal enum PaymentStatus
    {
        Pending,
        Paid,
        Cancelled,
        Expired,
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Program.DrawTitle("CINEMA BOOKING SYSTEM");
            Console.WriteLine("");
            bool visible = true;

            while (!Console.KeyAvailable) // keep blinking until user presses a key
            {
                if (visible)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(" Press any key to enter!");
                }
                else
                {
                    Console.Write("                        "); // overwrite with spaces
                }

                Console.SetCursorPosition(0, Console.CursorTop); // move cursor back
                visible = !visible;
                Thread.Sleep(500);
            }

            Console.ReadKey(true);
            Console.ResetColor();
            Console.Clear();
            Program.ShowLoadingAnimation("Loading Cinema Booking System..", 2000);


            Thread.Sleep(1000);

            Console.Clear();

            // --- Initialize Movies and Schedules ---
            List<Movie> movies = [new("Heneral Luna"), new("Conjuring V"), new("Encanto")];

            movies[0]
                .Schedules.AddRange(
                    [new("12:30 PM", 250), new("4:00 PM", 260), new("7:30 PM", 270)]
                );

            movies[1].Schedules.AddRange([new("3:00 AM", 300), new("3:00 PM", 350)]);

            movies[2].Schedules.AddRange([new("1:00 PM", 200), new("4:00 PM", 210)]);

            // --- Load persistent bookings from JSON ---
            List<Booking> allBookings = DataManager.LoadBookings();

            // --- Initialize Inventory (creates inventory.json if missing) ---
            List<FoodInventory> inventory = InventoryManager.LoadInventory();

            // Initialize Seat Managers per movie schedule (movie + schedule)
            Dictionary<string, SeatManager> seatManagers = [];
            foreach (Movie m in movies)
            {
                foreach (Schedule sched in m.Schedules)
                {
                    string key = m.Movies + "|" + sched.Time;
                    if (!seatManagers.ContainsKey(key))
                    {
                        seatManagers[key] = new SeatManager(5, 8);
                    }
                }
            }

            // Mark previously booked seats as unavailable per movie schedule
            foreach (Booking booking in allBookings)
            {
                if (booking == null)
                    continue;
                string movieKey = booking.Movie ?? string.Empty;
                string scheduleKey = booking.Schedule ?? string.Empty;
                string key = movieKey + "|" + scheduleKey;
                if (seatManagers.TryGetValue(key, out SeatManager? value))
                {
                    value.MarkSeatsAsUnavailable(booking.Seats ?? []);
                }
            }

            // --- Auto-expire any pending bookings past deadline ---
            bool expiredAny = false;
            foreach (Booking? booking in allBookings.ToList())
            {
                if (
                    booking != null
                    && booking.PaymentStatus == PaymentStatus.Pending
                    && booking.PaymentDeadline != default
                    && booking.PaymentDeadline < DateTime.Now
                )
                {
                    booking.PaymentStatus = PaymentStatus.Expired;
                    string key = (booking.Movie ?? "") + "|" + (booking.Schedule ?? "");
                    if (seatManagers.TryGetValue(key, out SeatManager? value))
                    {
                        value.ReleaseSeats(booking.Seats);
                    }
                    expiredAny = true;
                }
            }
            if (expiredAny)
                DataManager.SaveBookings(allBookings);

            string mainChoice;
            do
            {
                Console.Clear();
                Console.WriteLine();
                // Create menu box
                const int menuWidth = 40;
                Console.WriteLine("╔" + new string('═', menuWidth - 2) + "╗");
                Console.WriteLine("║" + Center("MAIN MENU", menuWidth - 2) + "║");
                Console.WriteLine("╠" + new string('═', menuWidth - 2) + "╣");
                Console.WriteLine("║  [1] New Reservation" + new string(' ', menuWidth - 23) + "║");
                Console.WriteLine(
                    "║  [2] Edit Existing Reservation" + new string(' ', menuWidth - 33) + "║"
                );
                Console.WriteLine(
                    "║  [3] Complete Pending Payment" + new string(' ', menuWidth - 32) + "║"
                );
                Console.WriteLine("║  [4] Exit" + new string(' ', menuWidth - 12) + "║");
                Console.WriteLine("╚" + new string('═', menuWidth - 2) + "╝");
                Console.WriteLine();
                Console.Write("Select an option: ");
                mainChoice = InputValidator.ReadNonEmptyLine();

                switch (mainChoice)
                {
                    case "1":
                        CreateNewReservation(movies, allBookings, seatManagers);
                        break;

                    case "2":
                        EditReservation(movies, allBookings, seatManagers);
                        break;

                    case "3":
                        CompletePendingPayment(allBookings, seatManagers);
                        break;

                    case "4":
                        Console.Write("\nAre you sure you want to exit? (Y/N): ");
                        string exitConfirm = InputValidator.ReadNonEmptyLine().ToUpper();
                        if (exitConfirm == "Y")
                            break;
                        mainChoice = "";
                        break;

                    case "admin":
                        AdminLogin(movies, allBookings, seatManagers);
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option. Please try again.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(1000);
                        break;
                }
            } while (mainChoice != "4");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nThank you for using the Cinema Booking System!\n");
            Console.ResetColor();
        }

        static void CreateNewReservation(
            List<Movie> movies,
            List<Booking> allBookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            Console.Clear();
            Movie selectedMovie = Movie.SelectMovie(movies);
            Schedule selectedSchedule = Schedule.SelectSchedule(selectedMovie);
            string selectedKey = selectedMovie.Movies + "|" + selectedSchedule.Time;
            if (!seatManagers.TryGetValue(selectedKey, out SeatManager? seatManagerForShow))
            {
                seatManagerForShow = new SeatManager(5, 8);
                seatManagers[selectedKey] = seatManagerForShow;
            }

            Booking booking = Booking.CreateBooking(
                selectedMovie,
                selectedSchedule,
                seatManagerForShow
            );
            booking.DisplaySummary();

            // Prompt for passkey (masked) and confirmation before saving/continuing
            while (true)
            {
                string pass = InputValidator.ReadMasked("\nCreate your passkey: ");
                if (string.IsNullOrWhiteSpace(pass))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Passkey cannot be empty.");
                    Console.ResetColor();
                    continue;
                }
                string confirm = InputValidator.ReadMasked("Confirm passkey: ");
                if (pass != confirm)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Passkeys do not match. Please try again.");
                    Console.ResetColor();
                    continue;
                }
                booking.Passkey = pass;
                break;
            }

            // Ask to Cancel or Confirm reservation
            string action;
            while (true)
            {
                Console.Write("\nCancel [X] | Confirm [Y]: ");
                action = InputValidator.ReadNonEmptyLine().ToUpper();
                if (action == "X" || action == "Y")
                    break;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please enter X to cancel or Y to confirm.");
                Console.ResetColor();
            }

            if (action == "Y")
            {
                // Initialize payment status and deadline then save
                booking.PaymentStatus = PaymentStatus.Pending;
                booking.PaymentDeadline = DateTime.Now.AddMinutes(15);
                allBookings.Add(booking);
                DataManager.SaveBookings(allBookings);

                // Process payment immediately
                FoodOrder.PaymentResult payment = PaymentProcessor.ProcessPayment(booking);
                if (payment != null && payment.Success)
                {
                    bool wasPending = booking.PaymentStatus != PaymentStatus.Paid;
                    booking.PaymentStatus = PaymentStatus.Paid;
                    booking.PaymentMethod = payment.Method;
                    booking.PaymentReference = payment.Reference;
                    booking.AmountPaid = payment.AmountPaid;
                    booking.LastModified = DateTime.Now;
                    // Record sale if payment was pending and has food items
                    bool hasFoodItems = booking.FoodItems != null && booking.FoodItems.Any(kv => kv.Value > 0);
                    if (wasPending && hasFoodItems)
                    {
                        InventoryManager.RecordSale(booking.FoodItems);
                    }
                    DataManager.SaveBookings(allBookings);
                    PaymentProcessor.DisplayReceipt(booking, payment);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nPayment cancelled. Reservation saved as PENDING.");
                    Console.ResetColor();
                    DisplayPaymentDeadline(booking.PaymentDeadline);
                }
            }
            else
            {
                // Cancel: release seats for this showtime
                seatManagerForShow.ReleaseSeats(booking.Seats);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Reservation cancelled.");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void EditReservation(
            List<Movie> movies,
            List<Booking> allBookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            Console.Clear();
            DrawTitle("EDIT RESERVATION");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nPress (x) to cancel.");
            Console.ResetColor();
            // Ask for Reservation ID
            string reservationID = InputValidator
                .ReadNonEmptyLine("Enter Reservation ID: ")
                .Trim()
                .ToUpper();

            if (reservationID == "X")
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            // Ask for Passkey
            string passkey = InputValidator.ReadMasked("Enter Passkey: ");

            // Find booking
            Booking? booking = allBookings.FirstOrDefault<Booking?>(b =>
                b != null
                && b.ReservationID != null
                && b.ReservationID.Equals(reservationID, StringComparison.OrdinalIgnoreCase)
                && b.Passkey == passkey
            );

            if (booking == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nReservation not found or passkey incorrect.");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            // Check payment status
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== RESERVATION ALREADY PAID ===\n");
                Console.ResetColor();

                booking.DisplayBookingDetails();

                Console.WriteLine("\nThis reservation has been paid and cannot be edited.");
                Console.WriteLine("You can only cancel this reservation.");
                Console.Write("\nDo you want to cancel this booking? (Y/N): ");
                string cancelChoice = InputValidator.ReadNonEmptyLine().ToUpper();

                if (cancelChoice == "Y")
                {
                    if (CancelBooking(booking, seatManagers, allBookings))
                    {
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        return;
                    }
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }
            if (booking.PaymentStatus == PaymentStatus.Expired)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=== RESERVATION EXPIRED ===\n");
                Console.ResetColor();

                booking.DisplayBookingDetails();

                Console.WriteLine("\nThis reservation has expired and cannot be edited.");
                Console.WriteLine("Payment deadline has passed and seats have been released.");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            // Display booking details
            booking.DisplayBookingDetails();

            // Edit menu
            string editChoice;
            do
            {
                Console.WriteLine("\nEDIT RESERVATION");
                Console.WriteLine("------------------------");
                Console.WriteLine("[1] Change Movie");
                Console.WriteLine("[2] Change Schedule");
                Console.WriteLine("[3] Change Seat(s)");
                Console.WriteLine("[4] Change Ticket Quantity");
                Console.WriteLine("[5] Change Food Order");
                Console.WriteLine("[6] Save Edit");
                Console.WriteLine("[7] Cancel Booking");
                Console.WriteLine("------------------------");
                Console.Write("Select an option: ");
                editChoice = InputValidator.ReadNonEmptyLine();

                switch (editChoice)
                {
                    case "1":
                        EditMovie(booking, movies, seatManagers, allBookings);
                        booking.DisplayBookingDetails();
                        break;

                    case "2":
                        EditSchedule(booking, movies, seatManagers, allBookings);
                        booking.DisplayBookingDetails();
                        break;

                    case "3":
                        EditSeats(booking, seatManagers, allBookings);
                        booking.DisplayBookingDetails();
                        break;

                    case "4":
                        EditTicketQuantity(booking, seatManagers, allBookings);
                        booking.DisplayBookingDetails();
                        break;

                    case "5":
                        // If booking is paid, restore old stock first
                        if (
                            booking.PaymentStatus == PaymentStatus.Paid
                            && booking.FoodItems != null
                            && booking.FoodItems.Count != 0
                        )
                        {
                            InventoryManager.RestoreSale(booking.FoodItems);
                        }

                        // Customer food selection without stock visibility
                        FoodOrder foodOrder = FoodOrder.SelectFood(false, booking.FoodItems ?? new Dictionary<string, int>());
                        booking.FoodItems = foodOrder.Items;
                        booking.FoodTotal = foodOrder.TotalAmount;
                        booking.LastModified = DateTime.Now;

                        // If booking is paid, deduct new stock
                        if (
                            booking.PaymentStatus == PaymentStatus.Paid
                            && booking.FoodItems != null
                            && booking.FoodItems.Count != 0
                        )
                        {
                            InventoryManager.RecordSale(booking.FoodItems);
                        }

                        DataManager.SaveBookings(allBookings);
                        booking.DisplayBookingDetails();
                        break;

                    case "6":
                        break;

                    case "7":
                        if (CancelBooking(booking, seatManagers, allBookings))
                        {
                            return;
                        }
                        booking.DisplayBookingDetails();
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option. Please try again.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(1000);
                        break;
                }
            } while (editChoice != "6");
        }

        internal static void EditSchedule(
            Booking booking,
            List<Movie> movies,
            Dictionary<string, SeatManager> seatManagers,
            List<Booking> allBookings
        )
        {
            // Find the movie
            Movie? movie = movies.FirstOrDefault(m => m.Movies == booking.Movie);
            if (movie == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Movie not found.");
                Console.ResetColor();
                return;
            }



            // Release old seats
            string oldKey = booking.Movie + "|" + booking.Schedule;
            if (seatManagers.TryGetValue(oldKey, out SeatManager? value))
            {
                value.ReleaseSeats(booking.Seats);
            }

            // Select new schedule
            Schedule newSchedule = Schedule.SelectSchedule(movie);
            string newKey = booking.Movie + "|" + newSchedule.Time;
            if (!seatManagers.TryGetValue(newKey, out SeatManager? newSeatManager))
            {
                newSeatManager = new SeatManager(5, 8);
                seatManagers[newKey] = newSeatManager;
            }

            if (newSchedule.Time == booking.Schedule)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nSchedule unchanged.");
                Console.ResetColor();
                Thread.Sleep(1200);
                return;
            }

            foreach (Booking b in allBookings)
            {
                if (
                    b != null
                    && b != booking
                    && b.Movie == booking.Movie
                    && b.Schedule == newSchedule.Time
                )
                {
                    newSeatManager.MarkSeatsAsUnavailable(b.Seats ?? []);
                }
            }

            // Update booking
            booking.Schedule = newSchedule.Time;
            booking.Price = newSchedule.Price;
            booking.LastModified = DateTime.Now;

            // Re-select seats for new schedule (pass empty list since it's a new schedule)
            List<string> newSeats = newSeatManager.SelectSeatsForEdit([], booking.Quantity);
            booking.Seats = newSeats;

            DataManager.SaveBookings(allBookings);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nSchedule updated successfully!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(1500);
        }

        internal static void EditMovie(
            Booking booking,
            List<Movie> movies,
            Dictionary<string, SeatManager> seatManagers,
            List<Booking> allBookings
        )
        {
            // Release seats from old showtime
            string oldKey = booking.Movie + "|" + booking.Schedule;
            if (seatManagers.TryGetValue(oldKey, out SeatManager? value))
            {
                value.ReleaseSeats(booking.Seats);
            }

            // Select new movie and schedule
            Movie newMovie = Movie.SelectMovie(movies);
            Schedule newSchedule = Schedule.SelectSchedule(newMovie);
            string newKey = newMovie.Movies + "|" + newSchedule.Time;
            if (!seatManagers.TryGetValue(newKey, out SeatManager? newSeatManager))
            {
                newSeatManager = new SeatManager(5, 8);
                seatManagers[newKey] = newSeatManager;
            }

            // If nothing changed, early out
            if (newMovie.Movies == booking.Movie && newSchedule.Time == booking.Schedule)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nMovie and schedule unchanged.");
                Console.ResetColor();
                Thread.Sleep(1200);
                return;
            }

            foreach (Booking b in allBookings)
            {
                if (
                    b != null
                    && b != booking
                    && b.Movie == newMovie.Movies
                    && b.Schedule == newSchedule.Time
                )
                {
                    newSeatManager.MarkSeatsAsUnavailable(b.Seats ?? []);
                }
            }

            // Update booking (keep ReservationID and ReservationTime)
            booking.Movie = newMovie.Movies;
            booking.Schedule = newSchedule.Time;
            booking.Price = newSchedule.Price;
            booking.LastModified = DateTime.Now;

            // Reselect seats for the new showtime for the same quantity
            List<string> newSeats = newSeatManager.SelectSeatsForEdit([], booking.Quantity);
            booking.Seats = newSeats;

            // Persist changes
            DataManager.SaveBookings(allBookings);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nMovie updated successfully!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(1500);
        }

        internal static void EditSeats(
            Booking booking,
            Dictionary<string, SeatManager> seatManagers,
            List<Booking> allBookings
        )
        {
            string key = booking.Movie + "|" + booking.Schedule;
            if (!seatManagers.TryGetValue(key, out SeatManager? seatManager))
            {
                seatManager = new SeatManager(5, 8);
                seatManagers[key] = seatManager;
            }

            // Mark all other bookings' seats as unavailable
            foreach (Booking b in allBookings)
            {
                if (
                    b != null
                    && b != booking
                    && b.Movie == booking.Movie
                    && b.Schedule == booking.Schedule
                )
                {
                    seatManager.MarkSeatsAsUnavailable(b.Seats ?? []);
                }
            }

            // Release old seats
            seatManager.ReleaseSeats(booking.Seats);

            // Select new seats (pass copy of current seats to show in blue)
            List<string> currentSeatsCopy = [.. booking.Seats];
            List<string> newSeats = seatManager.SelectSeatsForEdit(
                currentSeatsCopy,
                booking.Quantity
            );
            booking.Seats = newSeats;
            booking.LastModified = DateTime.Now;

            DataManager.SaveBookings(allBookings);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nSeats updated successfully!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(1500);
        }

        internal static void EditTicketQuantity(
            Booking booking,
            Dictionary<string, SeatManager> seatManagers,
            List<Booking> allBookings
        )
        {
            string key = booking.Movie + "|" + booking.Schedule;
            if (!seatManagers.TryGetValue(key, out SeatManager? seatManager))
            {
                seatManager = new SeatManager(5, 8);
                seatManagers[key] = seatManager;
            }

            // Mark all other bookings' seats as unavailable
            foreach (Booking b in allBookings)
            {
                if (
                    b != null
                    && b != booking
                    && b.Movie == booking.Movie
                    && b.Schedule == booking.Schedule
                )
                {
                    seatManager.MarkSeatsAsUnavailable(b.Seats ?? []);
                }
            }

            Console.Clear();
            Console.WriteLine("Change Ticket Quantity\n");
            Console.WriteLine($"Current ticket quantity: {booking.Quantity}");
            Console.WriteLine(
                $"Selected seats: {string.Join("", booking.Seats.Select(s => $"[{s}]"))}"
            );
            Console.WriteLine();

            // Interactive quantity selector (arrow keys)
            int newQuantity = booking.Quantity;
            ConsoleKey keyPress;
            int selectorTop = Console.CursorTop;
            do
            {
                DisplayQuantitySelector(selectorTop, booking.Price, newQuantity);
                keyPress = Console.ReadKey(true).Key;

                switch (keyPress)
                {
                    case ConsoleKey.RightArrow when newQuantity < 5:
                        newQuantity++;
                        break;
                    case ConsoleKey.LeftArrow when newQuantity > 1:
                        newQuantity--;
                        break;
                }
            } while (keyPress != ConsoleKey.Enter);

            if (newQuantity == booking.Quantity)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nTicket quantity unchanged.");
                Console.ResetColor();
                Thread.Sleep(1200);
                return;
            }

            int difference = newQuantity - booking.Quantity;
            if (difference > 0)
            {
                // Need to add seats
                Console.Clear();
                Console.WriteLine(
                    $"You currently have {booking.Seats.Count} seats selected. Please select {difference} additional seat(s)."
                );
                List<string> additionalSeats = seatManager.SelectAdditionalSeats(difference);
                booking.Seats.AddRange(additionalSeats);
            }
            else
            {
                // Need to remove seats
                int toRemove = -difference;
                Console.Clear();
                Console.WriteLine(
                    $"You currently have {booking.Seats.Count} seats selected. Please deselect {toRemove} seat(s)."
                );
                Console.WriteLine(
                    $"Selected seats: {string.Join(" ", booking.Seats.Select(s => $"[{s}]"))}"
                );

                int removed = 0;
                while (removed < toRemove)
                {
                    string seatToRemove = InputValidator
                        .ReadNonEmptyLine($"Enter seat to remove ({removed + 1}/{toRemove}): ")
                        .ToUpper();
                    if (!booking.Seats.Contains(seatToRemove))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Seat is not in your current selection. Try again.");
                        Console.ResetColor();
                        continue;
                    }
                    booking.Seats.Remove(seatToRemove);
                    // Release seat
                    seatManager.ReleaseSeats([seatToRemove]);
                    removed++;
                }
            }

            // Update quantity and timestamps
            booking.Quantity = newQuantity;
            booking.LastModified = DateTime.Now;

            // Persist
            DataManager.SaveBookings(allBookings);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nTicket quantity updated successfully!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(1500);
        }

        internal static void EditFood(Booking booking, List<Booking> allBookings)
        {
            FoodOrder foodOrder = FoodOrder.SelectFood(false, booking.FoodItems);
            booking.FoodItems = foodOrder.Items;
            booking.FoodTotal = foodOrder.TotalAmount;
            booking.LastModified = DateTime.Now;

            DataManager.SaveBookings(allBookings);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nFood order updated successfully!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(1500);
        }

        static void CompletePendingPayment(
            List<Booking> allBookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            Console.Clear();
            DrawTitle("COMPLETE PENDING PAYMENT");
            Console.WriteLine();

            List<Booking> pending =
            [
                .. allBookings.Where(b => b != null && b.PaymentStatus == PaymentStatus.Pending),
            ];
            if (pending.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No pending bookings found.");
                Console.ResetColor();
                Thread.Sleep(1500);
                return;
            }

            foreach (Booking? b in pending)
            {
                Console.WriteLine(
                    $"ID: {b.ReservationID} | {b.Movie} - {b.Schedule} | Total: ₱{b.GetGrandTotal():F2}"
                );
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nPress (x) to cancel.");
            Console.ResetColor();

            string reservationID = InputValidator
                .ReadNonEmptyLine("\nEnter Reservation ID: ")
                .Trim()
                .ToUpper();

            if (reservationID == "X")
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            string passkey = InputValidator.ReadMasked("Enter Passkey: ");

            Booking? booking = allBookings.FirstOrDefault(b =>
                b != null
                && b.ReservationID != null
                && b.ReservationID.Equals(reservationID, StringComparison.OrdinalIgnoreCase)
                && b.Passkey == passkey
                && b.PaymentStatus == PaymentStatus.Pending
            );

            if (booking == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nPending reservation not found or passkey incorrect.");
                Console.ResetColor();
                Thread.Sleep(1500);
                return;
            }

            if (booking.PaymentDeadline != default && booking.PaymentDeadline < DateTime.Now)
            {
                booking.PaymentStatus = PaymentStatus.Expired;
                string key = booking.Movie + "|" + booking.Schedule;
                if (seatManagers.TryGetValue(key, out SeatManager? value))
                {
                    value.ReleaseSeats(booking.Seats);
                }
                DataManager.SaveBookings(allBookings);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    "\nPayment deadline expired. Booking marked as EXPIRED and seats released."
                );
                Console.ResetColor();
                Thread.Sleep(2000);
                return;
            }

            DisplayPaymentDeadline(booking.PaymentDeadline);
            FoodOrder.PaymentResult payment = PaymentProcessor.ProcessPayment(booking);
            if (payment != null && payment.Success)
            {
                bool wasPending = booking.PaymentStatus != PaymentStatus.Paid;
                booking.PaymentStatus = PaymentStatus.Paid;
                booking.PaymentMethod = payment.Method;
                booking.PaymentReference = payment.Reference;
                booking.AmountPaid = payment.AmountPaid;
                booking.LastModified = DateTime.Now;
                if (
                    wasPending
                    && booking.FoodItems != null
                    && booking.FoodItems.Any(kv => kv.Value > 0)
                )
                {
                    InventoryManager.RecordSale(booking.FoodItems);
                }
                DataManager.SaveBookings(allBookings);
                PaymentProcessor.DisplayReceipt(booking, payment);
                Console.WriteLine("\nPress any key to return to the menu...");
                Console.ReadKey();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nPayment cancelled. Booking remains PENDING.");
                Console.ResetColor();
                DisplayPaymentDeadline(booking.PaymentDeadline);
            }
        }

        internal static void DisplayPaymentDeadline(DateTime deadline)
        {
            if (deadline == default)
            {
                return;
            }
            TimeSpan remaining = deadline - DateTime.Now;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;
            string timeText = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";

            if (remaining.TotalMinutes < 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                PlayWarningBeep();
            }
            else if (remaining.TotalMinutes < 5)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                PlayWarningBeep();
            }
            else
            {
                Console.ResetColor();
            }
            Console.WriteLine($"Payment due in: {timeText} remaining");
            Console.ResetColor();
        }

        static bool CancelBooking(
            Booking booking,
            Dictionary<string, SeatManager> seatManagers,
            List<Booking> allBookings
        )
        {
            Console.Write("\nAre you sure you want to cancel this booking? (Y/N): ");
            string confirm = InputValidator.ReadNonEmptyLine().ToUpper();
            if (confirm == "Y")
            {
                // Release seats
                string key = booking.Movie + "|" + booking.Schedule;
                if (seatManagers.TryGetValue(key, out SeatManager? value))
                {
                    value.ReleaseSeats(booking.Seats);
                }

                // If booking was PAID, restore food stock and show refund message
                if (booking.PaymentStatus == PaymentStatus.Paid)
                {
                    if (booking.FoodItems != null && booking.FoodItems.Count != 0)
                    {
                        InventoryManager.RestoreSale(booking.FoodItems);
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n💰 REFUND NOTICE 💰");
                    Console.WriteLine($"Refund Amount: ₱{booking.GetGrandTotal():F2}");
                    Console.WriteLine($"Payment Method: {booking.PaymentMethod}");
                    Console.WriteLine($"Reference: {booking.PaymentReference}");
                    Console.WriteLine("Please process refund manually.");
                    Console.ResetColor();
                    Thread.Sleep(2000);

                    AdminLogger.Log(
                        $"Booking {booking.ReservationID} cancelled - Refund: ₱{booking.GetGrandTotal():F2}"
                    );
                }

                // Remove from list
                allBookings.Remove(booking);
                DataManager.SaveBookings(allBookings);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nBooking cancelled successfully!");
                Console.ResetColor();
                Program.PlaySuccessBeep();
                Thread.Sleep(1500);
                return true;
            }
            return false;
        }

        // ---------- ADMIN AUTH (delegates to class) ----------
        static void AdminLogin(
            List<Movie> movies,
            List<Booking> allBookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            AdminConsoleUI adminUI = new(movies, allBookings, seatManagers);
            adminUI.RunLogin();
        }

        // ---------- UI HELPER METHODS ----------
        internal static void DrawTitle(string title)
        {
            int width = title.Length + 6;
            string border = new('═', width);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔" + border + "╗");
            Console.WriteLine("║  " + title + "    ║");
            Console.WriteLine("╚" + border + "╝");
            Console.ResetColor();
        }

        private static void DisplayQuantitySelector(int topPosition, double price, int quantity)
        {
            Console.SetCursorPosition(0, topPosition);
            Console.WriteLine("----------------------------------          ");
            Console.WriteLine($"Ticket Price: ₱{price}               ");
            Console.WriteLine($"Ticket Quantity: {quantity}               ");
            Console.WriteLine($"Total Amount: ₱{price * quantity:F2}               ");
            Console.WriteLine();
            Console.WriteLine("Use arrow key <- / -> to adjust quantity. Press ENTER to confirm.     ");
        }

        internal static string Center(string text, int width)
        {
            if (text.Length >= width)
                return text;
            int totalPadding = width - text.Length;
            int leftPadding = totalPadding / 2;
            int rightPadding = totalPadding - leftPadding;
            return new string(' ', leftPadding) + text + new string(' ', rightPadding);
        }

        internal static void ShowLoadingAnimation(string message, int durationMs)
        {
            string[] spinner = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
            int elapsed = 0;
            int spinnerIndex = 0;

            Console.CursorVisible = false;
            int cursorTop = Console.CursorTop;

            while (elapsed < durationMs)
            {
                Console.SetCursorPosition(0, cursorTop);
                Console.Write($"{spinner[spinnerIndex]} {message}...");
                spinnerIndex = (spinnerIndex + 1) % spinner.Length;
                Thread.Sleep(100);
                elapsed += 100;
            }

            Console.SetCursorPosition(0, cursorTop);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, cursorTop);
            Console.CursorVisible = true;
        }

        // Beep sound helpers
        internal static void PlaySuccessBeep()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            Console.Beep(700, 200);
            Console.Beep(900, 200);
#pragma warning restore CA1416 // Validate platform compatibility
        }

        internal static void PlayErrorBeep()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            Console.Beep(300, 500);
#pragma warning restore CA1416 // Validate platform compatibility
        }

        internal static void PlayWarningBeep()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            Console.Beep(500, 300);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }

    // ---------- ADMIN CONSOLE UI ----------
    internal class AdminConsoleUI
    {
        private readonly List<Movie> movies;
        private readonly List<Booking> allBookings;
        private readonly Dictionary<string, SeatManager> seatManagers;

        public AdminConsoleUI(
            List<Movie> movies,
            List<Booking> allBookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            this.movies = movies;
            this.allBookings = allBookings;
            this.seatManagers = seatManagers;
        }

        public void RunLogin()
        {
            int attempts = 0;
            while (attempts < 3)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Program.DrawTitle("ADMIN LOGIN");
                Console.ResetColor();

                Console.Write("Username: ");
                string username = Console.ReadLine()?.Trim() ?? string.Empty;
                string password = InputValidator.ReadMasked("Password: ");

                if (
                    string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase)
                    && password == "admin123"
                )
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nAccess Granted.");
                    Console.ResetColor();
                    Thread.Sleep(800);
                    RunMenu();
                    return;
                }

                attempts++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nAccess Denied.");
                Console.ResetColor();
                Thread.Sleep(800);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nMax attempts reached. Returning to main menu...");
            Console.ResetColor();
            Thread.Sleep(1200);
        }

        public void RunMenu()
        {
            string? choice;
            do
            {
                Console.Clear();
                DrawHeader();
                Console.Write("\nSelect an option: ");
                choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        SalesReportManager.ViewSalesReports(allBookings);
                        break;

                    case "2":
                        InventoryConsoleUI.ManageInventory();
                        break;

                    case "3":
                        AdminReservationManager.ManageReservations(
                            movies,
                            allBookings,
                            seatManagers
                        );
                        break;

                    case "4":
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid choice.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(800);
                        break;
                }
            } while (choice != "4");
        }
        private static void WriteCentered(int width, string content)
        {
            string line = "║" + Center(content, width - 2) + "║";
            Console.WriteLine(line);
        }

        private static void WriteMenuLine(int width, string content)
        {
            if (content.Length > width - 4)
                content = content[..(width - 4)];
            string line = $"║ {content}{new string(' ', width - 3 - content.Length)}║";
            Console.WriteLine(line);
        }

        private static string Center(string s, int width)
        {
            if (s.Length >= width)
                return s[..width];
            int pad = width - s.Length;
            int left = pad / 2;
            int right = pad - left;
            return new string(' ', left) + s + new string(' ', right);
        }

        private void DrawHeader()
        {
            

            const int width = 50;
            string top = "╔" + new string('═', width - 2) + "╗";
            string mid = "╠" + new string('═', width - 2) + "╣";
            string bot = "╚" + new string('═', width - 2) + "╝";
            Console.WriteLine(top);
            WriteCentered(width, "ADMIN DASHBOARD");
            Console.WriteLine(mid);

            DashboardSummary.Summary summary = DashboardSummary.Calculate(allBookings);

            // Format each line to exactly width-4 characters (accounting for "║ " and " ║")
            string line1 = $"Today's Sales: ₱{summary.TodaysSales:F2}";
            string line2 = $"Pending Payments: {summary.PendingPayments}";
            string line3 = $"Low Stock Items: {summary.LowStockItems}";
            string line4 = $"Total Reservations: {summary.TotalReservations}";

            Console.WriteLine("║ " + line1.PadRight(width - 4) + " ║");
            Console.WriteLine("║ " + line2.PadRight(width - 4) + " ║");
            Console.WriteLine("║ " + line3.PadRight(width - 4) + " ║");
            Console.WriteLine("║ " + line4.PadRight(width - 4) + " ║");

            Console.WriteLine(mid);
            WriteMenuLine(width, "[1] View Sales Reports");
            WriteMenuLine(width, "[2] Manage Stock Inventory");
            WriteMenuLine(width, "[3] View Reservations");
            WriteMenuLine(width, "[4] Logout");
            Console.WriteLine(bot);
        }
    }

    // ---------- SALES REPORT MANAGER ----------
    internal static class SalesReportManager
    {
        public static void ViewSalesReports(List<Booking> bookings)
        {
            string? choice;
            do
            {
                Console.Clear();
                DrawMenuHeader("SALES REPORTS");
                DrawMenuLine("[1] Daily Report");
                DrawMenuLine("[2] Weekly Report");
                DrawMenuLine("[3] Monthly Report");
                DrawMenuLine("[4] Back");
                DrawMenuLine("[5] Export to Text File");
                DrawMenuFooter();
                Console.Write("\nSelect an option: ");
                choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        {
                            string report = GenerateDailyReport(bookings, out string filenameHint);
                            Console.WriteLine(report);
                            PromptContinue();
                            break;
                        }

                    case "2":
                        {
                            string report = GenerateWeeklyReport(bookings, out string filenameHint);
                            Console.WriteLine(report);
                            PromptContinue();
                            break;
                        }

                    case "3":
                        {
                            string report = GenerateMonthlyReport(bookings, out string filenameHint);
                            Console.WriteLine(report);
                            PromptContinue();
                            break;
                        }

                    case "4":
                        break;

                    case "5":
                        ExportReportMenu(bookings);
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid selection.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(800);
                        break;
                }
            } while (choice != "4");
        }

        private static void ExportReportMenu(List<Booking> bookings)
        {
            Console.WriteLine("\nSelect report to export:");
            Console.WriteLine("[1] Daily");
            Console.WriteLine("[2] Weekly");
            Console.WriteLine("[3] Monthly");
            Console.WriteLine("[4] Cancel");
            Console.Write("Choice: ");
            string? choice = Console.ReadLine()?.Trim();

            string report;
            string filenameHint;

            switch (choice)
            {
                case "1":
                    report = GenerateDailyReport(bookings, out filenameHint);
                    break;

                case "2":
                    report = GenerateWeeklyReport(bookings, out filenameHint);
                    break;

                case "3":
                    report = GenerateMonthlyReport(bookings, out filenameHint);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid selection.");
                    Console.ResetColor();
                    Program.PlayErrorBeep();
                    Thread.Sleep(800);
                    return;
            }

            if (report == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nNo report to export.");
                Console.ResetColor();
                Thread.Sleep(1000);
                return;
            }

            string? safeHint = filenameHint.Replace('/', '-').Replace('\\', '-').Replace(' ', '_');
            string fileName =
                $"sales_report_{safeHint ?? DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
            try
            {
                File.WriteAllText(fileName, report);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nReport saved to {fileName}");
                Console.ResetColor();
                Program.PlaySuccessBeep();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to export report: {ex.Message}");
                Console.ResetColor();
            }
            PromptContinue();
        }

        private static string GenerateDailyReport(List<Booking> bookings, out string filenameHint)
        {
            Console.Write("\nEnter date (MM/DD/YYYY) or press Enter for today: ");
            string? input = Console.ReadLine();
            DateTime date = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(input) && !DateTime.TryParse(input, out date))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid date. Defaulting to today.");
                Console.ResetColor();
                date = DateTime.Today;
            }
            filenameHint = date.ToString("yyyyMMdd");

            List<Booking> paid =
            [
                .. bookings.Where(b =>
                    b != null
                    && b.PaymentStatus == PaymentStatus.Paid
                    && b.ReservationTime.Date == date.Date
                ),
            ];

            return BuildReport("DAILY SALES REPORT", $"Date: {date:MM/dd/yyyy}", paid, date, date);
        }

        private static string GenerateWeeklyReport(List<Booking> bookings, out string filenameHint)
        {
            Console.Write("\nEnter week start date (MM/DD/YYYY) or press Enter for current week: ");
            string? input = Console.ReadLine();
            DateTime start = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(input) && !DateTime.TryParse(input, out start))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid date. Defaulting to current week.");
                Console.ResetColor();
                start = DateTime.Today;
            }
            // Fix Monday calculation to correctly handle Sunday as end of week
            int daysFromMonday = ((int)start.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            start = start.Date.AddDays(-daysFromMonday);
            DateTime end = start.AddDays(6);
            filenameHint = $"{start:yyyyMMdd}_{end:yyyyMMdd}";

            List<Booking> paid =
            [
                .. bookings.Where(b =>
                    b != null
                    && b.PaymentStatus == PaymentStatus.Paid
                    && b.ReservationTime.Date >= start.Date
                    && b.ReservationTime.Date <= end.Date
                ),
            ];

            StringBuilder sb = new();
            string baseReport = BuildReport(
                "WEEKLY SALES REPORT",
                $"Week: {start:MM/dd/yyyy} - {end:MM/dd/yyyy}",
                paid,
                start,
                end
            );
            sb.Append(baseReport);
            sb.AppendLine();
            sb.AppendLine("Daily Sales Chart:");
            Dictionary<string, double> dailyTotals = new();
            for (int i = 0; i < 7; i++)
            {
                DateTime day = start.AddDays(i);
                double total = paid.Where(b => b.ReservationTime.Date == day.Date)
                    .Sum(b => b.Price * b.Quantity + CalculateFoodTotal(b));
                dailyTotals[day.ToString("ddd")] = total;
            }
            foreach (KeyValuePair<string, double> kvp in dailyTotals)
            {
                int barLength = (int)Math.Round(kvp.Value / 100);
                string bar = kvp.Value > 0 ? new string('█', Math.Max(1, barLength)) : "";
                sb.AppendLine($"{kvp.Key,-6} {bar} ₱{kvp.Value:F2}");
            }
            return sb.ToString();
        }

        private static string GenerateMonthlyReport(List<Booking> bookings, out string filenameHint)
        {
            Console.Write("\nEnter month/year (MM/YYYY) or press Enter for current month: ");
            string? input = Console.ReadLine();
            DateTime month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (
                !string.IsNullOrWhiteSpace(input)
                && DateTime.TryParse("01/" + input, out DateTime parsed)
            )
            {
                month = parsed;
            }
            else if (!string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid month. Defaulting to current month.");
                Console.ResetColor();
            }
            DateTime monthStart = new(month.Year, month.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);
            filenameHint = $"{monthStart:yyyyMM}";

            List<Booking> paid =
            [
                .. bookings.Where(b =>
                    b != null
                    && b.PaymentStatus == PaymentStatus.Paid
                    && b.ReservationTime.Date >= monthStart.Date
                    && b.ReservationTime.Date <= monthEnd.Date
                ),
            ];

            StringBuilder sb = new();
            sb.Append(
                BuildReport(
                    "MONTHLY SALES REPORT",
                    $"Month: {monthStart:MMMM yyyy}",
                    paid,
                    monthStart,
                    monthEnd
                )
            );

            Dictionary<string, (int Quantity, double Amount)> movieSales = AggregateMovieSales(
                paid
            );
            Dictionary<string, (int Quantity, double Amount)> foodSales = AggregateFoodSales(paid);
            KeyValuePair<string, (int Quantity, double Amount)> topMovie = movieSales
                .OrderByDescending(m => m.Value.Quantity)
                .FirstOrDefault();
            KeyValuePair<string, (int Quantity, double Amount)> topFood = foodSales
                .OrderByDescending(f => f.Value.Quantity)
                .FirstOrDefault();

            sb.AppendLine();
            sb.AppendLine(
                $"Top Selling Movie: {topMovie.Key ?? "N/A"} ({topMovie.Value.Quantity} tickets)"
            );
            sb.AppendLine(
                $"Top Selling Food Item: {topFood.Key ?? "N/A"} ({topFood.Value.Quantity} sold)"
            );
            sb.AppendLine();
            sb.AppendLine("Weekly Breakdown:");
            DateTime weekStart = monthStart;
            int weekNumber = 1;
            while (weekStart <= monthEnd)
            {
                DateTime weekEnd = weekStart.AddDays(6);
                if (weekEnd > monthEnd)
                    weekEnd = monthEnd;
                double total = paid.Where(b =>
                        b.ReservationTime.Date >= weekStart.Date
                        && b.ReservationTime.Date <= weekEnd.Date
                    )
                    .Sum(b => b.Price * b.Quantity + CalculateFoodTotal(b));
                sb.AppendLine(
                    $"Week {weekNumber}: {weekStart:MM/dd} - {weekEnd:MM/dd} | ₱{total:F2}"
                );
                weekNumber++;
                weekStart = weekEnd.AddDays(1);
            }

            return sb.ToString();
        }

        private static string BuildReport(
            string title,
            string subtitle,
            List<Booking> paidBookings,
            DateTime rangeStart, // Reserved for future date filtering
            DateTime rangeEnd // Reserved for future date filtering
        )
        {
            StringBuilder sb = new();
            const int width = 50;
            sb.AppendLine("╔" + new string('═', width - 2) + "╗");
            sb.AppendLine(BoxCenter(title, width));
            sb.AppendLine(BoxCenter(subtitle, width));
            sb.AppendLine("╠" + new string('═', width - 2) + "╣");

            Dictionary<string, (int Quantity, double Amount)> movieSales = AggregateMovieSales(
                paidBookings
            );
            Dictionary<string, (int Quantity, double Amount)> foodSales = AggregateFoodSales(
                paidBookings
            );

            sb.AppendLine(BoxLine("MOVIE SALES", width));
            sb.AppendLine("╠" + new string('═', width - 2) + "╣");
            if (movieSales.Count == 0)
            {
                sb.AppendLine(BoxLine("No ticket sales.", width));
            }
            else
            {
                foreach (KeyValuePair<string, (int Quantity, double Amount)> kvp in movieSales)
                {
                    sb.AppendLine(
                        BoxLine(
                            $"{kvp.Key,-20} {kvp.Value.Quantity,4} tickets   ₱{kvp.Value.Amount,8:F2}",
                            width
                        )
                    );
                }
            }

            int totalTickets = movieSales.Sum(m => m.Value.Quantity);
            double totalTicketSales = movieSales.Sum(m => m.Value.Amount);

            sb.AppendLine("╠" + new string('═', width - 2) + "╣");
            sb.AppendLine(BoxLine("FOOD & BEVERAGE SALES", width));
            sb.AppendLine("╠" + new string('═', width - 2) + "╣");
            if (foodSales.Count == 0)
            {
                sb.AppendLine(BoxLine("No food sales.", width));
            }
            else
            {
                foreach (KeyValuePair<string, (int Quantity, double Amount)> kvp in foodSales)
                {
                    sb.AppendLine(
                        BoxLine(
                            $"{kvp.Key,-20} {kvp.Value.Quantity,4} sold      ₱{kvp.Value.Amount,8:F2}",
                            width
                        )
                    );
                }
            }
            double totalFoodSales = foodSales.Sum(f => f.Value.Amount);

            sb.AppendLine("╠" + new string('═', width - 2) + "╣");
            double grandTotal = totalTicketSales + totalFoodSales;
            sb.AppendLine(
                BoxLine($"Total Tickets: {totalTickets}     Total: ₱{totalTicketSales:F2}", width)
            );
            sb.AppendLine(BoxLine($"Total Food: ₱{totalFoodSales:F2}", width));
            sb.AppendLine("╠" + new string('═', width - 2) + "╣");
            sb.AppendLine(BoxLine($"GRAND TOTAL: ₱{grandTotal:F2}", width));
            sb.AppendLine(BoxLine($"Total Transactions: {paidBookings.Count}", width));
            sb.AppendLine("╚" + new string('═', width - 2) + "╝");
            return sb.ToString();
        }

        private static Dictionary<string, (int Quantity, double Amount)> AggregateMovieSales(
            List<Booking> bookings
        )
        {
            Dictionary<string, (int, double)> result = new(StringComparer.OrdinalIgnoreCase);
            foreach (Booking booking in bookings)
            {
                if (string.IsNullOrWhiteSpace(booking.Movie))
                    continue;
                if (!result.TryGetValue(booking.Movie, out (int, double) entry))
                {
                    entry = (0, 0);
                }
                entry.Item1 += booking.Quantity;
                entry.Item2 += booking.Price * booking.Quantity;
                result[booking.Movie] = entry;
            }
            return result;
        }

        private static Dictionary<string, (int Quantity, double Amount)> AggregateFoodSales(
            List<Booking> bookings
        )
        {
            Dictionary<string, (int, double)> result = new(StringComparer.OrdinalIgnoreCase);
            foreach (Booking booking in bookings)
            {
                if (booking.FoodItems == null)
                    continue;
                foreach (KeyValuePair<string, int> kvp in booking.FoodItems)
                {
                    if (!result.TryGetValue(kvp.Key, out (int, double) entry))
                    {
                        entry = (0, 0);
                    }
                    entry.Item1 += kvp.Value;
                    double price = InventoryManager.GetInventoryItem(kvp.Key)?.Price ?? 0;
                    entry.Item2 += price * kvp.Value;
                    result[kvp.Key] = entry;
                }
            }
            return result;
        }

        private static double CalculateFoodTotal(Booking booking)
        {
            if (booking.FoodItems == null)
                return 0;
            double total = 0;
            foreach (KeyValuePair<string, int> kvp in booking.FoodItems)
            {
                double price = InventoryManager.GetInventoryItem(kvp.Key)?.Price ?? 0;
                total += price * kvp.Value;
            }
            return total;
        }

        private static void DrawMenuHeader(string title)
        {
            const int width = 50;
            Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            Console.WriteLine(BoxCenter(title, width));
            Console.WriteLine("╠" + new string('═', width - 2) + "╣");
        }

        private static void DrawMenuLine(string text)
        {
            const int width = 50;
            Console.WriteLine(BoxLine(text, width));
        }

        private static void DrawMenuFooter()
        {
            const int width = 50;
            Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        }

        private static string BoxLine(string text, int width)
        {
            if (text.Length > width - 4)
                text = text[..(width - 4)];
            return $"║ {text}{new string(' ', width - 3 - text.Length)}║";
        }

        private static string BoxCenter(string text, int width)
        {
            if (text.Length > width - 2)
                text = text[..(width - 2)];
            int pad = width - 2 - text.Length;
            int left = pad / 2;
            int right = pad - left;
            return $"║{new string(' ', left)}{text}{new string(' ', right)}║";
        }

        private static void PromptContinue()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    // ---------- INVENTORY CONSOLE UI ----------
    internal static class InventoryConsoleUI
    {
        public static void ManageInventory()
        {
            string? choice;
            do
            {
                Console.Clear();
                DrawMenuHeader("STOCK INVENTORY");
                DrawMenuLine("[1] View Current Stock");
                DrawMenuLine("[2] Add Stock");
                DrawMenuLine("[3] Remove Stock");
                DrawMenuLine("[4] View Low Stock Alert");
                DrawMenuLine("[5] Back");
                DrawMenuFooter();
                Console.Write("\nSelect an option: ");
                choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        ViewCurrentStock();
                        break;

                    case "2":
                        AddStock();
                        break;

                    case "3":
                        RemoveStock();
                        break;

                    case "4":
                        ViewLowStock();
                        break;

                    case "5":
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid selection.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(800);
                        break;
                }
            } while (choice != "5");
        }

        private static void ViewCurrentStock()
        {
            Console.Clear();
            List<FoodInventory> inventory = InventoryManager.LoadInventory();
            Console.WriteLine("════════════════════════════════════════════════════════════════");
            Console.WriteLine(
                $"{"Category",-10} | {"Item",-24} | {"Stock",5} | {"Sold",5} | {"Price",8}"
            );
            Console.WriteLine("════════════════════════════════════════════════════════════════");
            foreach (FoodInventory item in inventory)
            {
                if (item.Stock < item.ReorderLevel)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (item.Stock < item.ReorderLevel * 2)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ForegroundColor = ConsoleColor.Green;

                Console.WriteLine(
                    $"{item.Category,-10} | {item.Name,-24} | {item.Stock,5} | {item.TotalSold,5} | ₱{item.Price,7:F2}"
                );
                Console.ResetColor();
            }
            Console.WriteLine("════════════════════════════════════════════════════════════════");
            PromptContinue();
        }

        private static void AddStock()
        {
            Console.Clear();
            List<FoodInventory> inventory = InventoryManager.LoadInventory();
            for (int i = 0; i < inventory.Count; i++)
            {
                Console.WriteLine(
                    $"{i + 1}. {inventory[i].Name} (Current stock: {inventory[i].Stock})"
                );
            }
            Console.Write("\nEnter item number to add stock (or 0 to cancel): ");
            if (
                !int.TryParse(Console.ReadLine(), out int choice)
                || choice < 0
                || choice > inventory.Count
            )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid selection.");
                Console.ResetColor();
                Program.PlayErrorBeep();
                Thread.Sleep(800);
                return;
            }
            if (choice == 0)
                return;

            FoodInventory item = inventory[choice - 1];
            Console.Write($"Enter quantity to add for {item.Name}: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Quantity must be a positive number.");
                Console.ResetColor();
                Thread.Sleep(800);
                return;
            }

            InventoryManager.UpdateStock(item.Name, quantity, true);
            AdminLogger.Log($"Added {quantity} units to {item.Name}");
            Console.ForegroundColor = ConsoleColor.Green;
            FoodInventory? updated = InventoryManager.GetInventoryItem(item.Name);
            Console.WriteLine($"Stock updated. New stock: {updated?.Stock}");
            Console.ResetColor();
            PromptContinue();
        }

        private static void RemoveStock()
        {
            Console.Clear();
            List<FoodInventory> inventory = InventoryManager.LoadInventory();
            for (int i = 0; i < inventory.Count; i++)
            {
                Console.WriteLine(
                    $"{i + 1}. {inventory[i].Name} (Current stock: {inventory[i].Stock})"
                );
            }
            Console.Write("\nEnter item number to remove stock (or 0 to cancel): ");
            if (
                !int.TryParse(Console.ReadLine(), out int choice)
                || choice < 0
                || choice > inventory.Count
            )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid selection.");
                Console.ResetColor();
                Program.PlayErrorBeep();
                Thread.Sleep(800);
                return;
            }
            if (choice == 0)
                return;

            FoodInventory item = inventory[choice - 1];
            Console.Write($"Enter quantity to remove from {item.Name}: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Quantity must be a positive number.");
                Console.ResetColor();
                Thread.Sleep(800);
                return;
            }
            if (quantity > item.Stock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cannot remove more than current stock.");
                Console.ResetColor();
                Thread.Sleep(800);
                return;
            }
            string reason = InputValidator.ReadNonEmptyLine("Reason (expired/damaged/other): ");
            Console.Write("Confirm removal? (Y/N): ");
            string? confirm = Console.ReadLine()?.Trim().ToUpper();
            if (confirm != "Y")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Removal cancelled.");
                Console.ResetColor();
                Thread.Sleep(800);
                return;
            }

            InventoryManager.UpdateStock(item.Name, quantity, false);
            AdminLogger.Log($"Removed {quantity} units from {item.Name}. Reason: {reason}");
            Console.ForegroundColor = ConsoleColor.Green;
            FoodInventory? updated = InventoryManager.GetInventoryItem(item.Name);
            Console.WriteLine($"Stock updated. New stock: {updated?.Stock}");
            Console.ResetColor();
            PromptContinue();
        }

        private static void ViewLowStock()
        {
            Console.Clear();
            List<FoodInventory> inventory =
            [
                .. InventoryManager.LoadInventory().Where(i => i.Stock < i.ReorderLevel),
            ];
            if (inventory.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All items are above reorder levels.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("⚠️  LOW STOCK ALERT ⚠️");
                Console.ResetColor();
                Console.WriteLine("════════════════════════════════════════");
                foreach (FoodInventory? item in inventory)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        $"{item.Name,-20}: {item.Stock} units (Need {item.ReorderLevel})"
                    );
                    Console.ResetColor();
                }
                Console.WriteLine("════════════════════════════════════════");
            }
            PromptContinue();
        }

        private static void DrawMenuHeader(string title)
        {
            const int width = 50;
            Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            Console.WriteLine(BoxCenter(title, width));
            Console.WriteLine("╠" + new string('═', width - 2) + "╣");
        }

        private static void DrawMenuLine(string text)
        {
            const int width = 50;
            Console.WriteLine(BoxLine(text, width));
        }

        private static void DrawMenuFooter()
        {
            const int width = 50;
            Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        }

        private static string BoxLine(string text, int width)
        {
            if (text.Length > width - 4)
                text = text[..(width - 4)];
            return $"║ {text}{new string(' ', width - 3 - text.Length)}║";
        }

        private static string BoxCenter(string text, int width)
        {
            if (text.Length > width - 2)
                text = text[..(width - 2)];
            int pad = width - 2 - text.Length;
            int left = pad / 2;
            int right = pad - left;
            return $"║{new string(' ', left)}{text}{new string(' ', right)}║";
        }

        private static void PromptContinue()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    // ---------- ADMIN RESERVATION MANAGER ----------
    internal static class AdminReservationManager
    {
        private const int PageSize = 10;

        public static void ManageReservations(
            List<Movie> movies,
            List<Booking> bookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            string? choice;
            do
            {
                Console.Clear();
                DrawMenuHeader("RESERVATION MANAGEMENT");
                DrawMenuLine("[1] View All Bookings");
                DrawMenuLine("[2] View Pending Payments");
                DrawMenuLine("[3] View Paid Reservations");
                DrawMenuLine("[4] View Expired Reservations");
                DrawMenuLine("[5] Edit Reservation (Admin Override)");
                DrawMenuLine("[6] Cancel Reservation (Admin)");
                DrawMenuLine("[7] Back");
                DrawMenuFooter();
                Console.Write("\nSelect an option: ");
                choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        ViewBookings("ALL BOOKINGS", bookings, _ => true);
                        break;

                    case "2":
                        ViewBookings(
                            "PENDING PAYMENTS",
                            bookings,
                            b => b.PaymentStatus == PaymentStatus.Pending
                        );
                        break;

                    case "3":
                        ViewBookings(
                            "PAID RESERVATIONS",
                            bookings,
                            b => b.PaymentStatus == PaymentStatus.Paid
                        );
                        break;

                    case "4":
                        ViewBookings(
                            "EXPIRED RESERVATIONS",
                            bookings,
                            b => b.PaymentStatus == PaymentStatus.Expired
                        );
                        break;

                    case "5":
                        AdminEditReservation(movies, bookings, seatManagers);
                        break;

                    case "6":
                        AdminCancelReservation(bookings, seatManagers);
                        break;

                    case "7":
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid selection.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(800);
                        break;
                }
            } while (choice != "7");
        }

        private static void ViewBookings(
            string title,
            List<Booking> bookings,
            Func<Booking, bool> predicate
        )
        {
            List<Booking> filtered =
            [
                .. bookings
                    .Where(b => b != null && predicate(b))
                    .OrderByDescending(b => b.ReservationTime),
            ];

            if (filtered.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nNo bookings found for {title.ToLower()}.");
                Console.ResetColor();
                PromptContinue();
                return;
            }

            int page = 0;
            int totalPages = (int)Math.Ceiling(filtered.Count / (double)PageSize);
            string? nav;

            do
            {
                Console.Clear();
                Program.DrawTitle($"{title}");
                Console.WriteLine(
                    "══════════════════════════════════════════════════════════════════════════════════════════════════"
                );
                Console.WriteLine(
                    $"{"ID",-10} {"Movie",-14} {"Schedule",-10} {"Seats",-18} {"Status",-10} {"Total",-10} {"Reserved",-20}"
                );
                Console.WriteLine(
                    "══════════════════════════════════════════════════════════════════════════════════════════════════"
                );

                List<Booking> pageItems = [.. filtered.Skip(page * PageSize).Take(PageSize)];
                foreach (Booking? booking in pageItems)
                {
                    string seats = string.Join(",", booking.Seats ?? []);
                    seats = Truncate(seats, 18);
                    string movie = Truncate(booking.Movie ?? "-", 14);
                    string statusPadding = new(' ', 0);

                    Console.Write(
                        $"{booking.ReservationID,-10} {movie,-14} {booking.Schedule,-10} {seats,-18} "
                    );
                    WriteStatus(booking.PaymentStatus, 10);
                    Console.Write($" ₱{booking.GetGrandTotal(),-9:F2} ");
                    Console.WriteLine($"{booking.ReservationTime:MM/dd/yyyy hh:mm tt}");
                }

                Console.WriteLine(
                    "══════════════════════════════════════════════════════════════════════════════════════════════════"
                );
                Console.WriteLine($"\nPage {page + 1} of {totalPages}. (N)ext, (P)revious, (E)xit");
                nav = Console.ReadLine()?.Trim().ToUpper();

                switch (nav)
                {
                    case "N" when page < totalPages - 1:
                        page++;
                        break;

                    case "P" when page > 0:
                        page--;
                        break;

                    case "E":
                    case "":
                        break;
                }
            } while (nav != "E");
        }

        private static void AdminEditReservation(
            List<Movie> movies,
            List<Booking> bookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            Console.Write("\nEnter Reservation ID (or X to cancel): ");
            string? reservationID = Console.ReadLine()?.Trim().ToUpper();
            if (string.IsNullOrEmpty(reservationID) || reservationID == "X")
                return;

            Booking? booking = bookings.FirstOrDefault(b =>
                b != null
                && b.ReservationID != null
                && b.ReservationID.Equals(reservationID, StringComparison.OrdinalIgnoreCase)
            );

            if (booking == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Reservation not found.");
                Console.ResetColor();
                Thread.Sleep(1000);
                return;
            }

            bool exit = false;
            while (!exit)
            {
                booking.DisplayBookingDetails();
                Console.WriteLine("\nADMIN OVERRIDE MENU");
                Console.WriteLine("------------------------");
                Console.WriteLine("[1] Change Movie");
                Console.WriteLine("[2] Change Schedule");
                Console.WriteLine("[3] Change Seat(s)");
                Console.WriteLine("[4] Change Ticket Quantity");
                Console.WriteLine("[5] Change Food Order");
                Console.WriteLine("[6] Update Payment Status");
                Console.WriteLine("[7] Apply Discount");
                Console.WriteLine("[8] Update Admin Notes");
                Console.WriteLine("[9] Back");
                Console.WriteLine("------------------------");
                Console.Write("Select an option: ");
                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        Program.EditMovie(booking, movies, seatManagers, bookings);
                        AdminLogger.Log(
                            $"Admin changed movie for reservation {booking.ReservationID}"
                        );
                        break;
                    case "2":
                        Program.EditSchedule(booking, movies, seatManagers, bookings);
                        AdminLogger.Log(
                            $"Admin changed schedule for reservation {booking.ReservationID}"
                        );
                        break;
                    case "3":
                        Program.EditSeats(booking, seatManagers, bookings);
                        AdminLogger.Log(
                            $"Admin changed seats for reservation {booking.ReservationID}"
                        );
                        break;
                    case "4":
                        Program.EditTicketQuantity(booking, seatManagers, bookings);
                        AdminLogger.Log(
                            $"Admin changed ticket quantity for reservation {booking.ReservationID}"
                        );
                        break;
                    case "5":
                        Program.EditFood(booking, bookings);
                        AdminLogger.Log(
                            $"Admin changed food order for reservation {booking.ReservationID}"
                        );
                        break;
                    case "6":
                        UpdatePaymentStatus(booking, seatManagers);
                        DataManager.SaveBookings(bookings);
                        AdminLogger.Log(
                            $"Admin updated payment status for reservation {booking.ReservationID} to {booking.PaymentStatus}"
                        );
                        break;
                    case "7":
                        ApplyDiscount(booking, bookings);
                        break;
                    case "8":
                        UpdateAdminNotes(booking, bookings);
                        break;
                    case "9":
                        exit = true;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid selection.");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(800);
                        break;
                }
            }
        }

        private static void UpdatePaymentStatus(
            Booking booking,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            Console.WriteLine("\nSet payment status:");
            Console.WriteLine("[1] Paid");
            Console.WriteLine("[2] Pending");
            Console.WriteLine("[3] Expired");
            Console.Write("Choice: ");
            string? choice = Console.ReadLine()?.Trim();

            if (choice == "1")
            {
                if (booking.PaymentStatus != PaymentStatus.Paid)
                {
                    booking.PaymentStatus = PaymentStatus.Paid;
                    if (string.IsNullOrWhiteSpace(booking.PaymentMethod))
                    {
                        booking.PaymentMethod = "Admin Override";
                    }
                    booking.PaymentReference = string.IsNullOrWhiteSpace(booking.PaymentReference)
                        ? PaymentProcessor.GenerateReference("ADMIN")
                        : booking.PaymentReference;
                    booking.AmountPaid = booking.GetGrandTotal();
                    if (booking.FoodItems != null && booking.FoodItems.Any(kv => kv.Value > 0))
                    {
                        InventoryManager.RecordSale(booking.FoodItems);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Status set to PAID.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Reservation is already PAID.");
                    Console.ResetColor();
                }
            }
            else if (choice == "2")
            {
                booking.PaymentStatus = PaymentStatus.Pending;
                booking.PaymentMethod = string.Empty;
                booking.PaymentReference = string.Empty;
                booking.AmountPaid = 0;
                booking.PaymentDeadline = DateTime.Now.AddMinutes(15);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Status set to PENDING.");
                Console.ResetColor();
            }
            else if (choice == "3")
            {
                booking.PaymentStatus = PaymentStatus.Expired;
                string key = (booking.Movie ?? "") + "|" + (booking.Schedule ?? "");
                if (seatManagers.TryGetValue(key, out SeatManager? value))
                {
                    value.ReleaseSeats(booking.Seats);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Status set to EXPIRED and seats released.");
                Console.ResetColor();
            }
            booking.LastModified = DateTime.Now;
            Thread.Sleep(1000);
        }

        private static void ApplyDiscount(Booking booking, List<Booking> bookings)
        {
            double baseTotal = booking.Price * booking.Quantity + booking.FoodTotal;
            Console.Write(
                $"\nCurrent discount: ₱{booking.Discount:F2}. Enter new discount amount: "
            );
            if (!double.TryParse(Console.ReadLine(), out double discount) || discount < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid discount amount.");
                Console.ResetColor();
                Program.PlayErrorBeep();
                Thread.Sleep(800);
                return;
            }
            if (discount > baseTotal)
            {
                discount = baseTotal;
            }
            booking.Discount = discount;
            booking.LastModified = DateTime.Now;
            DataManager.SaveBookings(bookings);
            AdminLogger.Log(
                $"Admin set discount ₱{discount:F2} for reservation {booking.ReservationID}"
            );
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Discount updated.");
            Console.ResetColor();
            Thread.Sleep(800);
        }

        private static void UpdateAdminNotes(Booking booking, List<Booking> bookings)
        {
            Console.WriteLine("\nEnter admin notes (leave blank to clear): ");
            string? notes = Console.ReadLine();
            booking.AdminNotes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
            booking.LastModified = DateTime.Now;
            DataManager.SaveBookings(bookings);
            AdminLogger.Log($"Admin updated notes for reservation {booking.ReservationID}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Notes updated.");
            Console.ResetColor();
            Thread.Sleep(800);
        }

        private static void AdminCancelReservation(
            List<Booking> bookings,
            Dictionary<string, SeatManager> seatManagers
        )
        {
            Console.Write("\nEnter Reservation ID to cancel (or X to abort): ");
            string? reservationID = Console.ReadLine()?.Trim().ToUpper();
            if (string.IsNullOrEmpty(reservationID) || reservationID == "X")
                return;

            Booking? booking = bookings.FirstOrDefault(b =>
                b != null
                && b.ReservationID != null
                && b.ReservationID.Equals(reservationID, StringComparison.OrdinalIgnoreCase)
            );

            if (booking == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Reservation not found.");
                Console.ResetColor();
                Thread.Sleep(800);
                return;
            }

            Console.Write("Confirm cancellation? (Y/N): ");
            string? confirm = Console.ReadLine()?.Trim().ToUpper();
            if (confirm != "Y")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Cancellation aborted.");
                Console.ResetColor();
                Thread.Sleep(800);
                return;
            }

            string key = (booking.Movie ?? string.Empty) + "|" + (booking.Schedule ?? string.Empty);
            if (seatManagers.TryGetValue(key, out SeatManager? value))
            {
                value.ReleaseSeats(booking.Seats);
            }

            // Refund stock and show refund info if PAID
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                if (booking.FoodItems != null && booking.FoodItems.Count != 0)
                {
                    InventoryManager.RestoreSale(booking.FoodItems);
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n💰 REFUND NOTICE 💰");
                Console.WriteLine($"Refund Amount: ₱{booking.GetGrandTotal():F2}");
                Console.WriteLine("Please process refund manually.");
                Console.ResetColor();
                Thread.Sleep(1500);
            }
            bookings.Remove(booking);
            DataManager.SaveBookings(bookings);
            AdminLogger.Log($"Admin cancelled reservation {reservationID}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Reservation cancelled.");
            Console.ResetColor();
            Thread.Sleep(1000);
        }

        private static void WriteStatus(PaymentStatus status, int width)
        {
            ConsoleColor color = ConsoleColor.Gray;
            switch (status)
            {
                case PaymentStatus.Paid:
                    color = ConsoleColor.Green;
                    break;
                case PaymentStatus.Pending:
                    color = ConsoleColor.Yellow;
                    break;
                case PaymentStatus.Expired:
                    color = ConsoleColor.Red;
                    break;
                case PaymentStatus.Cancelled:
                    color = ConsoleColor.Red;
                    break;
            }
            string text = status.ToString();
            if (text.Length > width)
                text = text[..width];
            Console.ForegroundColor = color;
            Console.Write($"{text}{new string(' ', Math.Max(0, width - text.Length))}");
            Console.ResetColor();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
        }

        private static void DrawMenuHeader(string title)
        {
            const int width = 50;
            Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            Console.WriteLine(BoxCenter(title, width));
            Console.WriteLine("╠" + new string('═', width - 2) + "╣");
        }

        private static void DrawMenuLine(string text)
        {
            const int width = 50;
            Console.WriteLine(BoxLine(text, width));
        }

        private static void DrawMenuFooter()
        {
            const int width = 50;
            Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        }

        private static string BoxLine(string text, int width)
        {
            if (text.Length > width - 4)
                text = text[..(width - 4)];
            return $"║ {text}{new string(' ', width - 3 - text.Length)}║";
        }

        private static string BoxCenter(string text, int width)
        {
            if (text.Length > width - 2)
                text = text[..(width - 2)];
            int pad = width - 2 - text.Length;
            int left = pad / 2;
            int right = pad - left;
            return $"║{new string(' ', left)}{text}{new string(' ', right)}║";
        }

        private static void PromptContinue()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    // ---------- ADMIN LOGGER ----------
    internal static class AdminLogger
    {
        private const string LogPath = "admin_log.txt";

        public static void Log(string action)
        {
            try
            {
                string entry = $"[{DateTime.Now:MM/dd/yyyy hh:mm tt}] {action}";
                File.AppendAllLines(LogPath, [entry]);
            }
            catch
            {
                // swallow logging errors
            }
        }

        public static (double Amount, int Count) GetTodaysSalesAndTransactions(
            List<Booking> bookings
        )
        {
            DateTime today = DateTime.Today;
            double amount = 0;
            int count = 0;
            foreach (Booking booking in bookings)
            {
                if (
                    booking != null
                    && booking.PaymentStatus == PaymentStatus.Paid
                    && booking.ReservationTime.Date == today
                )
                {
                    amount += booking.GetGrandTotal();
                    count++;
                }
            }
            return (amount, count);
        }
    }

    // ---------- DASHBOARD SUMMARY ----------
    internal static class DashboardSummary
    {
        public struct Summary
        {
            public double TodaysSales;
            public int PendingPayments;
            public int LowStockItems;
            public int TotalReservations;
        }

        public static Summary Calculate(List<Booking> bookings)
        {
            List<FoodInventory> inventory = InventoryManager.LoadInventory();
            double todaysSales = bookings
                .Where(b =>
                    b != null
                    && b.PaymentStatus == PaymentStatus.Paid
                    && b.ReservationTime.Date == DateTime.Today
                )
                .Sum(b => b.GetGrandTotal());
            int pending = bookings.Count(b =>
                b != null && b.PaymentStatus == PaymentStatus.Pending
            );
            int lowStock = inventory.Count(i => i.Stock < i.ReorderLevel);
            int total = bookings.Count(b => b != null);

            return new Summary
            {
                TodaysSales = todaysSales,
                PendingPayments = pending,
                LowStockItems = lowStock,
                TotalReservations = total,
            };
        }
    }

    // ---------- PAYMENT PROCESSOR ----------
    internal static class PaymentProcessor
    {
        private static readonly HashSet<string> usedReferences = new(
            StringComparer.OrdinalIgnoreCase
        );

        public static FoodOrder.PaymentResult ProcessPayment(Booking booking)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Program.DrawTitle("PAYMENT SUMMARY");
            Console.ResetColor();

            Console.WriteLine($"Reservation ID: {booking.ReservationID}");
            Console.WriteLine($"Movie: {booking.Movie}");
            Console.WriteLine($"Schedule: {booking.Schedule}");
            Console.WriteLine($"Seats: {string.Join(" ", booking.Seats.Select(s => $"[{s}]"))}");
            Console.WriteLine($"Tickets: {booking.Quantity} x ₱{booking.Price}");
            Console.WriteLine($"Food Total: ₱{booking.FoodTotal:F2}");
            if (booking.Discount > 0)
            {
                Console.WriteLine($"Discount: -₱{booking.Discount:F2}");
            }
            Console.WriteLine($"Grand Total: ₱{booking.GetGrandTotal():F2}");
            if (booking.PaymentDeadline != default)
            {
                Program.DisplayPaymentDeadline(booking.PaymentDeadline);
            }
            Console.WriteLine();

            string methodChoice = SelectPaymentMethod();

            return methodChoice switch
            {
                "1" => ProcessCash(booking),
                "2" => ProcessEWallet(booking, "GCash"),
                "3" => ProcessEWallet(booking, "PayMaya"),
                "4" => ProcessCard(booking),
                "5" => ProcessPayLater(booking),
                _ => new FoodOrder.PaymentResult
                {
                    Success = false,
                    Method = "Unknown",
                    Reference = "",
                    AmountPaid = 0,
                    Timestamp = DateTime.Now,
                    CashTendered = 0,
                    Change = 0,
                },
            };
        }

        public static string SelectPaymentMethod()
        {
            int menuTop = Console.CursorTop;

            while (true)
            {
                Console.SetCursorPosition(0, menuTop);
                Console.WriteLine("[1] Cash at Counter                    ");
                Console.WriteLine("[2] GCash                              ");
                Console.WriteLine("[3] PayMaya                            ");
                Console.WriteLine("[4] Credit/Debit Card                  ");
                Console.WriteLine("[5] Pay Later                          ");
                Console.WriteLine("------------------------------         ");
                Console.Write("Select payment method: ");
                string? choice = Console.ReadLine()?.Trim();
                if (choice == "1" || choice == "2" || choice == "3" || choice == "4" || choice == "5")
                    return choice;

                int errorTop = Console.CursorTop;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid choice. Try again.             ");
                Console.ResetColor();
                Program.PlayErrorBeep();
                Thread.Sleep(1200);

                // Clear error message
                Console.SetCursorPosition(0, errorTop);
                Console.WriteLine("                                       ");
            }
        }

        public static FoodOrder.PaymentResult ProcessCash(Booking booking)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Program.DrawTitle("CASH PAYMENT");
            Console.ResetColor();

            double total = booking.GetGrandTotal();
            Console.WriteLine($"Total Amount: ₱{total:F2}");

            double tendered = 0;
            while (true)
            {
                Console.Write("Amount tendered: ₱");
                string? raw = Console.ReadLine();
                if (double.TryParse(raw, out tendered) && tendered >= total)
                    break;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Insufficient amount. Please try again.\n");
                Console.ResetColor();
                Program.PlayErrorBeep();
                Thread.Sleep(800);
            }

            double change = tendered - total;
            Console.WriteLine($"Change: ₱{change:F2}");

            string reference = GenerateReference("CASH");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Payment Successful!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(800);

            return new FoodOrder.PaymentResult
            {
                Success = true,
                Method = "Cash",
                Reference = reference,
                AmountPaid = total,
                Timestamp = DateTime.Now,
                CashTendered = tendered,
                Change = change,
            };
        }

        public static FoodOrder.PaymentResult ProcessEWallet(Booking booking, string walletName)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Program.DrawTitle($"{walletName.ToUpper()} PAYMENT");
            Console.ResetColor();

            double total = booking.GetGrandTotal();
            Console.WriteLine($"Total Amount: ₱{total:F2}");
            Console.WriteLine("Send payment to: 0912-345-6789\n");
            DisplayQRCode();

            string reference = InputValidator
                .ReadNonEmptyLine("\nEnter reference number: ")
                .ToUpper();
            Console.Write("Confirm payment received? (Y/N): ");
            string? confirm = Console.ReadLine()?.Trim().ToUpper();

            bool success = confirm == "Y";
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Payment Confirmed!");
                Console.ResetColor();
                Program.PlaySuccessBeep();
                Thread.Sleep(800);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Payment not confirmed.");
                Console.ResetColor();
                Thread.Sleep(800);
            }

            // Ensure unique reference, prefix by wallet
            string uniqueRef = EnsureUniqueReference($"{walletName.ToUpper()}-{reference}");
            return new FoodOrder.PaymentResult
            {
                Success = success,
                Method = walletName,
                Reference = uniqueRef,
                AmountPaid = success ? total : 0,
                Timestamp = DateTime.Now,
                CashTendered = 0,
                Change = 0,
            };
        }

        public static FoodOrder.PaymentResult ProcessCard(Booking booking)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Program.DrawTitle("CARD PAYMENT");
            Console.ResetColor();

            double total = booking.GetGrandTotal();
            Console.WriteLine($"Total Amount: ₱{total:F2}\n");

            // Card details are collected but not validated in this demo implementation
            _ = ReadMaskedCardNumber("Card number: ");
            _ = InputValidator.ReadNonEmptyLine("Cardholder name: ");
            _ = ReadMaskedCVV("CVV: ");

            Console.WriteLine();
            Program.ShowLoadingAnimation("Processing payment", 2000);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Payment Approved!");
            Console.ResetColor();
            Program.PlaySuccessBeep();
            Thread.Sleep(800);

            string reference = GenerateReference("CARD");
            return new FoodOrder.PaymentResult
            {
                Success = true,
                Method = "Card",
                Reference = reference,
                AmountPaid = total,
                Timestamp = DateTime.Now,
                CashTendered = 0,
                Change = 0,
            };
        }

        public static FoodOrder.PaymentResult ProcessPayLater(Booking booking)
        {
            Console.Clear();
            Program.DrawTitle("PAY LATER");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nYour booking will remain PENDING.");
            Console.ResetColor();

            Program.DisplayPaymentDeadline(booking.PaymentDeadline);

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();

            return new FoodOrder.PaymentResult
            {
                Success = false,
                Method = "Pay Later",
                Reference = "",
                AmountPaid = 0,
                Timestamp = DateTime.Now,
                CashTendered = 0,
                Change = 0,
            };
        }

        public static void DisplayQRCode()
        {
            Console.WriteLine("+----------------------+");
            Console.WriteLine("| █ █  ▓▓  █   ▓  █ █ |");
            Console.WriteLine("| ▓  ▓  █  ▓ █  █  ▓  |");
            Console.WriteLine("| █ ▓ █ ▓  ▓  ▓ █ ▓ █ |");
            Console.WriteLine("| ▓  █  ▓  █  ▓  █  ▓ |");
            Console.WriteLine("| █ █  ▓▓  █   ▓  █ █ |");
            Console.WriteLine("+----------------------+");
        }

        public static string GenerateReference(string prefix)
        {
            string candidate;
            Random rnd = new();
            do
            {
                candidate = $"{prefix}-{rnd.Next(100000, 999999)}";
            } while (!usedReferences.Add(candidate));
            return candidate;
        }

        private static string EnsureUniqueReference(string baseRef)
        {
            string finalRef = baseRef;
            int counter = 1;
            while (!usedReferences.Add(finalRef))
            {
                finalRef = $"{baseRef}-{counter++}";
            }
            return finalRef;
        }

        public static void DisplayReceipt(Booking booking, FoodOrder.PaymentResult payment)
        {
            Console.Clear();
            int width = 40;
            string borderTop = "╔" + new string('═', width - 2) + "╗";
            string borderMid = "╠" + new string('═', width - 2) + "╣";
            string borderSide = "║";
            string borderBot = "╚" + new string('═', width - 2) + "╝";

            Console.WriteLine(borderTop);
            WriteReceiptLine(borderSide, width, "CINEMA BOOKING RECEIPT", center: true);
            Console.WriteLine(borderMid);
            WriteReceiptLine(borderSide, width, $"Reservation: {booking.ReservationID}");
            WriteReceiptLine(borderSide, width, $"Payment Ref: {payment.Reference}");
            WriteReceiptLine(borderSide, width, $"Method: {payment.Method}");
            if (booking.Discount > 0)
            {
                WriteReceiptLine(borderSide, width, $"Discount: -₱{booking.Discount:F2}");
            }
            WriteReceiptLine(borderSide, width, $"Amount: ₱{booking.GetGrandTotal():F2}");
            if (payment.Method == "Cash")
            {
                WriteReceiptLine(borderSide, width, $"Cash Tendered: ₱{payment.CashTendered:F2}");
                WriteReceiptLine(borderSide, width, $"Change: ₱{payment.Change:F2}");
            }
            WriteReceiptLine(borderSide, width, $"Status: PAID");
            WriteReceiptLine(borderSide, width, $"Date: {payment.Timestamp:g}");
            Console.WriteLine(borderMid);
            WriteReceiptLine(borderSide, width, $"Movie: {booking.Movie}");
            WriteReceiptLine(borderSide, width, $"Schedule: {booking.Schedule}");
            WriteReceiptLine(borderSide, width, $"Seats: {string.Join(",", booking.Seats)}");
            Console.WriteLine(borderMid);
            WriteReceiptLine(borderSide, width, "Thank you for your booking!", center: true);
            Console.WriteLine(borderBot);
        }

        private static void WriteReceiptLine(
            string side,
            int width,
            string content,
            bool center = false
        )
        {
            if (content.Length > width - 4)
                content = content[..(width - 4)];
            int pad = width - 2 - content.Length;
            if (center)
            {
                int left = pad / 2;
                int right = pad - left;
                Console.WriteLine(
                    $"{side}{new string(' ', left)}{content}{new string(' ', right)}{side}"
                );
            }
            else
            {
                Console.WriteLine($"{side} {content}{new string(' ', pad - 1)}{side}");
            }
        }

        private static string ReadMaskedCardNumber(string prompt)
        {
            Console.Write(prompt);
            string raw = string.Empty;
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                if (key.Key == ConsoleKey.Backspace && raw.Length > 0)
                {
                    raw = raw[..^1];
                }
                else if (char.IsDigit(key.KeyChar) && raw.Length < 16)
                {
                    raw += key.KeyChar;
                }
                // redraw masked
                Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
                Console.Write(prompt);
                Console.Write(FormatMaskedCard(raw));
            }
            Console.WriteLine();
            return raw;
        }

        private static string FormatMaskedCard(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return string.Empty;
            string visibleStart = digits.Length >= 4 ? digits[..4] : digits;
            string visibleEnd =
                digits.Length > 4 ? digits[Math.Max(0, digits.Length - 4)..] : string.Empty;
            int middleGroups = Math.Max(0, (digits.Length - 8 + 3) / 4);
            string maskedMiddle = string.Join(" ", Enumerable.Repeat("****", middleGroups));
            string result = visibleStart;
            if (!string.IsNullOrEmpty(maskedMiddle))
                result += " " + maskedMiddle;
            if (!string.IsNullOrEmpty(visibleEnd))
            {
                if (!string.IsNullOrEmpty(maskedMiddle))
                    result += " ";
                result += visibleEnd;
            }
            return result;
        }

        private static string ReadMaskedCVV(string prompt)
        {
            Console.Write(prompt);
            string raw = string.Empty;
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                if (key.Key == ConsoleKey.Backspace && raw.Length > 0)
                {
                    raw = raw[..^1];
                }
                else if (char.IsDigit(key.KeyChar) && raw.Length < 4)
                {
                    raw += key.KeyChar;
                }
                // redraw masked
                Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
                Console.Write(prompt);
                Console.Write(new string('*', raw.Length));
            }
            Console.WriteLine();
            return raw;
        }
    }

    // ---------- INPUT VALIDATOR ----------
    internal static class InputValidator
    {
        public static string ReadNonEmptyLine(string prompt)
        {
            string? input;
            do
            {
                if (!string.IsNullOrEmpty(prompt))
                    Console.Write(prompt);
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Input cannot be empty. Please try again.");
                    Console.ResetColor();
                }
            } while (string.IsNullOrWhiteSpace(input));

            return input.Trim();
        }

        public static string ReadNonEmptyLine()
        {
            return ReadNonEmptyLine(string.Empty);
        }

        public static string ReadMasked(string prompt)
        {
            Console.Write(prompt);
            string input = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    input += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return input;
        }

        public static int ReadIntInRange(string prompt, int min, int max)
        {
            int cursorTop = Console.CursorTop;
            while (true)
            {
                Console.SetCursorPosition(0, cursorTop);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, cursorTop);
                Console.Write(prompt);

                string? input = Console.ReadLine();
                if (int.TryParse(input, out int result) && result >= min && result <= max)
                {
                    return result;
                }

                // Clear the error line if it exists
                int errorLine = Console.CursorTop;
                Console.SetCursorPosition(0, errorLine);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, errorLine);
                Console.WriteLine(
                    "Invalid input. Please enter a number between {0} and {1}.",
                    min,
                    max
                );
                Console.ResetColor();
                Program.PlayErrorBeep();
                Thread.Sleep(1200);

                // Clear error message
                Console.SetCursorPosition(0, errorLine);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, cursorTop);
            }
        }
    }

    // ---------- FOOD ITEM ----------
    internal class FoodItem
    {
        private string name;
        private double price;
        private int quantity;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public double Price
        {
            get => price;
            set => price = value;
        }

        public int Quantity
        {
            get => quantity;
            set => quantity = value;
        }

        public FoodItem(string name, double price)
        {
            Name = name;
            Price = price;
            Quantity = 0;
        }

        public double GetTotal()
        {
            return Price * Quantity;
        }
    }

    // ---------- FOOD ORDER ----------
    internal class FoodOrder
    {
        private Dictionary<string, int> items;
        private double totalAmount;

        // ---------- PAYMENT RESULT ----------
        internal class PaymentResult
        {
            public bool Success { get; set; }
            public string Method { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public double AmountPaid { get; set; }
            public DateTime Timestamp { get; set; }
            public double CashTendered { get; set; }
            public double Change { get; set; }
        }

        public Dictionary<string, int> Items
        {
            get => items;
            set => items = value;
        }

        public double TotalAmount
        {
            get => totalAmount;
            set => totalAmount = value;
        }

        public FoodOrder()
        {
            Items = [];
            TotalAmount = 0;
        }

        public static FoodOrder SelectFood(
            bool isAdmin = false,
            Dictionary<string, int>? existingOrder = null
        )
        {
            List<FoodItem> foodItems =
            [
                new FoodItem("Popcorn Regular", 85.00),
                new FoodItem("Popcorn Large", 120.00),
                new FoodItem("Popcorn Paper Bucket", 140.00),
                new FoodItem("Classic Hotdog", 95.00),
                new FoodItem("Chicken Nuggets", 140.00),
                new FoodItem("Classic Fries", 60.00),
                new FoodItem("Cheese Fries", 75.00),
                new FoodItem("BBQ Fries", 75.00),
                new FoodItem("Sour Cream Fries", 75.00),
                new FoodItem("Regular Soda", 45.00),
                new FoodItem("Large Soda", 80.00),
                new FoodItem("Bottled Water", 35.00),
                new FoodItem("Iced Tea", 45.00),
                new FoodItem("Iced Coffee", 50.00),
            ];

            List<FoodInventory> inventoryList = InventoryManager.LoadInventory();
            Dictionary<string, int> stockLookup = inventoryList.ToDictionary(
                i => i.Name,
                i => i.Stock,
                StringComparer.OrdinalIgnoreCase
            );

            // Pre-populate from existing order if provided
            if (existingOrder != null)
            {
                foreach (FoodItem item in foodItems)
                {
                    if (existingOrder.TryGetValue(item.Name, out int qty))
                    {
                        item.Quantity = Math.Max(0, qty);
                    }
                }
            }

            FoodOrder order = new();
            int currentIndex = 0;

            while (currentIndex < foodItems.Count)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Program.DrawTitle("FOOD SELECTION");
                Console.ResetColor();

                // Display FOOD section
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("FOOD:");
                Console.ResetColor();
                for (int i = 0; i < 9; i++)
                {
                    if (i == currentIndex)
                        Console.ForegroundColor = ConsoleColor.Green;
                    if (isAdmin)
                    {
                        int stock = stockLookup.TryGetValue(foodItems[i].Name, out int s) ? s : 0;
                        int remaining = Math.Max(0, stock - foodItems[i].Quantity);
                        Console.WriteLine(
                            $"{foodItems[i].Name} (₱{foodItems[i].Price:F2}): {foodItems[i].Quantity} [Stock: {remaining}]"
                        );
                    }
                    else
                    {
                        Console.WriteLine(
                            $"{foodItems[i].Name} (₱{foodItems[i].Price:F2}): {foodItems[i].Quantity}"
                        );
                    }
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("BEVERAGE:");
                Console.ResetColor();
                for (int i = 9; i < foodItems.Count; i++)
                {
                    if (i == currentIndex)
                        Console.ForegroundColor = ConsoleColor.Green;
                    if (isAdmin)
                    {
                        int stock = stockLookup.TryGetValue(foodItems[i].Name, out int s) ? s : 0;
                        int remaining = Math.Max(0, stock - foodItems[i].Quantity);
                        Console.WriteLine(
                            $"{foodItems[i].Name} (₱{foodItems[i].Price:F2}): {foodItems[i].Quantity} [Stock: {remaining}]"
                        );
                    }
                    else
                    {
                        Console.WriteLine(
                            $"{foodItems[i].Name} (₱{foodItems[i].Price:F2}): {foodItems[i].Quantity}"
                        );
                    }
                    Console.ResetColor();
                }

                Console.WriteLine("______________________");
                double total = foodItems.Sum(f => f.GetTotal());
                Console.WriteLine($"Total Amount: ₱{total:F2}");
                Console.WriteLine(
                    "\nUse arrow key <- / -> to adjust quantity for highlighted item."
                );
                Console.WriteLine(
                    "Use UP / DOWN arrows to navigate items. Press ENTER to confirm selection."
                );

                ConsoleKey key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.RightArrow)
                {
                    FoodItem item = foodItems[currentIndex];
                    int stock = stockLookup.TryGetValue(item.Name, out int s) ? s : 0;
                    if (stock <= 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Insufficient stock!");
                        Console.ResetColor();
                        Program.PlayErrorBeep();
                        Thread.Sleep(800);
                    }
                    else if (item.Quantity < (stock - 1))
                    {
                        item.Quantity++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(
                            $"Maximum order: {stock - 1} (must keep at least 1 in stock)!"
                        );
                        Console.ResetColor();
                        Thread.Sleep(800);
                    }
                }
                else if (key == ConsoleKey.LeftArrow && foodItems[currentIndex].Quantity > 0)
                {
                    foodItems[currentIndex].Quantity--;
                }
                else if (key == ConsoleKey.DownArrow && currentIndex < foodItems.Count - 1)
                {
                    currentIndex++;
                }
                else if (key == ConsoleKey.UpArrow && currentIndex > 0)
                {
                    currentIndex--;
                }
                else if (key == ConsoleKey.Enter)
                {
                    // Move to next item, or finish if at the end
                    if (currentIndex < foodItems.Count - 1)
                    {
                        currentIndex++;
                    }
                    else
                    {
                        // Finished selecting all items
                        break;
                    }
                }
            }

            // Build the order
            foreach (FoodItem item in foodItems)
            {
                if (item.Quantity > 0)
                {
                    order.Items[item.Name] = item.Quantity;
                    order.TotalAmount += item.GetTotal();
                }
            }

            return order;
        }
    }

    // ---------- BOOKING ----------
    internal class Booking
    {
        private string movie;
        private string schedule;
        private int price;
        private int quantity;
        private List<string> seats;
        private string reservationID;
        private string passkey;
        private DateTime reservationTime;
        private DateTime lastModified;
        private Dictionary<string, int> foodItems;
        private double foodTotal;
        private PaymentStatus paymentStatus;
        private string paymentMethod;
        private string paymentReference;
        private DateTime paymentDeadline;
        private double amountPaid;
        private double discount;
        private string adminNotes;

        // Properties with SETTERS for JSON serialization
        public string Movie
        {
            get => movie;
            set => movie = value;
        }

        public string Schedule
        {
            get => schedule;
            set => schedule = value;
        }

        public int Price
        {
            get => price;
            set => price = value;
        }

        public int Quantity
        {
            get => quantity;
            set => quantity = value;
        }

        public List<string> Seats
        {
            get => seats;
            set => seats = value;
        }

        public string ReservationID
        {
            get => reservationID;
            set => reservationID = value;
        }

        public string Passkey
        {
            get => passkey;
            set => passkey = value;
        }

        public DateTime ReservationTime
        {
            get => reservationTime;
            set => reservationTime = value;
        }

        public DateTime LastModified
        {
            get => lastModified;
            set => lastModified = value;
        }

        public Dictionary<string, int> FoodItems
        {
            get => foodItems;
            set => foodItems = value;
        }

        public double FoodTotal
        {
            get => foodTotal;
            set => foodTotal = value;
        }

        public PaymentStatus PaymentStatus
        {
            get => paymentStatus;
            set => paymentStatus = value;
        }

        public string PaymentMethod
        {
            get => paymentMethod;
            set => paymentMethod = value;
        }

        public string PaymentReference
        {
            get => paymentReference;
            set => paymentReference = value;
        }

        public DateTime PaymentDeadline
        {
            get => paymentDeadline;
            set => paymentDeadline = value;
        }

        public double AmountPaid
        {
            get => amountPaid;
            set => amountPaid = value;
        }

        public double Discount
        {
            get => discount;
            set => discount = value;
        }

        public string AdminNotes
        {
            get => adminNotes;
            set => adminNotes = value;
        }

        // Parameterless constructor for JSON deserialization
        public Booking()
        {
            seats = [];
            foodItems = [];
            paymentStatus = PaymentStatus.Pending;
            discount = 0;
            adminNotes = string.Empty;
        }

        public Booking(
            string movie,
            string schedule,
            int price,
            int quantity,
            List<string> seats,
            Dictionary<string, int> foodItems,
            double foodTotal
        )
        {
            this.movie = movie;
            this.schedule = schedule;
            this.price = price;
            this.quantity = quantity;
            this.seats = seats;
            this.reservationTime = DateTime.Now;
            this.lastModified = DateTime.Now;
            this.foodItems = foodItems;
            this.foodTotal = foodTotal;
            this.paymentStatus = PaymentStatus.Pending;
            this.discount = 0;
            this.adminNotes = string.Empty;
        }

        public double GetGrandTotal()
        {
            double total = (price * quantity) + foodTotal - discount;
            if (total < 0)
                total = 0;
            return total;
        }

        public static Booking CreateBooking(Movie movie, Schedule schedule, SeatManager seatManager)
        {
            Console.Clear();
            Program.DrawTitle("SELECT TICKETS");
            Console.WriteLine($"Movie: {movie.Movies}");
            Console.WriteLine($"Schedule: {schedule.Time}");
            Console.WriteLine($"Ticket Price: ₱{schedule.Price}");
            Console.WriteLine("----------------------------------");

            int quantity = 1;
            ConsoleKey key;
            do

            {
                Console.SetCursorPosition(0, 4);
                Console.WriteLine($"Ticket Quantity: {quantity}        ");
                double total = schedule.Price * quantity;
                Console.WriteLine($"Total Amount: ₱{total:F2}        ");
                Console.WriteLine(
                    "\nUse arrow key <- / -> to adjust quantity. Press ENTER to confirm."
                );

                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.RightArrow && quantity < 5)
                    quantity++;
                else if (key == ConsoleKey.LeftArrow && quantity > 1)
                    quantity--;
            } while (key != ConsoleKey.Enter);

            Console.Clear();
            List<string> selectedSeats = seatManager.SelectSeats(quantity);

            Program.PlaySuccessBeep();
            // Select food after seats
            FoodOrder foodOrder = FoodOrder.SelectFood(false);

            return new Booking(
                movie.Movies,
                schedule.Time,
                schedule.Price,
                quantity,
                selectedSeats,
                foodOrder.Items,
                foodOrder.TotalAmount
            );
        }

        public void DisplaySummary()
        {
            Console.Clear();
            Program.DrawTitle("BOOKING SUMMARY");
            Console.WriteLine();
            const int boxWidth = 50;
            double ticketTotal = price * quantity;
            double grandTotal = GetGrandTotal();

            Console.WriteLine("╔" + new string('═', boxWidth - 2) + "╗");
            Console.WriteLine("║  Movie: " + movie.PadRight(boxWidth - 11) + "║");
            Console.WriteLine("║  Schedule: " + schedule.PadRight(boxWidth - 15) + "║");
            Console.WriteLine(
                "║  Seats: "
                    + string.Join(" ", seats.ConvertAll(s => $"[{s}]")).PadRight(boxWidth - 11)
                    + "║"
            );
            Console.WriteLine("║  Quantity: " + quantity.ToString().PadRight(boxWidth - 15) + "║");
            Console.WriteLine("╠" + new string('═', boxWidth - 2) + "╣");
            Console.WriteLine(
                "║  Ticket Total: ₱" + ticketTotal.ToString("F2").PadRight(boxWidth - 20) + "║"
            );
            Console.WriteLine(
                "║  Payment Status: " + paymentStatus.ToString().PadRight(boxWidth - 21) + "║"
            );
            if (paymentStatus == PaymentStatus.Pending && paymentDeadline != default)
            {
                TimeSpan remaining = paymentDeadline - DateTime.Now;
                string timeText =
                    remaining.TotalHours >= 1
                        ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"{remaining.Minutes}m {remaining.Seconds}s";
                Console.WriteLine("║  Payment due in: " + timeText.PadRight(boxWidth - 21) + "║");
            }
            if (paymentStatus == PaymentStatus.Paid)
            {
                Console.WriteLine(
                    "║  Payment Method: " + paymentMethod.PadRight(boxWidth - 21) + "║"
                );
                Console.WriteLine(
                    "║  Payment Reference: " + paymentReference.PadRight(boxWidth - 26) + "║"
                );
            }

            if (foodItems != null && foodItems.Count > 0)
            {
                Console.WriteLine("╠" + new string('═', boxWidth - 2) + "╣");
                Console.WriteLine("║  Food Items:" + new string(' ', boxWidth - 15) + "║");
                foreach (KeyValuePair<string, int> item in foodItems)
                {
                    string foodLine = $"    - {item.Key} x{item.Value}";
                    Console.WriteLine("║" + foodLine.PadRight(boxWidth - 2) + "║");
                }
                Console.WriteLine(
                    "║  Food Total: ₱" + foodTotal.ToString("F2").PadRight(boxWidth - 19) + "║"
                );
            }
            if (discount > 0)
            {
                Console.WriteLine(
                    "║  Discount: -₱" + discount.ToString("F2").PadRight(boxWidth - 18) + "║"
                );
            }
            if (!string.IsNullOrWhiteSpace(adminNotes))
            {
                Console.WriteLine("║  Admin Notes: " + adminNotes.PadRight(boxWidth - 17) + "║");
            }

            Console.WriteLine("╠" + new string('═', boxWidth - 2) + "╣");
            Console.WriteLine(
                "║  GRAND TOTAL: ₱" + grandTotal.ToString("F2").PadRight(boxWidth - 20) + "║"
            );
            Console.WriteLine("╚" + new string('═', boxWidth - 2) + "╝");

            Console.WriteLine("\nEnter your details below:");

            reservationID = GenerateReservationID();

            Console.Clear();
            Program.DrawTitle("FINAL BOOKING DETAILS");
            Console.WriteLine();

            // Define column widths
            int timeWidth = 19;
            int movieWidth = 14;
            int scheduleWidth = 10;
            int seatsWidth = 14;
            int ticketWidth = 10;
            int itemsWidth = 28;
            int foodWidth = 10;
            int grandTotalWidth = 12;
            int idWidth = 12;

            // Print header
            int totalLineWidth =
                timeWidth
                + movieWidth
                + scheduleWidth
                + seatsWidth
                + ticketWidth
                + itemsWidth
                + foodWidth
                + grandTotalWidth
                + idWidth
                + (8 * 3);
            Console.WriteLine(new string('-', totalLineWidth));
            string header = string.Format(
                "{0,-"
                    + timeWidth
                    + "} | {1,-"
                    + movieWidth
                    + "} | {2,-"
                    + scheduleWidth
                    + "} | {3,-"
                    + seatsWidth
                    + "} | {4,-"
                    + ticketWidth
                    + "} | {5,-"
                    + itemsWidth
                    + "} | {6,-"
                    + foodWidth
                    + "} | {7,-"
                    + grandTotalWidth
                    + "} | {8,-"
                    + idWidth
                    + "}",
                "Time",
                "Movie",
                "Schedule",
                "Seats",
                "Ticket",
                "Items",
                "Food",
                "Grand Total",
                "ID"
            );
            Console.WriteLine(header);

            Console.WriteLine(new string('-', totalLineWidth));

            // Prepare item lines
            List<string> itemLines = [];
            if (foodItems != null && foodItems.Count > 0)
            {
                itemLines.AddRange(foodItems.Select(kv => $"({kv.Value}) {kv.Key}"));
            }
            else
            {
                itemLines.Add("-");
            }

            // Totals
            string ticketTotalStr = $"₱{ticketTotal:F2}";
            string foodTotalStr = $"₱{foodTotal:F2}";
            string grandTotalStr = $"₱{grandTotal:F2}";

            // Print data rows with padding
            string timeStr = reservationTime.ToString("g");
            string seatsStr = string.Join(",", seats);

            for (int i = 0; i < itemLines.Count; i++)
            {
                string timeCell = i == 0 ? timeStr : string.Empty;
                string movieCell = i == 0 ? movie : string.Empty;
                string scheduleCell = i == 0 ? schedule : string.Empty;
                string seatsCell = i == 0 ? seatsStr : string.Empty;
                string ticketCell = i == 0 ? ticketTotalStr : string.Empty;
                string foodCell = i == 0 ? foodTotalStr : string.Empty;
                string totalCell = i == 0 ? grandTotalStr : string.Empty;
                string idCell = i == 0 ? reservationID : string.Empty;

                string dataRow = string.Format(
                    "{0,-"
                        + timeWidth
                        + "} | {1,-"
                        + movieWidth
                        + "} | {2,-"
                        + scheduleWidth
                        + "} | {3,-"
                        + seatsWidth
                        + "} | {4,-"
                        + ticketWidth
                        + "} | {5,-"
                        + itemsWidth
                        + "} | {6,-"
                        + foodWidth
                        + "} | {7,-"
                        + grandTotalWidth
                        + "} | {8,-"
                        + idWidth
                        + "}",
                    timeCell,
                    movieCell,
                    scheduleCell,
                    seatsCell,
                    ticketCell,
                    itemLines[i],
                    foodCell,
                    totalCell,
                    idCell
                );
                Console.WriteLine(dataRow);
            }
            Console.WriteLine(new string('-', totalLineWidth));
        }

        public void DisplayBookingDetails()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Program.DrawTitle("BOOKING DETAILS");
            Console.ResetColor();

            // Payment status line
            if (paymentStatus == PaymentStatus.Paid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (paymentStatus == PaymentStatus.Pending)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (paymentStatus == PaymentStatus.Expired)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine($"Payment Status: {paymentStatus}");
            Console.ResetColor();
            if (paymentStatus == PaymentStatus.Paid)
            {
                Console.WriteLine($"Payment Method: {paymentMethod}");
                Console.WriteLine($"Payment Reference: {paymentReference}");
            }
            else if (paymentStatus == PaymentStatus.Pending && paymentDeadline != default)
            {
                Program.DisplayPaymentDeadline(paymentDeadline);
            }
            if (discount > 0)
            {
                Console.WriteLine($"Discount: -₱{discount:F2}");
            }
            if (!string.IsNullOrWhiteSpace(adminNotes))
            {
                Console.WriteLine($"Admin Notes: {adminNotes}");
            }

            // Define column widths
            int timeWidth = 19;
            int movieWidth = 14;
            int scheduleWidth = 10;
            int seatsWidth = 14;
            int ticketWidth = 10;
            int itemsWidth = 28;
            int foodWidth = 10;
            int grandTotalWidth = 12;
            int idWidth = 12;

            // Prepare item lines
            List<string> itemLines = [];
            if (foodItems != null && foodItems.Count > 0)
            {
                itemLines.AddRange(foodItems.Select(kv => $"({kv.Value}) {kv.Key}"));
            }
            else
            {
                itemLines.Add("-");
            }

            // Print header
            int totalLineWidth =
                timeWidth
                + movieWidth
                + scheduleWidth
                + seatsWidth
                + ticketWidth
                + itemsWidth
                + foodWidth
                + grandTotalWidth
                + idWidth
                + (8 * 3);
            Console.WriteLine(new string('-', totalLineWidth));
            string header = string.Format(
                "{0,-"
                    + timeWidth
                    + "} | {1,-"
                    + movieWidth
                    + "} | {2,-"
                    + scheduleWidth
                    + "} | {3,-"
                    + seatsWidth
                    + "} | {4,-"
                    + ticketWidth
                    + "} | {5,-"
                    + itemsWidth
                    + "} | {6,-"
                    + foodWidth
                    + "} | {7,-"
                    + grandTotalWidth
                    + "} | {8,-"
                    + idWidth
                    + "}",
                "Time",
                "Movie",
                "Schedule",
                "Seats",
                "Ticket",
                "Items",
                "Food",
                "Grand Total",
                "ID"
            );
            Console.WriteLine(header);

            Console.WriteLine(new string('-', totalLineWidth));

            // Totals
            double ticketTotal = price * quantity;
            double grandTotal = GetGrandTotal();
            string ticketTotalStr = $"₱{ticketTotal:F2}";
            string foodTotalStr = $"₱{foodTotal:F2}";
            string grandTotalStr = $"₱{grandTotal:F2}";

            // Print data rows with padding
            string timeStr = reservationTime.ToString("g");
            string seatsStr = string.Join(",", seats);

            for (int i = 0; i < itemLines.Count; i++)
            {
                string timeCell = i == 0 ? timeStr : string.Empty;
                string movieCell = i == 0 ? movie : string.Empty;
                string scheduleCell = i == 0 ? schedule : string.Empty;
                string seatsCell = i == 0 ? seatsStr : string.Empty;
                string ticketCell = i == 0 ? ticketTotalStr : string.Empty;
                string foodCell = i == 0 ? foodTotalStr : string.Empty;
                string totalCell = i == 0 ? grandTotalStr : string.Empty;
                string idCell = i == 0 ? reservationID : string.Empty;

                string dataRow = string.Format(
                    "{0,-"
                        + timeWidth
                        + "} | {1,-"
                        + movieWidth
                        + "} | {2,-"
                        + scheduleWidth
                        + "} | {3,-"
                        + seatsWidth
                        + "} | {4,-"
                        + ticketWidth
                        + "} | {5,-"
                        + itemsWidth
                        + "} | {6,-"
                        + foodWidth
                        + "} | {7,-"
                        + grandTotalWidth
                        + "} | {8,-"
                        + idWidth
                        + "}",
                    timeCell,
                    movieCell,
                    scheduleCell,
                    seatsCell,
                    ticketCell,
                    itemLines[i],
                    foodCell,
                    totalCell,
                    idCell
                );
                Console.WriteLine(dataRow);
            }
            Console.WriteLine(new string('-', totalLineWidth));

            Console.WriteLine($"\nReservation Time: {reservationTime:g}");
            Console.WriteLine($"Last Modified: {lastModified:g}");
        }

        private static string GenerateReservationID()
        {
            Random rnd = new();
            return $"R-{rnd.Next(100000, 999999)}";
        }
    }

    // ---------- MOVIE ----------
    internal class Movie
    {
        private string movies;
        private List<Schedule> schedules;

        public string Movies
        {
            get { return movies; }
            set { movies = value; }
        }

        public List<Schedule> Schedules
        {
            get { return schedules; }
            set { schedules = value; }
        }

        public Movie(string movies)
        {
            this.movies = movies;
            schedules = [];
        }

        public static Movie SelectMovie(List<Movie> movies)
        {
            Console.Clear();
            Program.DrawTitle("NOW SHOWING");
            for (int i = 0; i < movies.Count; i++)
                Console.WriteLine($"{i + 1}. {movies[i].Movies}");
            int choice = InputValidator.ReadIntInRange(
                "\nSelect a movie (1-" + movies.Count + "): ",
                1,
                movies.Count
            );
            return movies[choice - 1];
        }
    }

    // ---------- SCHEDULE ----------
    internal class Schedule
    {
        private string time;
        private int price;

        public string Time
        {
            get { return time; }
            set { time = value; }
        }

        public int Price
        {
            get { return price; }
            set { price = value; }
        }

        public Schedule(string time, int price)
        {
            this.time = time;
            this.price = price;
        }

        public static Schedule SelectSchedule(Movie movie)
        {
            Console.Clear();
            Program.DrawTitle($"{movie.Movies} show times".ToUpper());
            for (int i = 0; i < movie.Schedules.Count; i++)
                Console.WriteLine(
                    $"{i + 1}. {movie.Schedules[i].Time} (₱{movie.Schedules[i].Price})"
                );
            int choice = InputValidator.ReadIntInRange(
                "\nSelect a schedule (1-" + movie.Schedules.Count + "): ",
                1,
                movie.Schedules.Count
            );
            return movie.Schedules[choice - 1];
        }
    }

    // ---------- SEAT ----------
    internal class Seat
    {
        private string seatNumber;
        private bool isAvailable;

        public string SeatNumber
        {
            get { return seatNumber; }
            set { seatNumber = value; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
            set { isAvailable = value; }
        }

        public Seat(string seatNumber)
        {
            this.seatNumber = seatNumber;
            this.isAvailable = true;
        }
    }

    // ---------- SEAT MANAGER ----------
    internal class SeatManager
    {
        private readonly List<Seat> seats;
        private readonly int rows;
        private readonly int cols;

        public List<Seat> Seats => seats;

        public SeatManager(int rows, int cols)
        {
            this.rows = rows;
            this.cols = cols;
            seats = [];

            for (int r = 1; r <= rows; r++)
                for (int c = 1; c <= cols; c++)
                    seats.Add(new Seat($"{(char)('A' + r - 1)}{c}"));
        }

        public List<string> SelectSeats(int quantity)
        {
            List<string> selectedSeats = [];

            while (selectedSeats.Count < quantity)
            {
                DisplaySeatsWithUserSelection(selectedSeats);
                string choice = InputValidator
                    .ReadNonEmptyLine($"\nSelect seat {selectedSeats.Count + 1}/{quantity}: ")
                    .ToUpper();

                Seat? seat = seats.Find(s => s.SeatNumber == choice);
                if (seat == null)
                {
                    int errorTop = Console.CursorTop + 1;
                    Console.SetCursorPosition(0, errorTop);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Seat does not exist. Try again." + new string(' ', 30));
                    Console.ResetColor();
                    Program.PlayErrorBeep();
                    Thread.Sleep(1200);
                    Console.SetCursorPosition(0, errorTop);
                    Console.Write(new string(' ', Console.BufferWidth));
                    continue;
                }

                if (!seat.IsAvailable)
                {
                    int errorTop = Console.CursorTop + 1;
                    Console.SetCursorPosition(0, errorTop);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Seat already taken. Choose another." + new string(' ', 30));
                    Console.ResetColor();
                    Program.PlayErrorBeep();
                    Thread.Sleep(1500);
                    Console.SetCursorPosition(0, errorTop);
                    Console.Write(new string(' ', Console.BufferWidth));
                    continue;
                }

                seat.IsAvailable = false;
                selectedSeats.Add(seat.SeatNumber);
            }

            DisplaySeatsWithUserSelection(selectedSeats);
            return selectedSeats;
        }

        private void DisplaySeats()
        {
            Console.Clear();
            Console.WriteLine(
                "Legend: [Green] Available | [Red] Sold/Unavailable | [Blue] Your Seats\n"
            );

            Console.WriteLine("================ SCREEN ================\n");

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Seat seat = seats[index++];
                    Console.ForegroundColor = seat.IsAvailable
                        ? ConsoleColor.Green
                        : ConsoleColor.Red;
                    Console.Write($"[{seat.SeatNumber}] ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }

        private void DisplaySeatsWithUserSelection(List<string> userSeats)
        {
            Console.Clear();
            Program.DrawTitle("SELECT SEATS");

            Console.WriteLine("\n================ SCREEN ================\n");

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Seat seat = seats[index++];
                    if (userSeats != null && userSeats.Contains(seat.SeatNumber))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else
                    {
                        Console.ForegroundColor = seat.IsAvailable
                            ? ConsoleColor.Green
                            : ConsoleColor.Red;
                    }
                    Console.Write($"[{seat.SeatNumber}] ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
            Console.WriteLine(
                "\nLegend: [Green] Available | [Red] Sold/Unavailable | [Blue] Your Seats"
            );

        }

        public void MarkSeatsAsUnavailable(List<string> bookedSeats)
        {
            foreach (string seatNumber in bookedSeats)
            {
                Seat? seat = seats.Find(s => s.SeatNumber == seatNumber);
                if (seat != null)
                    seat.IsAvailable = false;
            }
        }

        public void ReleaseSeats(List<string> seatNumbers)
        {
            if (seatNumbers == null)
                return;
            foreach (string seatNumber in seatNumbers)
            {
                Seat? seat = seats.Find(s => s.SeatNumber == seatNumber);
                if (seat != null)
                    seat.IsAvailable = true;

            }
        }

        public List<string> SelectSeatsForEdit(List<string> currentSeats, int quantity)
        {
            List<string> selectedSeats = [];
            List<string> remainingCurrentSeats = [.. currentSeats ?? []];

            while (selectedSeats.Count < quantity)
            {
                List<string> highlightSeats = [.. remainingCurrentSeats];
                highlightSeats.AddRange(selectedSeats);
                DisplaySeatsForEdit(highlightSeats);
                string choice = InputValidator
                    .ReadNonEmptyLine($"\nSelect seat {selectedSeats.Count + 1}/{quantity}: ")
                    .ToUpper();

                Seat? seat = seats.Find(s => s.SeatNumber == choice);
                if (seat == null)
                {
                    int errorTop = Console.CursorTop + 1;
                    Console.SetCursorPosition(0, errorTop);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Seat does not exist. Try again." + new string(' ', 30));
                    Console.ResetColor();
                    Program.PlayErrorBeep();
                    Thread.Sleep(1500);
                    Console.SetCursorPosition(0, errorTop);
                    Console.Write(new string(' ', Console.BufferWidth));
                    continue;
                }

                if (!seat.IsAvailable && !remainingCurrentSeats.Contains(choice))
                {
                    int errorTop = Console.CursorTop + 1;
                    Console.SetCursorPosition(0, errorTop);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Seat already taken. Choose another." + new string(' ', 30));
                    Console.ResetColor();
                    Program.PlayErrorBeep();
                    Thread.Sleep(1500);
                    Console.SetCursorPosition(0, errorTop);
                    Console.Write(new string(' ', Console.BufferWidth));
                    continue;
                }

                // If it's a current seat, remove it from remainingCurrentSeats
                if (remainingCurrentSeats.Contains(choice))
                {
                    remainingCurrentSeats.Remove(choice);
                    seat.IsAvailable = true;
                }
                else
                {
                    seat.IsAvailable = false;
                }
                selectedSeats.Add(seat.SeatNumber);
            }

            DisplaySeatsForEdit([.. selectedSeats]);
            return selectedSeats;
        }

        public List<string> SelectAdditionalSeats(int additionalCount)
        {
            List<string> selectedSeats = [];
            while (selectedSeats.Count < additionalCount)
            {
                DisplaySeatsWithUserSelection(selectedSeats);
                string choice = InputValidator
                    .ReadNonEmptyLine(
                        $"\nSelect additional seat {selectedSeats.Count + 1}/{additionalCount}: "
                    )
                    .ToUpper();

                Seat? seat = seats.Find(s => s.SeatNumber == choice);
                if (seat == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Seat does not exist. Try again.");
                    Console.ResetColor();
                    Thread.Sleep(700);
                    continue;
                }

                if (!seat.IsAvailable)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Seat already taken. Choose another.");
                    Console.ResetColor();
                    Program.PlayErrorBeep();
                    Thread.Sleep(700);
                    continue;
                }

                seat.IsAvailable = false;
                selectedSeats.Add(seat.SeatNumber);
            }

            DisplaySeatsWithUserSelection(selectedSeats);
            return selectedSeats;
        }

        private void DisplaySeatsForEdit(List<string> userSeats)
        {
            Console.Clear();
            Console.WriteLine(
                "Legend: [Green] Available | [Red] Sold/Unavailable | [Blue] Your Seats\n"
            );

            Console.WriteLine("================ SCREEN ================\n");

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Seat seat = seats[index++];
                    if (userSeats.Contains(seat.SeatNumber))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else if (seat.IsAvailable)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.Write($"[{seat.SeatNumber}] ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
    }

    // ---------- DATA MANAGER ----------
    internal static class DataManager
    {
        private static readonly string filePath = "bookings.json";

        public static List<Booking> LoadBookings()
        {
            try
            {
                if (!File.Exists(filePath))
                    return [];

                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                    return [];

                return JsonSerializer.Deserialize<List<Booking>>(json) ?? [];
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Warning: Could not load bookings file. Starting fresh. Error: {ex.Message}"
                );
                Console.ResetColor();
                Thread.Sleep(2000);
                return [];
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error loading bookings: {ex.Message}");
                Console.ResetColor();
                Thread.Sleep(2000);
                return [];
            }
        }

        public static void SaveBookings(List<Booking> bookings)
        {
            try
            {
                JsonSerializerOptions options = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(bookings, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving bookings: {ex.Message}");
                Console.ResetColor();
                Thread.Sleep(2000);
            }
        }
    }

    // ---------- FOOD INVENTORY DOMAIN ----------
    internal class FoodInventory
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Food" or "Beverage"
        public double Price { get; set; }
        public int Stock { get; set; }
        public int ReorderLevel { get; set; }
        public int TotalSold { get; set; }
    }

    internal static class InventoryManager
    {
        private static readonly string inventoryPath = "inventory.json";
        private static List<FoodInventory> cached = [];

        public static List<FoodInventory> LoadInventory()
        {
            try
            {
                if (File.Exists(inventoryPath))
                {
                    string json = File.ReadAllText(inventoryPath);
                    cached =
                        JsonSerializer.Deserialize<List<FoodInventory>>(json)
                        ?? BuildDefaultInventory();
                }
                else
                {
                    cached = BuildDefaultInventory();
                    SaveInventory(cached);
                }
            }
            catch
            {
                cached = BuildDefaultInventory();
                SaveInventory(cached);
            }
            return cached;
        }

        public static void SaveInventory(List<FoodInventory> inventory)
        {
            try
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                File.WriteAllText(inventoryPath, JsonSerializer.Serialize(inventory, opts));
                cached = inventory;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving inventory: {ex.Message}");
                Console.ResetColor();
            }
        }

        public static FoodInventory? GetInventoryItem(string name)
        {
            List<FoodInventory> list = cached ?? LoadInventory();

            return list.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        }

        public static void UpdateStock(string name, int quantity, bool isAddition)
        {
            List<FoodInventory> list = cached ?? LoadInventory();
            FoodInventory? item = list.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
            if (item == null)
                return;

            if (isAddition)
            {
                item.Stock += quantity;
            }
            else
            {
                item.Stock = Math.Max(0, item.Stock - quantity);
            }
            SaveInventory(list);
        }

        public static void RecordSale(Dictionary<string, int> foodItems)
        {
            if (foodItems == null || foodItems.Count == 0)
                return;
            List<FoodInventory> list = cached ?? LoadInventory();
            foreach (KeyValuePair<string, int> kvp in foodItems)
            {
                FoodInventory? item = list.FirstOrDefault(i =>
                    i.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)
                );
                if (item == null)
                    continue;
                int qty = Math.Max(0, kvp.Value);
                item.Stock = Math.Max(0, item.Stock - qty);
                item.TotalSold += qty;
            }
            SaveInventory(list);
        }

        public static void RestoreSale(Dictionary<string, int> foodItems)
        {
            if (foodItems == null || foodItems.Count == 0)
                return;
            List<FoodInventory> list = cached ?? LoadInventory();
            foreach (KeyValuePair<string, int> kvp in foodItems)
            {
                FoodInventory? item = list.FirstOrDefault(i =>
                    i.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)
                );
                if (item == null)
                    continue;
                int qty = Math.Max(0, kvp.Value);
                item.Stock += qty; // ADD back to stock
                item.TotalSold = Math.Max(0, item.TotalSold - qty); // SUBTRACT from total sold
            }
            SaveInventory(list);
        }

        private static List<FoodInventory> BuildDefaultInventory()
        {
            return
            [
                new FoodInventory
                {
                    Name = "Popcorn Regular",
                    Category = "Food",
                    Price = 85.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Popcorn Large",
                    Category = "Food",
                    Price = 120.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Popcorn Paper Bucket",
                    Category = "Food",
                    Price = 140.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Classic Hotdog",
                    Category = "Food",
                    Price = 95.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Chicken Nuggets",
                    Category = "Food",
                    Price = 140.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Classic Fries",
                    Category = "Food",
                    Price = 60.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Cheese Fries",
                    Category = "Food",
                    Price = 75.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "BBQ Fries",
                    Category = "Food",
                    Price = 75.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Sour Cream Fries",
                    Category = "Food",
                    Price = 75.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Regular Soda",
                    Category = "Beverage",
                    Price = 45.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Large Soda",
                    Category = "Beverage",
                    Price = 80.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Bottled Water",
                    Category = "Beverage",
                    Price = 35.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Iced Tea",
                    Category = "Beverage",
                    Price = 45.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
                new FoodInventory
                {
                    Name = "Iced Coffee",
                    Category = "Beverage",
                    Price = 50.00,
                    Stock = 100,
                    ReorderLevel = 20,
                    TotalSold = 0,
                },
            ];
        }
    }
}