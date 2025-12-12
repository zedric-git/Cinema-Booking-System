# 🎬 Cinema Booking System

A feature-rich console-based cinema ticket booking and management application built with C# and .NET. This system provides comprehensive functionality for both customers and cinema administrators, including real-time seat selection, food ordering with inventory management, multiple payment methods, and detailed sales reporting.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
  - [Customer Features](#customer-features)
  - [Admin Features](#admin-features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
  - [Customer Guide](#customer-guide)
  - [Admin Guide](#admin-guide)
- [Project Structure](#project-structure)
- [Technologies Used](#technologies-used)
- [Contributing](#contributing)
- [License](#license)

## 🎯 Overview

The Cinema Booking System is a comprehensive console application that simulates a complete movie theater booking and management experience. The system features a dual-interface design: a customer-facing booking system and an administrative dashboard for business management.

**Key Highlights:**
- Real-time seat availability tracking across multiple movies and showtimes
- Integrated food ordering system with live inventory management
- Multiple payment methods (Cash, GCash, PayMaya, Credit/Debit Card)
- 15-minute payment deadline for pending bookings with auto-expiration
- Complete admin dashboard with sales reports, inventory management, and reservation oversight
- JSON-based data persistence for bookings and inventory
- Secure passkey system for reservation access

## ✨ Features

### Customer Features

#### 🎫 **Booking Management**
- **New Reservation**: Create bookings with movie selection, showtime, seat selection, and food ordering
- **Edit Reservation**: Modify existing bookings (movie, schedule, seats, quantity, food) with passkey authentication
- **Complete Pending Payment**: Pay for previously reserved bookings before the deadline expires
- **Booking Summary**: Detailed confirmation with reservation ID, selected seats, food items, and total cost

#### 🎬 **Movie & Showtime Selection**
- Browse available movies (Heneral Luna, Conjuring V, Encanto)
- Multiple showtimes per movie with dynamic pricing
- Schedule-specific seat availability

#### 💺 **Interactive Seat Selection**
- Visual seating chart (5 rows × 8 columns, A1-E8)
- Color-coded display:
  - **Green**: Available seats
  - **Red**: Sold/Unavailable seats
  - **Blue**: Your selected seats
- Select 1-5 tickets per booking
- Real-time seat availability updates

#### 🍿 **Food & Beverage Ordering**
- **Food Items**: Popcorn (Regular/Large/Bucket), Hotdog, Chicken Nuggets, Fries (Classic/Cheese/BBQ/Sour Cream)
- **Beverages**: Soda (Regular/Large), Bottled Water, Iced Tea, Iced Coffee
- Arrow-key navigation for quantity selection
- Real-time total calculation
- Add/modify food orders when editing reservations

#### 💳 **Payment System**
- **Multiple Payment Methods**:
  - Cash at Counter (with change calculation)
  - GCash (with QR code display and reference number)
  - PayMaya (with QR code display and reference number)
  - Credit/Debit Card (with masked input)
  - Pay Later (15-minute deadline)
- **Payment Deadline**: 15-minute countdown for pending payments
- **Auto-Expiration**: Bookings expire and seats are released after deadline
- **Receipt Generation**: Detailed receipt with payment reference

#### 🔐 **Security & Authentication**
- Passkey protection for all reservations
- Masked passkey input
- Reservation ID system (R-XXXXXX format)

### Admin Features

*Access the admin panel by typing `admin` at the main menu*

**Login Credentials:**
- Username: `admin`
- Password: `admin123`

#### 📊 **Dashboard Overview**
- Today's sales total
- Pending payments count
- Low stock items alert
- Total reservations count

#### 📈 **Sales Reports**
- **Daily Report**: Sales breakdown for a specific date
- **Weekly Report**: 7-day sales with daily chart visualization
- **Monthly Report**: Full month analysis with:
  - Top-selling movie
  - Top-selling food item
  - Weekly breakdown
- **Export Functionality**: Save reports as text files
- **Detailed Metrics**: Ticket sales, food sales, transaction count, grand totals

#### 📦 **Inventory Management**
- **View Current Stock**: Color-coded stock levels (Red: Critical, Yellow: Low, Green: Good)
- **Add Stock**: Replenish inventory with logging
- **Remove Stock**: Reduce inventory with reason tracking (expired/damaged/other)
- **Low Stock Alerts**: Automatic warnings when items fall below reorder level
- **Stock Tracking**: Monitor quantities, prices, and total units sold
- **Real-time Updates**: Stock automatically deducted on payment and restored on cancellation

**Default Stock Items** (100 units each, reorder level: 20):
- 9 Food items (₱60-₱140)
- 5 Beverage items (₱35-₱80)

#### 🎟️ **Reservation Management**
- **View All Bookings**: Paginated list with status filtering
- **View by Status**: Filter by Paid, Pending, or Expired
- **Admin Edit Override**: Modify any reservation without passkey
  - Change movie, schedule, seats, quantity, food order
  - Update payment status
  - Apply discounts
  - Add admin notes
- **Admin Cancellation**: Cancel bookings with automatic refund notification
- **Detailed Booking Views**: Comprehensive information per reservation

#### 🔧 **Additional Admin Tools**
- **Admin Logging**: All admin actions logged to `admin_log.txt`
- **Refund Tracking**: Automatic refund notices for cancelled paid bookings
- **Stock Restoration**: Automatic inventory restoration on booking cancellation
- **Payment Override**: Manually mark bookings as paid

## 🛠️ Prerequisites

Before running this project, ensure you have the following installed:

- **[.NET SDK](https://dotnet.microsoft.com/download)** (Version 6.0 or higher)
  - Check your version: `dotnet --version`
- **IDE/Text Editor** (Choose one):
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community Edition or higher)
  - [Visual Studio Code](https://code.visualstudio.com/) with C# extension
  - [JetBrains Rider](https://www.jetbrains.com/rider/)
- **Operating System**: Windows (recommended for Console.Beep functionality), macOS, or Linux

## 📦 Installation

Follow these steps to set up the project on your local machine:

1. **Clone the Repository**
   ```bash
   git clone https://github.com/zedric-git/Cinema-Booking-System.git
   ```

2. **Navigate to the Project Directory**
   ```bash
   cd Cinema-Booking-System
   cd "Final Project"
   ```

3. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the Project**
   ```bash
   dotnet build
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

### Alternative: Using Visual Studio

1. Open the solution file (`.sln`) or project file (`.csproj`) in Visual Studio
2. Press `F5` or click the "Start" button to run

## 🚀 Usage

### Customer Guide

#### Starting the Application

When you launch the application, you'll see an animated splash screen:
```
╔═══════════════════════════════════════╗
║  CINEMA BOOKING SYSTEM                ║
╚═══════════════════════════════════════╝

 Press any key to enter!
```

#### Main Menu

```
╔══════════════════════════════════════╗
║            MAIN MENU                 ║
╠══════════════════════════════════════╣
║  [1] New Reservation                 ║
║  [2] Edit Existing Reservation       ║
║  [3] Complete Pending Payment        ║
║  [4] Exit                            ║
╚══════════════════════════════════════╝

Select an option:
```

#### Creating a New Reservation

1. **Select Movie**
   ```
   ╔══════════════════════════════════════╗
   ║         NOW SHOWING                  ║
   ╚══════════════════════════════════════╝
   1. Heneral Luna
   2. Conjuring V
   3. Encanto
   
   Select a movie (1-3):
   ```

2. **Choose Showtime** (prices vary by schedule)
   ```
   1. 12:30 PM (₱250)
   2. 4:00 PM (₱260)
   3. 7:30 PM (₱270)
   ```

3. **Select Number of Tickets** (1-5)
   - Use ← → arrow keys to adjust quantity
   - Press ENTER to confirm

4. **Pick Your Seats**
   ```
   ================ SCREEN ================
   
   [A1] [A2] [A3] [A4] [A5] [A6] [A7] [A8]
   [B1] [B2] [B3] [B4] [B5] [B6] [B7] [B8]
   [C1] [C2] [C3] [C4] [C5] [C6] [C7] [C8]
   [D1] [D2] [D3] [D4] [D5] [D6] [D7] [D8]
   [E1] [E2] [E3] [E4] [E5] [E6] [E7] [E8]
   
   Legend: [Green] Available | [Red] Sold/Unavailable | [Blue] Your Seats
   
   Select seat 1/2: A5
   ```

5. **Order Food & Beverages**
   - Use ↑ ↓ arrows to navigate items
   - Use ← → arrows to adjust quantities
   - Press ENTER to move to next item or finish

6. **Create Passkey**
   ```
   Create your passkey: ****
   Confirm passkey: ****
   ```
   - Passkey is required to edit/view your reservation
   - Input is masked for security

7. **Review & Confirm**
   - View complete booking summary
   - Choose Cancel [X] or Confirm [Y]

8. **Payment**
   - Select payment method
   - For Cash: Enter tendered amount, receive change
   - For E-Wallets: Scan QR code, enter reference number
   - For Card: Enter card details (masked)
   - For Pay Later: 15-minute deadline is set

9. **Receive Receipt**
   - Reservation ID (e.g., R-123456)
   - Payment reference
   - Complete booking details

#### Editing a Reservation

1. Enter your Reservation ID (e.g., R-123456)
2. Enter your Passkey (masked input)
3. Choose what to edit:
   - [1] Change Movie
   - [2] Change Schedule
   - [3] Change Seat(s)
   - [4] Change Ticket Quantity
   - [5] Change Food Order
   - [6] Save Edit
   - [7] Cancel Booking

**Note**: Paid reservations can only be cancelled, not edited.

#### Completing Pending Payment

1. View list of all pending bookings
2. Enter your Reservation ID
3. Enter your Passkey
4. Check remaining time before deadline
5. Complete payment using any method
6. Receive updated receipt

**Important**: Bookings expire 15 minutes after creation if payment is not completed. Expired bookings automatically release seats.

### Admin Guide

#### Accessing Admin Panel

At the main menu, type: `admin`

**Login:**
- Username: `admin`
- Password: `admin123`
- 3 attempts allowed

#### Admin Dashboard

```
╔════════════════════════════════════════════════╗
║              ADMIN DASHBOARD                   ║
╠════════════════════════════════════════════════╣
║ Today's Sales: ₱5,450.00                       ║
║ Pending Payments: 3                            ║
║ Low Stock Items: 2                             ║
║ Total Reservations: 47                         ║
╠════════════════════════════════════════════════╣
║ [1] View Sales Reports                         ║
║ [2] Manage Stock Inventory                     ║
║ [3] View Reservations                          ║
║ [4] Logout                                     ║
╚════════════════════════════════════════════════╝
```

#### Sales Reports

**Options:**
- [1] Daily Report - Sales for specific date
- [2] Weekly Report - 7-day analysis with daily chart
- [3] Monthly Report - Full month with top sellers
- [4] Back
- [5] Export to Text File

**Report Contents:**
- Movie sales (quantity and amount)
- Food & beverage sales
- Transaction count
- Grand totals
- Visual charts (weekly/monthly)

#### Stock Inventory

**Options:**
- [1] View Current Stock - Color-coded stock levels
- [2] Add Stock - Replenish inventory
- [3] Remove Stock - Record wastage/damage
- [4] View Low Stock Alert - Items below reorder level
- [5] Back

**Stock Display:**
```
Category   | Item                     | Stock | Sold | Price
═══════════════════════════════════════════════════════════
Food       | Popcorn Regular          |   45  |  55  | ₱  85.00
Food       | Classic Hotdog           |   12  |  88  | ₱  95.00  ← Low Stock (Red)
Beverage   | Regular Soda             |   67  |  33  | ₱  45.00
```

#### Reservation Management

**Options:**
- [1] View All Bookings
- [2] View Pending Payments
- [3] View Paid Reservations
- [4] View Expired Reservations
- [5] Edit Reservation (Admin Override)
- [6] Cancel Reservation (Admin)
- [7] Back

**Admin Edit Features:**
- No passkey required
- Can edit any booking (including paid ones)
- Update payment status manually
- Apply discounts
- Add admin notes
- Full cancellation with refund tracking

## 📂 Project Structure

```
Cinema-Booking-System/
├── Final Project/
│   ├── Program.cs                    # Main application with all classes
│   ├── bookings.json                 # Persistent booking data (auto-generated)
│   ├── inventory.json                # Food inventory data (auto-generated)
│   ├── admin_log.txt                 # Admin action logs (auto-generated)
│   └── Cinema-Booking-System.csproj  # Project configuration
└── README.md
```

### Code Architecture

The application is structured into the following main components within `Program.cs`:

**Core Classes:**
- `Program` - Main entry point and UI orchestration
- `Movie` - Movie entity with schedules
- `Schedule` - Showtime and pricing information
- `Booking` - Complete reservation details
- `Seat` - Individual seat representation
- `SeatManager` - Seat availability management per showtime

**Food System:**
- `FoodItem` - Individual food item with pricing
- `FoodOrder` - Customer food selection
- `FoodInventory` - Inventory tracking entity
- `InventoryManager` - Stock management and persistence

**Payment System:**
- `PaymentProcessor` - Handles all payment methods
- `PaymentResult` - Payment transaction details

**Admin System:**
- `AdminConsoleUI` - Admin panel interface
- `SalesReportManager` - Report generation (daily/weekly/monthly)
- `InventoryConsoleUI` - Stock management interface
- `AdminReservationManager` - Booking oversight
- `AdminLogger` - Activity logging
- `DashboardSummary` - Dashboard metrics calculator

**Utilities:**
- `DataManager` - JSON serialization for bookings
- `InputValidator` - Input validation and masking

**Enums:**
- `PaymentStatus` - Pending, Paid, Cancelled, Expired

## 💻 Technologies Used

- **Language**: C# 10.0+
- **Framework**: .NET 6.0+
- **Data Storage**: JSON file-based persistence
- **Standard Libraries**:
  - `System.Text.Json` - JSON serialization
  - `System.Text` - String manipulation
  - `System.IO` - File operations

### Key Programming Concepts Demonstrated

**Object-Oriented Programming:**
- Encapsulation with properties and getters/setters
- Class relationships and composition
- Enum types for status management

**Data Structures:**
- Lists for collections (movies, bookings, seats)
- Dictionaries for food orders and seat tracking
- HashSets for reference uniqueness

**Design Patterns:**
- Static utility classes (DataManager, InventoryManager, PaymentProcessor)
- Manager pattern (SeatManager, InventoryManager)
- Result object pattern (PaymentResult)

**Advanced Features:**
- JSON serialization/deserialization
- Masked console input
- Color-coded console output
- Loading animations
- Audio feedback (Console.Beep)
- Date/time handling with deadlines
- File I/O operations
- LINQ queries for data filtering

**Console UI Techniques:**
- Box-drawing characters (╔═╗║╚╝╠╣)
- Cursor positioning for animations
- Color-coded status displays
- Interactive keyboard navigation (arrow keys)
- Loading spinners

## 🔧 Data Files

The application generates and manages three data files:

### bookings.json
Stores all customer reservations with complete details:
```json
[
  {
    "Movie": "Heneral Luna",
    "Schedule": "7:30 PM",
    "Price": 270,
    "Quantity": 2,
    "Seats": ["A5", "A6"],
    "ReservationID": "R-123456",
    "Passkey": "secret",
    "ReservationTime": "2024-12-12T14:30:00",
    "LastModified": "2024-12-12T14:35:00",
    "FoodItems": {
      "Popcorn Large": 2,
      "Regular Soda": 2
    },
    "FoodTotal": 330.0,
    "PaymentStatus": 1,
    "PaymentMethod": "Cash",
    "PaymentReference": "CASH-123456",
    "PaymentDeadline": "2024-12-12T14:45:00",
    "AmountPaid": 870.0,
    "Discount": 0.0,
    "AdminNotes": ""
  }
]
```

### inventory.json
Tracks food item stock levels:
```json
[
  {
    "Name": "Popcorn Regular",
    "Category": "Food",
    "Price": 85.0,
    "Stock": 45,
    "ReorderLevel": 20,
    "TotalSold": 55
  }
]
```

### admin_log.txt
Logs all administrative actions:
```
[12/12/2024 02:30 PM] Added 50 units to Popcorn Regular
[12/12/2024 02:45 PM] Admin changed schedule for reservation R-123456
[12/12/2024 03:00 PM] Booking R-789012 cancelled - Refund: ₱1,250.00
```

## ⚙️ Configuration

### Modifying Movies and Schedules

Edit in `Program.cs` Main method (lines ~45-55):
```csharp
List<Movie> movies = [new("Heneral Luna"), new("Conjuring V"), new("Encanto")];

movies[0].Schedules.AddRange([
    new("12:30 PM", 250), 
    new("4:00 PM", 260), 
    new("7:30 PM", 270)
]);
```

### Adjusting Theater Size

Modify SeatManager initialization (line ~68):
```csharp
seatManagers[key] = new SeatManager(5, 8); // 5 rows, 8 columns
```

### Changing Payment Deadline

Edit in CreateNewReservation method (line ~244):
```csharp
booking.PaymentDeadline = DateTime.Now.AddMinutes(15); // 15-minute deadline
```

### Admin Credentials

Modify in AdminConsoleUI.RunLogin method (line ~1042):
```csharp
if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) 
    && password == "admin123")
```

### Food Menu & Pricing

Edit in FoodOrder.SelectFood method (lines ~1387-1400):
```csharp
List<FoodItem> foodItems = [
    new FoodItem("Popcorn Regular", 85.00),
    new FoodItem("Popcorn Large", 120.00),
    // ... add or modify items
];
```

## 🎨 User Experience Features

- **Visual Feedback**: Color-coded displays (Green/Yellow/Red) for status and availability
- **Audio Cues**: Beep sounds for success, errors, and warnings (Windows only)
- **Loading Animations**: Spinner animations for processing
- **Masked Input**: Secure passkey and payment card entry
- **Interactive Navigation**: Arrow key controls for quantity selection and menus
- **Real-time Updates**: Instant seat availability and stock level changes
- **Countdown Timers**: Payment deadline tracking with color-coded warnings
- **Professional Layout**: Box-drawing characters for clean, organized displays

## 🐛 Known Limitations

- **Console.Beep**: Audio feedback only works on Windows systems
- **Single User Session**: No concurrent user support (file locking not implemented)
- **No Database**: Uses JSON files instead of a database system
- **Fixed Movie List**: Movies must be modified in code, not through admin panel
- **Limited Validation**: Some edge cases in input validation may exist
- **No Email/SMS**: Booking confirmations are console-only
- **No Rollback**: Payment transactions cannot be automatically reversed
- **Time-based**: System relies on local machine time for deadlines

## 🔜 Future Enhancements

**Potential Features:**
- Database integration (SQL Server, PostgreSQL, SQLite)
- Movie poster display with ASCII art
- Customer loyalty program with discounts
- Gift card/voucher system
- Group booking discounts
- Online payment gateway integration
- Email/SMS confirmation system
- Seat type differentiation (Regular/VIP/Couple seats)
- 3D/IMAX surcharges
- Mobile app version
- Web-based admin dashboard
- Automated stock reordering
- Customer feedback system
- Movie ratings and reviews
- Trailer information display

## 🤝 Contributing

Contributions are welcome! If you'd like to improve this project:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Contribution Guidelines

- Follow existing code style and naming conventions
- Add comments for complex logic
- Test thoroughly before submitting
- Update README if adding new features
- Keep commits focused and descriptive

### Areas for Contribution

- Bug fixes and error handling improvements
- Additional payment methods
- Enhanced reporting features
- Database integration
- Unit tests implementation
- Performance optimization
- UI/UX improvements
- Documentation enhancements

## 📝 License

This project is open source and available under the [MIT License](LICENSE).

## 👨‍💻 Author

**Zedric Camilotes**
- GitHub: [@zedric-git](https://github.com/zedric-git)
- Repository: [Cinema-Booking-System](https://github.com/zedric-git/Cinema-Booking-System)

## 🙏 Acknowledgments

- Created as a comprehensive final project demonstrating C# and .NET proficiency
- Implements real-world business logic for cinema operations
- Showcases full-stack console application development
- Demonstrates understanding of data persistence and state management

## 📞 Support & Troubleshooting

### Common Issues

**Issue**: Bookings.json file corrupted
- **Solution**: Delete the file and restart - it will regenerate with default values

**Issue**: Console colors not displaying
- **Solution**: Use Windows Terminal or a terminal that supports ANSI colors

**Issue**: Beep sounds not working
- **Solution**: This is expected on non-Windows systems; functionality is unaffected

**Issue**: Payment deadline showing negative time
- **Solution**: System time may have changed; booking will auto-expire on next access

### Getting Help

1. Check [Issues](https://github.com/zedric-git/Cinema-Booking-System/issues) for existing problems
2. Open a new issue with:
   - Detailed description
   - Steps to reproduce
   - Expected vs actual behavior
   - System information (.NET version, OS)

## 📚 Learning Resources

This project demonstrates concepts useful for learning:
- Console application development in C#
- JSON serialization and file I/O
- State management across sessions
- Event-driven programming
- Business logic implementation
- User authentication basics
- Inventory management systems
- Report generation
- Date/time handling

---

**Project Status**: ✅ Complete and Functional

**Made with ❤️ for learning and demonstration purposes**
