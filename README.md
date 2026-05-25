# Modular Multi-Tenant E-Shop
Αυτό είναι ένα σύγχρονο E-shop βασισμένο στις αρχές της **Clean Architecture** και του **Multi-tenancy** (Database-per-Tenant) με τη χρήση .NET και PostgreSQL.

## 🏛️ Αρχιτεκτονική
- **Eshop.Core**: Entities και Interfaces (Domain Layer).
- **Eshop.Application**: Business Logic και Services.
- **Eshop.Infrastructure**: Entity Framework Core, Repositories και διαχείριση PostgreSQL.
- **Eshop.API**: REST Endpoints και Swagger UI.

## 🚀 Πώς να το τρέξετε
1. Βεβαιωθείτε ότι έχετε εγκατεστημένη την PostgreSQL.
2. Ρυθμίστε το Connection String στο `appsettings.json`.
3. Ανοίξτε την Package Manager Console και τρέξτε `Update-Database` για τη Master βάση.
4. Εκκινήστε το project `Eshop.API`.