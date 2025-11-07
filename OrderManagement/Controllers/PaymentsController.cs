using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderManagement.Data;
using OrderManagement.DTOs;
using OrderManagement.Models;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public PaymentsController(AppDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

       

        // Re-enable [Authorize] after JWT is aligned
        // [Authorize]
        [AllowAnonymous]
        [HttpPost("checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutDto dto)
        {
            var currency = _cfg["Stripe:Currency"] ?? "usd";

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            if (order.PaidAt != null)
                return BadRequest(new { message = "Order already paid" });

            var lineItems = (order.OrderItems ?? new List<OrderItem>())
                .Select(oi => new SessionLineItemOptions
                {
                    Quantity = oi.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)Math.Round(oi.UnitPrice * 100m),
                        Currency = currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = oi.NameSnapshot ??  "Item"
                        }
                    }
                }).ToList();

            if (lineItems.Count == 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)Math.Round(order.TotalPrice * 100m),
                        Currency = currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Order #{order.Id}"
                        }
                    }
                });
            }

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                LineItems = lineItems,
                SuccessUrl = dto.SuccessUrl ?? $"{Request.Scheme}://{Request.Host}/checkout/success?orderId={order.Id}",
                CancelUrl = dto.CancelUrl ?? $"{Request.Scheme}://{Request.Host}/checkout/cancel?orderId={order.Id}",

                // Let the webhook map back to your order
                ClientReferenceId = order.Id.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString()
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            Console.WriteLine($"[PAY] Created checkout session {session.Id} for order {order.Id}");

            return Ok(new { url = session.Url });
        }

        // Webhook is anonymous but signature-verified
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            var endpointSecret = _cfg["Stripe:WebhookSecret"];

            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEBHOOK] Signature verification failed: {ex.Message}");
                return BadRequest();
            }

            Console.WriteLine($"[WEBHOOK] Received event: {stripeEvent.Type}");

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    {
                        Stripe.Checkout.Session? session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        if (session == null && stripeEvent.Data.RawObject != null)
                        {
                            try
                            {
                                session = System.Text.Json.JsonSerializer.Deserialize<Stripe.Checkout.Session>(
                                    stripeEvent.Data.RawObject.ToString() ?? "{}",
                                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            }
                            catch { }
                        }

                        var orderId = ExtractOrderId(session);
                        if (orderId.HasValue)
                            await MarkOrderPaid(orderId.Value, session?.Id ?? "", "checkout.session.completed");
                        break;
                    }

                case "payment_intent.succeeded":
                    {
                        Stripe.PaymentIntent? pi = stripeEvent.Data.Object as Stripe.PaymentIntent;
                        if (pi == null && stripeEvent.Data.RawObject != null)
                        {
                            try
                            {
                                pi = System.Text.Json.JsonSerializer.Deserialize<Stripe.PaymentIntent>(
                                    stripeEvent.Data.RawObject.ToString() ?? "{}",
                                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            }
                            catch { }
                        }

                        int? orderId = null;
                        if (pi?.Metadata != null &&
                            pi.Metadata.TryGetValue("orderId", out var raw) &&
                            int.TryParse(raw, out var parsed))
                        {
                            orderId = parsed;
                        }

                        if (orderId.HasValue)
                            await MarkOrderPaid(orderId.Value, pi?.Id ?? "", "payment_intent.succeeded");
                        break;
                    }

                default:
                    // ignore other events
                    break;
            }


            return Ok();
        }

        private static int? ExtractOrderId(Stripe.Checkout.Session? session)
        {
            if (session == null) return null;

            if (session.Metadata != null &&
                session.Metadata.TryGetValue("orderId", out var raw) &&
                int.TryParse(raw, out var fromMeta))
                return fromMeta;

            if (!string.IsNullOrWhiteSpace(session.ClientReferenceId) &&
                int.TryParse(session.ClientReferenceId, out var fromClientRef))
                return fromClientRef;

            return null;
        }

        private async Task MarkOrderPaid(int orderId, string refId, string source)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                Console.WriteLine($"[WEBHOOK] Order {orderId} not found for {source} ({refId})");
                return;
            }

            var firstTime = order.PaidAt == null;
            order.PaidAt = order.PaidAt ?? DateTime.UtcNow;

            if (string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                order.Status = "Confirmed";

            await _db.SaveChangesAsync();

            Console.WriteLine($"[WEBHOOK] Marked order {orderId} as PAID via {source}. FirstTime={firstTime}");
        }
    }
}
