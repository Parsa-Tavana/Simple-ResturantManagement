// ClientApp/src/api.js

// ---- Token helpers ----------------------------------------------------------
function sanitizeToken(raw) {
    if (!raw) return null;
    // Trim, strip any wrapping quotes, and remove any internal whitespace/newlines
    const t = String(raw).trim().replace(/^"+|"+$/g, "");
    return t.replace(/\s+/g, "");
}

export function getToken() {
    // Only one canonical key: 'token'. (We still read 'auth_token' if present)
    const raw = localStorage.getItem("token") || localStorage.getItem("auth_token") || null;
    return sanitizeToken(raw);
}

export function setToken(token) {
    const t = sanitizeToken(token);
    if (t) localStorage.setItem("token", t);
    else localStorage.removeItem("token");
}

export function clearToken() {
    localStorage.removeItem("token");
}

// ---- Core fetch wrapper -----------------------------------------------------
export async function apiFetch(baseUrl, path, options = {}) {
    const token = getToken();
    const headers = {
        "Content-Type": "application/json",
        ...(options.headers || {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
    };

    // Quick dev debug: uncomment to log outgoing headers for auth endpoints
    // if (path.startsWith("/api/auth")) console.debug("[apiFetch] ->", path, headers.Authorization || "(no auth)");

    let res;
    try {
        res = await fetch(`${baseUrl}${path}`, {
            ...options,
            headers,
            credentials: "include", // safe; server ignores if not needed
        });
    } catch (netErr) {
        throw new Error(`Network error calling ${path}: ${netErr.message}`);
    }

    // Read body once
    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = text || null; }

    if (!res.ok) {
        const msg =
            typeof data === "string"
                ? data
                : data?.message || text || `HTTP ${res.status}`;
        const err = new Error(msg);
        err.status = res.status;
        err.data = data ?? text;
        // Quick dev debug headers from server (helps with 401)
        // console.debug("[apiFetch error]", path, res.status, {
        //   "www-authenticate": res.headers.get("www-authenticate"),
        //   "x-jwt-error": res.headers.get("x-jwt-error"),
        //   "x-jwt-desc": res.headers.get("x-jwt-desc"),
        // });
        throw err;
    }
    return data;
}

// ---- API surface ------------------------------------------------------------
export function makeApi(baseUrl) {
    return {
        // Auth
        me: () => apiFetch(baseUrl, "/api/auth/me"),
        register: ({ email, password, role, customerId, restaurantId }) =>
            apiFetch(baseUrl, "/api/auth/register", {
                method: "POST",
                body: JSON.stringify({ email, password, role, customerId, restaurantId }),
            }),
        login: ({ email, password }) =>
            apiFetch(baseUrl, "/api/auth/login", {
                method: "POST",
                body: JSON.stringify({ email, password }),
            }),

        // Customers
        listCustomers: () => apiFetch(baseUrl, "/api/customers"),
        createCustomer: (p) =>
            apiFetch(baseUrl, "/api/customers", { method: "POST", body: JSON.stringify(p) }),

        // Restaurants
        listRestaurants: () => apiFetch(baseUrl, "/api/restaurants"),
        createRestaurant: (p) =>
            apiFetch(baseUrl, "/api/restaurants", { method: "POST", body: JSON.stringify(p) }),

        // Orders
        listOrders: () => apiFetch(baseUrl, "/api/orders"),
        getOrder: (id) => apiFetch(baseUrl, `/api/orders/${id}`),
        createOrder: (p) =>
            apiFetch(baseUrl, "/api/orders", { method: "POST", body: JSON.stringify(p) }),
        updateOrderStatus: (id, status) =>
            apiFetch(baseUrl, `/api/orders/${id}/status`, {
                method: "PUT",
                body: JSON.stringify({ status }),
            }),
        deleteOrder: (id) => apiFetch(baseUrl, `/api/orders/${id}`, { method: "DELETE" }),

        // Menu + create order with items
        listMenu: (restaurantId) =>
            apiFetch(baseUrl, `/api/restaurants/${restaurantId}/menu`),
        createOrderWithItems: ({ customerId, restaurantId, items }) =>
            apiFetch(baseUrl, "/api/orders", {
                method: "POST",
                body: JSON.stringify({ customerId, restaurantId, items }),
            }),

        // Stripe
        createCheckoutSession: ({ orderId, successUrl, cancelUrl }) =>
            apiFetch(baseUrl, "/api/payments/checkout-session", {
                method: "POST",
                body: JSON.stringify({ orderId, successUrl, cancelUrl }),
            }),
    };
}
