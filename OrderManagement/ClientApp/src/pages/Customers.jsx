import React, { useEffect, useState } from "react";
import { Section, TextInput } from "../components/UI.jsx";
import { useAuth } from "../auth/AuthContext.jsx";

export default function Customers({ api, onAnyChange }) {
    const auth = useAuth();
    const canManage = auth.isInRole("RestaurantAdmin", "SuperAdmin");

    const [items, setItems] = useState([]);
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [phone, setPhone] = useState("");
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState("");

    async function refresh() {
        try { setItems(await api.listCustomers() || []); } catch (ex) { setErr(ex.message); }
    }
    useEffect(() => { refresh(); }, []);

    async function submit(e) {
        e.preventDefault(); setBusy(true); setErr("");
        try {
            await api.createCustomer({ name, email, phone });
            setName(""); setEmail(""); setPhone("");
            await refresh(); onAnyChange?.();
        } catch (ex) { setErr(ex.data ? JSON.stringify(ex.data) : ex.message); }
        finally { setBusy(false); }
    }

    return (
        <div className="space-y-6">
            {canManage && (
                <Section title="New Customer">
                    <form onSubmit={submit} className="space-y-3">
                        <TextInput label="Name" value={name} onChange={setName} required placeholder="Alice" />
                        <TextInput label="Email" value={email} onChange={setEmail} required placeholder="alice@example.com" />
                        <TextInput label="Phone" value={phone} onChange={setPhone} placeholder="+1 555 0100" />
                        {err && <p className="text-sm text-red-600">{err}</p>}
                        <button disabled={busy} className="rounded-xl bg-indigo-600 text-white px-4 py-2 disabled:opacity-60">
                            {busy ? "Creating..." : "Create Customer"}
                        </button>
                    </form>
                </Section>
            )}

            <Section title={`Customers (${items.length})`}>
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                            <tr className="text-left text-gray-600">
                                <th className="py-2">#</th>
                                <th className="py-2">Name</th>
                                <th className="py-2">Email</th>
                                <th className="py-2">Phone</th>
                            </tr>
                        </thead>
                        <tbody>
                            {items.map(c => (
                                <tr key={c.id} className="border-t">
                                    <td className="py-2">{c.id}</td>
                                    <td className="py-2">{c.name}</td>
                                    <td className="py-2">{c.email}</td>
                                    <td className="py-2">{c.phone || ""}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </Section>
        </div>
    );
}
