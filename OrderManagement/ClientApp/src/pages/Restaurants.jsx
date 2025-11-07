import React, { useEffect, useState } from "react";
import { Section, TextInput } from "../components/UI.jsx";
import { useAuth } from "../auth/AuthContext.jsx";

export default function Restaurants({ api, onAnyChange }) {
    const auth = useAuth();
    const canManage = auth.isInRole("RestaurantAdmin", "SuperAdmin");

    const [items, setItems] = useState([]);
    const [name, setName] = useState("");
    const [address, setAddress] = useState("");
    const [phone, setPhone] = useState("");
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState("");

    async function refresh() {
        try { setItems(await api.listRestaurants() || []); } catch (ex) { setErr(ex.message); }
    }
    useEffect(() => { refresh(); }, []);

    async function submit(e) {
        e.preventDefault(); setBusy(true); setErr("");
        try {
            await api.createRestaurant({ name, address, phone });
            setName(""); setAddress(""); setPhone("");
            await refresh(); onAnyChange?.();
        } catch (ex) { setErr(ex.data ? JSON.stringify(ex.data) : ex.message); }
        finally { setBusy(false); }
    }

    return (
        <div className="space-y-6">
            {canManage && (
                <Section title="New Restaurant">
                    <form onSubmit={submit} className="space-y-3">
                        <TextInput label="Name" value={name} onChange={setName} required placeholder="Pasta Place" />
                        <TextInput label="Address" value={address} onChange={setAddress} required placeholder="Main St 1" />
                        <TextInput label="Phone" value={phone} onChange={setPhone} placeholder="555-123" />
                        {err && <p className="text-sm text-red-600">{err}</p>}
                        <button disabled={busy} className="rounded-xl bg-indigo-600 text-white px-4 py-2 disabled:opacity-60">
                            {busy ? "Creating..." : "Create Restaurant"}
                        </button>
                    </form>
                </Section>
            )}

            <Section title={`Restaurants (${items.length})`}>
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                            <tr className="text-left text-gray-600">
                                <th className="py-2">#</th>
                                <th className="py-2">Name</th>
                                <th className="py-2">Address</th>
                                <th className="py-2">Phone</th>
                            </tr>
                        </thead>
                        <tbody>
                            {items.map(r => (
                                <tr key={r.id} className="border-t">
                                    <td className="py-2">{r.id}</td>
                                    <td className="py-2">{r.name}</td>
                                    <td className="py-2">{r.address}</td>
                                    <td className="py-2">{r.phone || ""}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </Section>
        </div>
    );
}
