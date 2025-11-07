import React, { useMemo, useState } from "react";
import { NavLink, Route, Routes, Navigate } from "react-router-dom";

import Customers from "./pages/Customers.jsx";
import RestaurantMenu from "./pages/RestaurantMenu.jsx";
import Restaurants from "./pages/Restaurants.jsx";
import Orders from "./pages/Orders.jsx";
import Forbidden from "./pages/Forbidden.jsx";

import { makeApi } from "./api.js";

// --- Auth bits ---
import { AuthProvider, useAuth } from "./auth/AuthContext.jsx";
import ProtectedRoute from "./auth/ProtectedRoute.jsx";
import Login from "./pages/Login.jsx";
import Register from "./pages/Register.jsx";

import CheckoutSuccess from "./pages/CheckoutSuccess.jsx";
import CheckoutCancel from "./pages/CheckoutCancel.jsx";

export default function App() {
    const defaultBase = (typeof window !== "undefined")
        ? `${window.location.origin}`
        : "http://localhost:5248";

    const [baseUrl, setBaseUrl] = useState(defaultBase);
    const api = useMemo(() => makeApi(baseUrl), [baseUrl]);

    const linkClass = ({ isActive }) =>
        `px-3 py-2 rounded-xl ${isActive ? "bg-indigo-600 text-white" : "text-gray-700 hover:bg-gray-100"}`;

    return (
        <AuthProvider api={api}>
            <div className="min-h-screen bg-gray-50">
                <header className="sticky top-0 z-10 bg-white border-b">
                    <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between gap-3">
                        <nav className="flex items-center gap-2">
                            <span className="text-2xl font-bold tracking-tight">🍽️ Order Management</span>
                            <NavLink to="/orders" className={linkClass}>Orders</NavLink>
                            <AdminLinks linkClass={linkClass} />
                            <NavLink to="/menu" className={linkClass}>Menu</NavLink>
                        </nav>

                        <div className="flex items-center gap-3">
                            <AuthStatus />
                            <input
                                className="w-80 rounded-xl border px-3 py-2"
                                value={baseUrl}
                                onChange={(e) => setBaseUrl(e.target.value)}
                                placeholder="http://localhost:5248"
                                title="API Base URL"
                            />
                        </div>
                    </div>
                </header>

                <main className="max-w-6xl mx-auto px-4 py-6">
                    <Routes>
                        {/* Public auth routes */}
                        <Route path="/login" element={<Login />} />
                        <Route path="/register" element={<Register api={api} />} />

                        {/* Default redirect */}
                        <Route path="/" element={<StartRoute />} />

                        {/* Protected app routes */}
                        <Route
                            path="/orders"
                            element={
                                <ProtectedRoute>
                                    <Orders api={api} />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/customers"
                            element={
                                <ProtectedRoute roles={["RestaurantAdmin", "SuperAdmin"]}>
                                    <Customers api={api} onAnyChange={() => { }} />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/restaurants"
                            element={
                                <ProtectedRoute roles={["RestaurantAdmin", "SuperAdmin"]}>
                                    <Restaurants api={api} onAnyChange={() => { }} />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/menu"
                            element={
                                <ProtectedRoute roles={["Customer", "RestaurantAdmin", "SuperAdmin"]}>
                                    <RestaurantMenu api={api} />
                                </ProtectedRoute>
                            }
                        />

                        {/* Checkout result pages */}
                        <Route path="/checkout/success" element={<CheckoutSuccess api={api} />} />
                        <Route path="/checkout/cancel" element={<CheckoutCancel />} />

                        {/* Forbidden + Not found */}
                        <Route path="/forbidden" element={<Forbidden />} />
                        <Route path="*" element={<div className="text-gray-600">Not found</div>} />
                    </Routes>
                </main>

                <footer className="text-xs text-gray-500 pt-4 pb-8 text-center">
                    <p>If requests fail due to CORS during dev, enable it in the API (Program.cs) with UseCors().</p>
                </footer>
            </div>
        </AuthProvider>
    );
}

function StartRoute() {
    const { isAuthed } = useAuth();
    return <Navigate to={isAuthed ? "/orders" : "/login"} replace />;
}

function AdminLinks({ linkClass }) {
    const auth = useAuth();
    const isAdmin = auth.isInRole("RestaurantAdmin", "SuperAdmin");
    if (!isAdmin) return null;
    return (
        <>
            <NavLink to="/customers" className={linkClass}>Customers</NavLink>
            <NavLink to="/restaurants" className={linkClass}>Restaurants</NavLink>
        </>
    );
}

/** Small header widget that shows email + Login/Logout links */
function AuthStatus() {
    const auth = useAuth();
    if (auth.isAuthed) {
        return (
            <div className="flex items-center gap-2">
                <span className="text-sm text-gray-600 truncate max-w-[14rem]" title={auth.email}>
                    {auth.email}
                </span>
                <button onClick={auth.logout} className="text-sm underline">Logout</button>
            </div>
        );
    }
    return (
        <div className="flex items-center gap-2 text-sm">
            <NavLink to="/login" className="underline">Login</NavLink>
            <NavLink to="/register" className="underline">Register</NavLink>
        </div>
    );
}
