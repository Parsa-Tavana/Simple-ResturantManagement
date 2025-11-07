//import React, { createContext, useContext, useEffect, useMemo, useState } from "react";

//const AuthCtx = createContext(null);
//export function useAuth() {
//    const ctx = useContext(AuthCtx);
//    if (!ctx) throw new Error("useAuth must be used within <AuthProvider>");
//    return ctx;
//}

//export function AuthProvider({ api, children }) {
//    const [token, setToken] = useState(() => localStorage.getItem("token") || "");
//    const [roles, setRoles] = useState(() => {
//        try { return JSON.parse(localStorage.getItem("roles") || "[]"); } catch { return []; }
//    });
//    const [email, setEmail] = useState(() => localStorage.getItem("email") || "");
//    const [customerId, setCustomerId] = useState(() => {
//        const v = localStorage.getItem("customerId"); return v ? Number(v) : null;
//    });
//    const [restaurantId, setRestaurantId] = useState(() => {
//        const v = localStorage.getItem("restaurantId"); return v ? Number(v) : null;
//    });

//    // persist
//    useEffect(() => { token ? localStorage.setItem("token", token) : localStorage.removeItem("token"); }, [token]);
//    useEffect(() => { localStorage.setItem("roles", JSON.stringify(roles || [])); }, [roles]);
//    useEffect(() => { email ? localStorage.setItem("email", email) : localStorage.removeItem("email"); }, [email]);
//    useEffect(() => { customerId != null ? localStorage.setItem("customerId", String(customerId)) : localStorage.removeItem("customerId"); }, [customerId]);
//    useEffect(() => { restaurantId != null ? localStorage.setItem("restaurantId", String(restaurantId)) : localStorage.removeItem("restaurantId"); }, [restaurantId]);

//    const isAuthed = !!token;
//    const isInRole = (...wanted) => roles?.some(r => wanted.includes(r));

//    const clearAll = () => {
//        localStorage.removeItem("token");
//        localStorage.removeItem("roles");
//        localStorage.removeItem("email");
//        localStorage.removeItem("customerId");
//        localStorage.removeItem("restaurantId");
//        setToken(""); setRoles([]); setEmail(""); setCustomerId(null); setRestaurantId(null);
//    };

//    const loadMe = async () => {
//        // Only try /me if we have a token persisted
//        if (!localStorage.getItem("token")) { clearAll(); return; }
//        try {
//            const me = await api.me(); // GET /api/auth/me
//            setEmail(me.email || "");
//            setRoles(me.roles || []);
//            setCustomerId(me.customerId ?? null);
//            setRestaurantId(me.restaurantId ?? null);
//        } catch {
//            // don’t blow away auth if /me fails; we still have a valid token from login
//            // You can log this if you want:
//            // console.warn("GET /api/auth/me failed; continuing with login response data");
//        }
//    };

//    // On first mount or when token changes, try to enrich with /me data
//    useEffect(() => { if (token) loadMe(); }, [token]);

//    // Robust login: set everything we can from response, then try /me for linked IDs
//    const login = async ({ email, password }) => {
//        try {
//            const res = await api.login({ email, password }); // { token, roles, email }
//            if (!res?.token) throw new Error("No token from server");

//            // Persist token first so apiFetch sends Authorization header immediately
//            localStorage.setItem("token", res.token);
//            setToken(res.token);

//            // Seed UI state from login response (works even if /me never runs)
//            setEmail(res.email || "");
//            setRoles(res.roles || []);
//            setCustomerId(res.customerId ?? null);      // <-- set from login
//            setRestaurantId(res.restaurantId ?? null);  // <-- set from login
//            // Now try to enrich with /me (for customerId/restaurantId, etc.)
//            await loadMe();
//            return true;
//        } catch {
//            clearAll();
//            return false;
//        }
//    };

//    const register = async ({ email, password, role, customerId, restaurantId }) => {
//        await api.register({ email, password, role, customerId, restaurantId });
//        await login({ email, password });
//    };

//    const logout = () => clearAll();

//    const value = useMemo(() => ({
//        token, roles, email, customerId, restaurantId,
//        isAuthed, isInRole,
//        login, logout, register, loadMe
//    }), [token, roles, email, customerId, restaurantId]);

//    return <AuthCtx.Provider value={value}>{children}</AuthCtx.Provider>;
//}
// ClientApp/src/auth/AuthContext.jsx
import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import {
    setToken as storeToken,      // from api.js
    clearToken as removeToken,   // from api.js
    getToken as readToken,       // from api.js
} from "../api.js";

const AuthCtx = createContext(null);

export function useAuth() {
    const ctx = useContext(AuthCtx);
    if (!ctx) throw new Error("useAuth must be used within <AuthProvider>");
    return ctx;
}

export function AuthProvider({ api, children }) {
    // Initialize from storage
    const [token, setToken] = useState(() => readToken() || "");
    const [roles, setRoles] = useState(() => {
        try { return JSON.parse(localStorage.getItem("roles") || "[]"); } catch { return []; }
    });
    const [email, setEmail] = useState(() => localStorage.getItem("email") || "");
    const [customerId, setCustomerId] = useState(() => {
        const v = localStorage.getItem("customerId"); return v ? Number(v) : null;
    });
    const [restaurantId, setRestaurantId] = useState(() => {
        const v = localStorage.getItem("restaurantId"); return v ? Number(v) : null;
    });

    // Keep storage in sync with state token
    useEffect(() => {
        const current = readToken() || "";
        if (token && token !== current) storeToken(token);
        if (!token && current) removeToken();
    }, [token]);

    // Persist other fields
    useEffect(() => { localStorage.setItem("roles", JSON.stringify(roles || [])); }, [roles]);
    useEffect(() => { email ? localStorage.setItem("email", email) : localStorage.removeItem("email"); }, [email]);
    useEffect(() => {
        customerId != null ? localStorage.setItem("customerId", String(customerId)) : localStorage.removeItem("customerId");
    }, [customerId]);
    useEffect(() => {
        restaurantId != null ? localStorage.setItem("restaurantId", String(restaurantId)) : localStorage.removeItem("restaurantId");
    }, [restaurantId]);

    const isAuthed = !!token;
    const isInRole = (...wanted) => roles?.some(r => wanted.includes(r));

    const clearAll = () => {
        removeToken();
        localStorage.removeItem("roles");
        localStorage.removeItem("email");
        localStorage.removeItem("customerId");
        localStorage.removeItem("restaurantId");
        setToken("");
        setRoles([]);
        setEmail("");
        setCustomerId(null);
        setRestaurantId(null);
    };

    // Load /me to enrich roles/links; do NOT kick user out if /me fails
    const loadMe = async () => {
        const t = readToken();
        if (!t) return; // nothing to do
        try {
            const me = await api.me(); // requires [Authorize] and valid JWT
            setEmail(me.email || "");
            setRoles(me.roles || []);
            setCustomerId(me.customerId ?? null);
            setRestaurantId(me.restaurantId ?? null);
        } catch (e) {
            // Keep login state, just log for debugging
            // console.warn("/api/auth/me failed:", e);
        }
    };

    // Refresh /me when token appears/changes
    useEffect(() => { if (token) void loadMe(); }, [token]);

    // Login writes storage first (so subsequent calls carry Authorization)
    const login = async ({ email, password }) => {
        try {
            const res = await api.login({ email, password }); // { token, roles, email, customerId, restaurantId }
            if (!res?.token) throw new Error("No token from server");

            storeToken(res.token);
            setToken(res.token);

            // Seed UI immediately from login payload
            setEmail(res.email || "");
            setRoles(res.roles || []);
            setCustomerId(res.customerId ?? null);
            setRestaurantId(res.restaurantId ?? null);

            // Best-effort enrichment
            try { await loadMe(); } catch { /* ignore */ }

            return true;
        } catch (e) {
            clearAll();
            return false;
        }
    };

    const register = async ({ email, password, role, customerId, restaurantId }) => {
        await api.register({ email, password, role, customerId, restaurantId });
        await login({ email, password });
    };

    const logout = () => clearAll();

    const value = useMemo(() => ({
        token, roles, email, customerId, restaurantId,
        isAuthed, isInRole,
        login, logout, register, loadMe,
    }), [token, roles, email, customerId, restaurantId]);

    return <AuthCtx.Provider value={value}>{children}</AuthCtx.Provider>;
}
