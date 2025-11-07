# Domain Extension: Customers & Restaurants

This update introduces `Customer` and `Restaurant` entities and links `Order` to both via foreign keys.

## Changes
- **Models**: `Customer`, `Restaurant`, updated `Order` (now has `CustomerId`, `RestaurantId`, nav props)
- **DTOs**: Customer & Restaurant create/read DTOs; Order DTOs updated
- **Repositories/Services**: Added for Customer and Restaurant; Order repository now includes related entities
- **Controllers**: `CustomersController`, `RestaurantsController` (CRUD). `OrdersController` expects `CreateOrderDto` with `CustomerId` and `RestaurantId`.
- **DbContext**: Added DbSets and relationships (restrict deletes). Unique index on `Customer.Email`.

## Migrations
Run these from the project folder (`OrderManagement/OrderManagement`):

```bash
dotnet tool restore
dotnet ef migrations add AddCustomerAndRestaurant
dotnet ef database update
```

## Sample Requests
```
POST /api/customers
{ "name":"Alice", "email":"alice@example.com", "phone":"12345" }

POST /api/restaurants
{ "name":"Pasta Place", "address":"Main St 1", "phone":"555-123" }

POST /api/orders
{ "customerId": 1, "restaurantId": 1, "totalPrice": 29.95 }

GET /api/orders
```