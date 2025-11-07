// ClientApp/src/pages/RestaurantMenu.jsx
import React, { useEffect, useMemo, useState } from "react";
import { useAuth } from "../auth/AuthContext.jsx";
import { Section, Select } from "../components/UI.jsx";

export default function RestaurantMenu({ api }) {
    const { isAuthed, customerId } = useAuth();

    const [restaurants, setRestaurants] = useState([]);
    const [restaurantId, setRestaurantId] = useState("");
    const [menu, setMenu] = useState([]);
    const [cart, setCart] = useState([]);
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState("");
    const [okMsg, setOkMsg] = useState("");

    // load restaurants on mount
    useEffect(() => {
        (async () => {
            try {
                const r = await api.listRestaurants();
                setRestaurants(r || []);
                if ((r?.length ?? 0) > 0) {
                    setRestaurantId(String(r[0].id));
                }
            } catch (ex) {
                setErr(ex?.data?.message || ex.message || "Failed to load restaurants");
            }
        })();
    }, []);

    // load menu when restaurant changes
    useEffect(() => {
        if (!restaurantId) { setMenu([]); return; }
        (async () => {
            setErr("");
            try {
                const m = await api.listMenu(restaurantId);
                setMenu(m || []);
            } catch (ex) {
                setErr(ex?.data?.message || ex.message || "Failed to load menu");
            }
        })();
    }, [restaurantId]);

    function addToCart(item) {
        setCart(prev => {
            const idx = prev.findIndex(x => x.menuItemId === item.id);
            if (idx >= 0) {
                const copy = [...prev];
                copy[idx] = { ...copy[idx], quantity: copy[idx].quantity + 1 };
                return copy;
            }
            return [...prev, { menuItemId: item.id, quantity: 1 }];
        });
    }

    function removeFromCart(menuItemId) {
        setCart(prev => prev.filter(x => x.menuItemId !== menuItemId));
    }

    const total = useMemo(() => {
        const byId = new Map((menu || []).map(m => [m.id, m]));
        return cart.reduce((sum, line) => {
            const item = byId.get(line.menuItemId);
            const price = Number(item?.price || 0);
            return sum + price * Number(line.quantity || 0);
        }, 0);
    }, [menu, cart]);

    async function placeOrder() {
        setErr(""); setOkMsg(""); setBusy(true);
        try {
            if (!isAuthed) throw new Error("Please sign in first.");
            if (customerId == null) throw new Error("Your account isn’t linked to a Customer. Ask an admin to link it.");
            if (!restaurantId) throw new Error("Choose a restaurant.");
            if (cart.length === 0) throw new Error("Your cart is empty.");

            // Build items with quantity
            const items = cart.map(c => ({ menuItemId: c.menuItemId, quantity: c.quantity }));

            // IMPORTANT: call API helper (adds Authorization automatically)
            const created = await api.createOrderWithItems({
                customerId: Number(customerId),
                restaurantId: Number(restaurantId),
                items
            });

            setOkMsg(`Order #${created?.id ?? ""} placed!`);
            setCart([]);
        } catch (ex) {
            // Show clear error for 401
            if (ex?.status === 401) {
                setErr("Unauthorized (401). Your session may be missing or expired. Please sign in again.");
            } else {
                const msg = ex?.data?.message || (typeof ex?.data === "string" ? ex.data : ex?.message) || "Failed to place order";
                setErr(msg);
            }
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="space-y-6">
            <Section title="Select restaurant">
                <Select
                    label="Restaurant"
                    value={restaurantId}
                    onChange={setRestaurantId}
                    options={restaurants.map(r => ({ value: r.id, label: r.name }))}
                />
                {!isAuthed && (
                    <p className="mt-2 text-sm text-amber-700">
                        You’re not signed in. You can browse the menu, but you must sign in to place an order.
                    </p>
                )}
            </Section>

            <Section title="Menu">
                {menu.length === 0 ? (
                    <p className="text-gray-600">No items.</p>
                ) : (
                    <ul className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                        {menu.map(mi => (
                            <li key={mi.id} className="rounded-xl border p-3">
                                <div className="font-medium">{mi.name}</div>
                                <div className="text-sm text-gray-600">{mi.description}</div>
                                <div className="mt-2 text-sm">Price: {Number(mi.price).toFixed(2)}</div>
                                <button
                                    className="mt-3 rounded-lg border px-3 py-1"
                                    onClick={() => addToCart(mi)}
                                >
                                    Add
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </Section>

            <Section title={`Cart (${cart.length})`}>
                {cart.length === 0 ? (
                    <p className="text-gray-600">Empty.</p>
                ) : (
                    <>
                        <ul className="space-y-2">
                            {cart.map(line => {
                                const item = menu.find(m => m.id === line.menuItemId);
                                return (
                                    <li key={line.menuItemId} className="flex items-center justify-between rounded-lg border px-3 py-2">
                                        <div className="min-w-0">
                                            <div className="font-medium truncate">{item?.name ?? `#${line.menuItemId}`}</div>
                                            <div className="text-xs text-gray-600">
                                                {Number(item?.price || 0).toFixed(2)} × {line.quantity}
                                            </div>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <button
                                                className="rounded border px-2"
                                                onClick={() =>
                                                    setCart(prev => prev.map(x =>
                                                        x.menuItemId === line.menuItemId
                                                            ? { ...x, quantity: Math.max(1, x.quantity - 1) }
                                                            : x
                                                    ))
                                                }
                                            >-</button>
                                            <span className="w-6 text-center">{line.quantity}</span>
                                            <button
                                                className="rounded border px-2"
                                                onClick={() =>
                                                    setCart(prev => prev.map(x =>
                                                        x.menuItemId === line.menuItemId
                                                            ? { ...x, quantity: x.quantity + 1 }
                                                            : x
                                                    ))
                                                }
                                            >+</button>
                                            <button
                                                className="rounded border px-2 py-1"
                                                onClick={() => removeFromCart(line.menuItemId)}
                                            >
                                                Remove
                                            </button>
                                        </div>
                                    </li>
                                );
                            })}
                        </ul>

                        <div className="mt-3 flex items-center justify-between">
                            <div className="font-semibold">Total: {total.toFixed(2)}</div>
                            <button
                                disabled={busy}
                                className="rounded-xl bg-indigo-600 text-white px-4 py-2 disabled:opacity-60"
                                onClick={placeOrder}
                            >
                                {busy ? "Placing..." : "Place order"}
                            </button>
                        </div>
                    </>
                )}
                {err && <p className="mt-2 text-sm text-red-600">{err}</p>}
                {okMsg && <p className="mt-2 text-sm text-green-700">{okMsg}</p>}
            </Section>
        </div>
    );
}
