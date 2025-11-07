// ClientApp/src/utils/status.js
export function normalizeStatusKey(status) {
    if (status == null) return "pending";
    if (typeof status === "number") {
        const map0 = { 0: "pending", 1: "confirmed", 2: "delivered" };
        const map1 = { 1: "pending", 2: "confirmed", 3: "delivered" };
        return map0[status] || map1[status] || "pending";
    }
    const s = String(status).trim().toLowerCase();
    if (s.startsWith("pend")) return "pending";
    if (s.startsWith("conf")) return "confirmed";
    if (s.startsWith("deliv")) return "delivered";
    return "pending";
}

export function prettyStatus(status) {
    const key = normalizeStatusKey(status);
    return key.charAt(0).toUpperCase() + key.slice(1);
}

export function rowBgFor(status) {
    const key = normalizeStatusKey(status);
    const byKey = {
        pending: "bg-gray-50",
        confirmed: "bg-green-50",
        delivered: "bg-blue-50",
    };
    return byKey[key] || byKey.pending;
}
