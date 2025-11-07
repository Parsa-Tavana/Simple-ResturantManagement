import React from "react";
import { Link, useSearchParams } from "react-router-dom";

export default function CheckoutCancel() {
    const [sp] = useSearchParams();
    const orderId = sp.get("orderId");

    return (
        <div className="max-w-md mx-auto text-center space-y-4">
            <div className="text-3xl">⚠️ Payment canceled</div>
            <p className="text-gray-600">
                The payment for order{orderId ? ` #${orderId}` : ""} was canceled. You can try again anytime.
            </p>
            <div className="space-x-3">
                <Link to="/orders" className="underline text-indigo-600">Back to Orders</Link>
            </div>
        </div>
    );
}
