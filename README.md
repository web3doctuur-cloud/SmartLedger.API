# SmartLedger API

A modern .NET 10 Web API for smart ledger and inventory management with PostgreSQL, JWT authentication, and Swagger documentation.

## Features

- 📝 **Double-Entry Accounting**: Full journal entry management with proper validation
- 📦 **Inventory Management**: Track products and inventory transactions
- 🔐 **JWT Authentication**: Secure API access with ASP.NET Core Identity
- 📊 **Reports**: Generate financial reports (trial balance, profit & loss)
- 📄 **Export**: Export data to Excel and CSV formats
- 🎯 **RESTful API**: Clean, well-structured endpoints with Swagger/OpenAPI docs

## Tech Stack

- **.NET 10.0**
- **ASP.NET Core Web API**
- **Entity Framework Core** with PostgreSQL
- **ASP.NET Core Identity**
- **JWT Bearer Authentication**
- **Swagger/OpenAPI**
- **EPPlus** (Excel export)
- **CsvHelper** (CSV export)

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL server (or Neon PostgreSQL for cloud hosting)
- IDE (Visual Studio, VS Code, or Rider)

### Installation

1. Clone the repo:
   ```bash
   git clone https://github.com/web3doctuur-cloud/SmartLedger.API.git
   cd SmartLedger
   ```

2. Set up user secrets or environment variables:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=SmartLedgerDb;Username=postgres;Password=your-password"
     },
     "JwtSettings": {
       "Secret": "YourSuperSecretKeyHereAtLeast32Characters",
       "Issuer": "smartledger-api",
       "Audience": "smartledger-frontend",
       "ExpiryMinutes": 60
     }
   }
   ```

3. Run database migrations:
   ```bash
   cd SmartLedger.API
   dotnet ef database update
   ```

4. Run the API:
   ```bash
   dotnet run
   ```

5. Open Swagger UI at: `http://localhost:5173/swagger/index.html`

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and get JWT token

### Accounts

- `GET /api/accounts` - Get all active accounts
- `GET /api/accounts/{id}` - Get account by ID
- `POST /api/accounts` - Create a new account
- `PUT /api/accounts/{id}` - Update an account
- `DELETE /api/accounts/{id}` - Deactivate an account

### Journal Entries

- `GET /api/JournalEntries` - Get all journal entries
- `GET /api/JournalEntries/{id}` - Get journal entry by ID
- `POST /api/JournalEntries` - Create a new journal entry
- `POST /api/JournalEntries/{id}/approve` - Approve a journal entry
- `DELETE /api/JournalEntries/{id}` - Deactivate a journal entry

### Products

- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create a new product
- `PUT /api/products/{id}` - Update a product
- `DELETE /api/products/{id}` - Deactivate a product

### Inventory

- `GET /api/inventory/transactions` - Get all inventory transactions
- `POST /api/inventory/transactions` - Create a new inventory transaction

### Reports

- `GET /api/reports/trial-balance` - Get trial balance report
- `GET /api/reports/profit-loss` - Get profit & loss report

### Export

- `GET /api/export/products/excel` - Export products to Excel
- `GET /api/export/products/csv` - Export products to CSV
- `GET /api/export/journal-entries/excel` - Export journal entries to Excel
- `GET /api/export/journal-entries/csv` - Export journal entries to CSV

## Environment Variables (Production)

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=your-neon-connection-string
JwtSettings__Secret=your-32+character-secret
JwtSettings__Issuer=smartledger-api
JwtSettings__Audience=smartledger-frontend
JwtSettings__ExpiryMinutes=60
PORT=10000
```

## Deployment

This API is deployed on Render with PostgreSQL hosted on Neon.

### Render Configuration

- **Build Command**: `dotnet publish -c Release -o out`
- **Start Command**: `dotnet out/SmartLedger.API.dll`
- **Runtime**: .NET 10
- **Region**: Choose your preferred region

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## License

MIT License

## Author

SmartLedger Team
