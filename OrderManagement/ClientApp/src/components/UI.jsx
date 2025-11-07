// ClientApp/src/components/UI.jsx
import React from "react";
import { normalizeStatusKey, prettyStatus } from "../utils/status.js";

export function Section({ title, children }) {
    return (
        <section className="rounded-2xl border p-4">
            {title && <h2 className="mb-3 text-lg font-semibold">{title}</h2>}
            {children}
        </section>
    );
}

export function TextInput({ label, value, onChange, type = "text", ...rest }) {
    return (
        <label className="block">
            {label && <div className="mb-1 text-sm text-gray-600">{label}</div>}
            <input
                className="w-full rounded-lg border px-3 py-2"
                value={value}
                onChange={e => onChange(e.target.value)}
                type={type}
                {...rest}
            />
        </label>
    );
}

export function Select({ label, value, onChange, options = [], ...rest }) {
    return (
        <label className="block">
            {label && <div className="mb-1 text-sm text-gray-600">{label}</div>}
            <select
                className="w-full rounded-lg border px-3 py-2"
                value={value}
                onChange={e => onChange(e.target.value)}
                {...rest}
            >
                <option value="" disabled>Choose</option>
                {options.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
            </select>
        </label>
    );
}

export function StatusBadge({ status }) {
    const key = normalizeStatusKey(status);
    const classesByStatus = {
        pending: "bg-gray-100 text-gray-700 border-gray-300",
        confirmed: "bg-green-100 text-green-700 border-green-300",
        delivered: "bg-blue-100  text-blue-700  border-blue-300",
    };
    const cls = classesByStatus[key] || classesByStatus.pending;

    return (
        <span className={`inline-flex items-center rounded-full border px-4 py-1 text-xs ${cls}`}>
            {prettyStatus(status)}
        </span>
    );
}
