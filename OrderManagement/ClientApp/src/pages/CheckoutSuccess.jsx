import React, { useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";

export default function CheckoutSuccess() {
    const [sp] = useSearchParams();
    const orderId = sp.get("orderId");

    // Optional: auto-refresh the Orders page state via SignalR in Phase 2.
    // For now, we just show a message.
    useEffect(() => {
        // no-op
    }, []);

    return (
        <div className="max-w-md mx-auto text-center space-y-4">
            <div className="text-3xl">✅ Payment successful</div>
            <p className="text-gray-600">
                Thanks! Your order{orderId ? ` #${orderId}` : ""} has been paid. It should soon show as <b>Confirmed</b>.
            </p>
            <div className="space-x-3">
                <Link to="/orders" className="underline text-indigo-600">Back to Orders</Link>
            </div>
        </div>
    );
}
