import React, { useEffect, useMemo, useState } from "react";
import { Section, StatusBadge } from "../components/UI.jsx";
import SwipeRow from "../components/SwipeRow.jsx";
import { prettyStatus, rowBgFor } from "../utils/status.js";
import { motion, AnimatePresence } from "framer-motion";

/** Hook: match Tailwind's sm breakpoint (min-width: 640px) */
function useIsDesktop() {
    const query = "(min-width: 640px)";
    const [isDesktop, setIsDesktop] = useState(
        typeof window !== "undefined" ? window.matchMedia(query).matches : true
    );
    useEffect(() => {
        const m = window.matchMedia(query);
        const handler = (e) => setIsDesktop(e.matches);
        try {
            m.addEventListener("change", handler);
            return () => m.removeEventListener("change", handler);
        } catch {
            // Safari fallback
            m.addListener(handler);
            return () => m.removeListener(handler);
        }
    }, []);
    return isDesktop;
}

// defensive normalizer for order items (handles various DTO shapes)
function normalizeItems(items) {
    const src = items || [];
    return src.map((i) => {
        const name = i.name ?? i.nameSnapshot ?? i.productName ?? "Item";
        const unitPrice = Number(
            i.unitPrice ?? i.price ?? i.unit_price ?? 0
        );
        const quantity = Number(i.quantity ?? i.qty ?? 1);
        const lineTotal =
            (i.lineTotal ?? i.line_total) != null
                ? Number(i.lineTotal ?? i.line_total)
                : unitPrice * quantity;
        return { name, unitPrice, quantity, lineTotal };
    });
}

export default function Orders({ api }) {
    const [orders, setOrders] = useState([]);
    const [detail, setDetail] = useState(null); // order shown in drawer

    // prevent background scroll when drawer/sheet is open
    useEffect(() => {
        if (!detail) return;
        const prev = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        return () => {
            document.body.style.overflow = prev;
        };
    }, [detail]);

    // close on ESC
    useEffect(() => {
        const onKey = (e) => {
            if (e.key === "Escape") setDetail(null);
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, []);

    // Sorting
    const [sort, setSort] = useState({ key: "createdAt", dir: "desc" }); // newest first
    const toggleSort = (key) => {
        setSort((prev) =>
            prev.key === key
                ? { key, dir: prev.dir === "asc" ? "desc" : "asc" }
                : { key, dir: "asc" }
        );
    };
    const cmp = (a, b) => (a === b ? 0 : a > b ? 1 : -1);

    const sortedOrders = useMemo(() => {
        const copy = [...orders];
        copy.sort((a, b) => {
            let av, bv;
            switch (sort.key) {
                case "createdAt":
                    av = new Date(a.createdAt || 0).getTime();
                    bv = new Date(b.createdAt || 0).getTime();
                    break;
                case "totalPrice":
                    av = Number(a.totalPrice || 0);
                    bv = Number(b.totalPrice || 0);
                    break;
                case "status":
                    av = String(a.status || "").toLowerCase();
                    bv = String(b.status || "").toLowerCase();
                    break;
                case "customerName":
                    av = String(a.customerName || "").toLowerCase();
                    bv = String(b.customerName || "").toLowerCase();
                    break;
                case "restaurantName":
                    av = String(a.restaurantName || "").toLowerCase();
                    bv = String(b.restaurantName || "").toLowerCase();
                    break;
                default:
                    av = Number(a.id || 0);
                    bv = Number(b.id || 0);
                    break;
            }
            const c = cmp(av, bv);
            return sort.dir === "asc" ? c : -c;
        });
        return copy;
    }, [orders, sort]);

    // Load data
    async function refreshAll() {
        const o = await api.listOrders();
        const normalized = (o || []).map((x) => {
            const items = normalizeItems(x.items || x.orderItems);
            const total =
                typeof x.totalPrice === "number"
                    ? x.totalPrice
                    : items.reduce((s, i) => s + Number(i.lineTotal || 0), 0);
            return {
                ...x,
                status: prettyStatus(x.status),
                items,
                totalPrice: total,
                paidAt: x.paidAt ?? x.PaidAt ?? null,
            };
        });
        setOrders(normalized);
    }
    useEffect(() => {
        refreshAll();
    }, []);

    // Actions
    async function updateStatus(id, status) {
        const display = prettyStatus(status);
        setOrders((prev) =>
            prev.map((x) => (x.id === id ? { ...x, status: display } : x))
        ); // optimistic
        await api.updateOrderStatus(id, status);
        await refreshAll();
    }

    async function remove(id) {
        await api.deleteOrder(id);
        await refreshAll();
        if (detail?.id === id) setDetail(null);
    }
    const [payingId, setPayingId] = useState(null);
    async function pay(orderId) {
        setPayingId(orderId);
        try {
            const base = window.location.origin;
            const res = await api.createCheckoutSession({
                orderId,
                successUrl: `${base}/checkout/success?orderId=${orderId}`,
                cancelUrl: `${base}/checkout/cancel?orderId=${orderId}`,
            });

            if (res?.url) {
                window.location.href = res.url;
            } else {
                console.error("Checkout session returned no URL:", res);
                alert("Could not start checkout (no URL returned). Check server logs.");
            }
        } catch (ex) {
            console.error("Checkout failed:", ex);
            const status = ex.status || "(no status)";
            const detail = typeof ex.data === "string" ? ex.data : (ex.data?.message || ex.message);
            alert(`Checkout error ${status}: ${detail}`);
        } finally {
            setPayingId(null);
        }
    }

    const statuses = ["Pending", "Confirmed", "Delivered"];
    const arrow = (key) =>
        sort.key === key ? (sort.dir === "asc" ? " ▲" : " ▼") : "";
    const fmt = (n) => (typeof n === "number" ? n.toFixed(2) : n ?? "");

    /** Animated Drawer/Sheet using Framer Motion (responsive) */
    function Drawer({ order, onClose }) {
        const isDesktop = useIsDesktop();
        return (
            <AnimatePresence>
                {order && (
                    <>
                        {/* Backdrop */}
                        <motion.div
                            key="backdrop"
                            className="fixed inset-0 bg-black/30 z-40"
                            onClick={onClose}
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1, transition: { duration: 0.25 } }}
                            exit={{ opacity: 0, transition: { duration: 0.2 } }}
                        />
                        {/* Panel (right drawer on desktop, bottom sheet on mobile) */}
                        {isDesktop ? (
                            <motion.aside
                                key="panel-desktop"
                                className="fixed right-0 top-0 h-full w-full sm:w-[28rem] bg-white z-50 shadow-xl flex flex-col"
                                role="dialog"
                                aria-modal="true"
                                initial={{ x: "100%" }}
                                animate={{
                                    x: 0,
                                    transition: { type: "spring", stiffness: 380, damping: 34 },
                                }}
                                exit={{ x: "100%", transition: { duration: 0.22 } }}
                            >
                                <Header order={order} onClose={onClose} />
                                <Content order={order} />
                            </motion.aside>
                        ) : (
                            <motion.aside
                                key="panel-mobile"
                                className="fixed left-0 right-0 bottom-0 h-[78%] max-h-[88%] bg-white z-50 shadow-2xl rounded-t-2xl flex flex-col"
                                role="dialog"
                                aria-modal="true"
                                initial={{ y: "100%" }}
                                animate={{
                                    y: 0,
                                    transition: { type: "spring", stiffness: 380, damping: 34 },
                                }}
                                exit={{ y: "100%", transition: { duration: 0.22 } }}
                            >
                                {/* drag handle */}
                                <div className="mx-auto mt-2 mb-1 h-1.5 w-10 rounded-full bg-gray-300" />
                                <Header order={order} onClose={onClose} compact />
                                <Content order={order} />
                            </motion.aside>
                        )}
                    </>
                )}
            </AnimatePresence>
        );
    }

    function Header({ order, onClose, compact = false }) {
        return (
            <header
                className={`flex items-center justify-between ${compact ? "px-4 py-2" : "p-4"
                    } border-b`}
            >
                <div className="min-w-0">
                    <h3 className="text-lg font-semibold truncate">Order #{order.id}</h3>
                    <p className="text-xs text-gray-500 truncate">
                        {order.customerName} • {order.restaurantName}
                    </p>
                </div>
                <button
                    className="rounded-lg border px-3 py-1 shrink-0"
                    onClick={onClose}
                    aria-label="Close"
                >
                    Close
                </button>
            </header>
        );
    }

    function Content({ order }) {
        return (
            <div className="p-4 space-y-4 overflow-y-auto grow">
                <div className="flex items-center gap-2">
                    <span className="text-sm text-gray-600">Status:</span>
                    <StatusBadge status={order.status} />
                    {order.paidAt && (
                        <span className="text-xs text-green-700">
                            • Paid {new Date(order.paidAt).toLocaleString()}
                        </span>
                    )}
                </div>

                <div className="rounded-xl border">
                    <table className="min-w-full text-sm">
                        <thead>
                            <tr className="text-left text-gray-600">
                                <th className="py-2 px-3">Item</th>
                                <th className="py-2 px-3">Price</th>
                                <th className="py-2 px-3">Qty</th>
                                <th className="py-2 px-3">Line</th>
                            </tr>
                        </thead>
                        <tbody>
                            {(order.items || []).length === 0 ? (
                                <tr>
                                    <td className="py-3 px-3 text-gray-500" colSpan={4}>
                                        No items.
                                    </td>
                                </tr>
                            ) : (
                                order.items.map((i, idx) => (
                                    <tr key={idx} className="border-t">
                                        <td className="py-2 px-3">{i.name}</td>
                                        <td className="py-2 px-3">{fmt(i.unitPrice)}</td>
                                        <td className="py-2 px-3">{i.quantity}</td>
                                        <td className="py-2 px-3">{fmt(i.lineTotal)}</td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                        <tfoot>
                            <tr className="border-t font-semibold">
                                <td className="py-2 px-3" colSpan={3}>
                                    Total
                                </td>
                                <td className="py-2 px-3">
                                    {fmt(
                                        typeof order.totalPrice === "number"
                                            ? order.totalPrice
                                            : (order.items || []).reduce(
                                                (s, i) => s + Number(i.lineTotal || 0),
                                                0
                                            )
                                    )}
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div className="text-xs text-gray-500">
                    Placed:{" "}
                    {order.createdAt ? new Date(order.createdAt).toLocaleString() : "—"}
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* New order creation now happens on the Menu page */}
            <Section title="Create New Order">
                <p className="text-sm">
                    Create orders on the{" "}
                    <a href="/menu" className="underline text-indigo-600">
                        Menu
                    </a>{" "}
                    page by selecting a restaurant, choosing items, and placing the order.
                </p>
            </Section>

            <Section title={`Orders (${sortedOrders.length})`}>
                {sortedOrders.length === 0 ? (
                    <p className="text-gray-600">No orders yet.</p>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="min-w-full text-sm">
                            <thead>
                                <tr className="text-left text-gray-600">
                                    <th
                                        className="py-2 cursor-pointer select-none"
                                        onClick={() => toggleSort("id")}
                                    >
                                        # {arrow("id")}
                                    </th>
                                    <th
                                        className="py-2 cursor-pointer select-none"
                                        onClick={() => toggleSort("customerName")}
                                    >
                                        Customer {arrow("customerName")}
                                    </th>
                                    <th
                                        className="py-2 cursor-pointer select-none"
                                        onClick={() => toggleSort("restaurantName")}
                                    >
                                        Restaurant {arrow("restaurantName")}
                                    </th>
                                    <th className="py-2">Items</th>
                                    <th
                                        className="py-2 cursor-pointer select-none"
                                        onClick={() => toggleSort("totalPrice")}
                                    >
                                        Total {arrow("totalPrice")}
                                    </th>
                                    <th
                                        className="py-2 cursor-pointer select-none"
                                        onClick={() => toggleSort("status")}
                                    >
                                        Status {arrow("status")}
                                    </th>
                                    <th
                                        className="py-2 cursor-pointer select-none"
                                        onClick={() => toggleSort("createdAt")}
                                    >
                                        Created {arrow("createdAt")}
                                    </th>
                                    <th className="py-2"></th>
                                </tr>
                            </thead>
                            <tbody>
                                {sortedOrders.map((o, idx) => {
                                    const itemsSummary = o.items?.length
                                        ? o.items.map((i) => `${i.quantity}× ${i.name}`).join(", ")
                                        : "";
                                    const isPending = String(o.status).toLowerCase().startsWith("pend");
                                    return (
                                        <SwipeRow
                                            key={o.id}
                                            className={`border-t ${rowBgFor(o.status)}`}
                                            onDelete={() => remove(o.id)}
                                        >
                                            <td className="py-2 bg-inherit">{idx + 1}</td>
                                            <td className="py-2 bg-inherit">{o.customerName}</td>
                                            <td className="py-2 bg-inherit">{o.restaurantName}</td>
                                            <td className="py-2 bg-inherit" title={itemsSummary}>
                                                {itemsSummary}
                                            </td>
                                            <td className="py-2 bg-inherit">
                                                {o.totalPrice?.toFixed
                                                    ? o.totalPrice.toFixed(2)
                                                    : o.totalPrice}
                                            </td>
                                            <td className="py-2 bg-inherit">
                                                <StatusBadge status={o.status} />
                                                {o.paidAt && (
                                                    <span className="ml-2 text-[11px] text-green-700 align-middle">
                                                        Paid
                                                    </span>
                                                )}
                                            </td>
                                            <td className="py-2 bg-inherit">
                                                {o.createdAt
                                                    ? new Date(o.createdAt).toLocaleString()
                                                    : ""}
                                            </td>
                                            <td className="py-2 bg-inherit space-x-2">
                                                <button
                                                    className="rounded-lg border px-2 py-1"
                                                    onClick={() => setDetail(o)}
                                                    title="View details"
                                                >
                                                    View
                                                </button>

                                                {/* Pay button only when not paid and still pending */}
                                                {!o.paidAt && isPending && (
                                                    <button
                                                        className="rounded-lg border px-2 py-1 disabled:opacity-50"
                                                        onClick={() => pay(o.id)}
                                                        disabled={payingId === o.id}
                                                        title="Pay with Stripe"
                                                    >
                                                        {payingId === o.id ? "Starting..." : "Pay"}
                                                    </button>

                                                )}

                                                {/* Disable status change once paid? keep enabled if you want */}
                                                <select
                                                    className="rounded-lg border px-2 py-1"
                                                    value={o.status}
                                                    onChange={async (e) =>
                                                        await updateStatus(o.id, e.target.value)
                                                    }
                                                    disabled={false}
                                                >
                                                    {["Pending", "Confirmed", "Delivered"].map((s) => (
                                                        <option key={s} value={s}>
                                                            {s}
                                                        </option>
                                                    ))}
                                                </select>

                                                <button
                                                    className="rounded-lg border px-2 py-1"
                                                    onClick={() => remove(o.id)}
                                                    title="Delete"
                                                >
                                                    Delete
                                                </button>
                                            </td>
                                        </SwipeRow>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                )}
            </Section>

            {/* Responsive animated detail drawer/sheet */}
            <Drawer order={detail} onClose={() => setDetail(null)} />
        </div>
    );
}
