import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext.jsx";
import { Section, TextInput, Select } from "../components/UI.jsx";

export default function Register({ api }) {
    const nav = useNavigate();
    const { register } = useAuth();

    // form
    const [email, setEmail] = useState("");
    const [pwd, setPwd] = useState("");
    const [role, setRole] = useState("Customer");
    const [customerId, setCustomerId] = useState("");
    const [restaurantId, setRestaurantId] = useState("");

    // data for dropdowns
    const [customers, setCustomers] = useState([]);
    const [restaurants, setRestaurants] = useState([]);

    // password policy
    const [policy, setPolicy] = useState(null);

    // ui state
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState("");
    const [errList, setErrList] = useState([]);

    // load customers/restaurants and password policy
    useEffect(() => {
        (async () => {
            try {
                const [c, r] = await Promise.all([api.listCustomers(), api.listRestaurants()]);
                setCustomers(c || []); setRestaurants(r || []);
            } catch { }
            try {
                if (api.passwordPolicy) {
                    const p = await api.passwordPolicy();
                    setPolicy(p);
                } else {
                    // fallback if endpoint isn't present
                    setPolicy({ requiredLength: 6, requireDigit: false, requireLowercase: false, requireUppercase: false, requireNonAlphanumeric: false });
                }
            } catch {
                setPolicy({ requiredLength: 6, requireDigit: false, requireLowercase: false, requireUppercase: false, requireNonAlphanumeric: false });
            }
        })();
    }, []);

    // derived checks for policy
    const checks = useMemo(() => {
        const p = policy || { requiredLength: 6 };
        return {
            hasLen: pwd.length >= (p.requiredLength ?? 6),
            hasDigit: /\d/.test(pwd),
            hasLower: /[a-z]/.test(pwd),
            hasUpper: /[A-Z]/.test(pwd),
            hasSym: /[^A-Za-z0-9]/.test(pwd),
        };
    }, [pwd, policy]);

    const meetsPolicy = useMemo(() => {
        const p = policy || { requiredLength: 6, requireDigit: false, requireLowercase: false, requireUppercase: false, requireNonAlphanumeric: false };
        return (
            checks.hasLen &&
            (!p.requireDigit || checks.hasDigit) &&
            (!p.requireLowercase || checks.hasLower) &&
            (!p.requireUppercase || checks.hasUpper) &&
            (!p.requireNonAlphanumeric || checks.hasSym)
        );
    }, [checks, policy]);

    async function onSubmit(e) {
        e.preventDefault();
        setBusy(true); setErr(""); setErrList([]);
        try {
            await register({
                email,
                password: pwd,
                role,
                customerId: role === "Customer" ? Number(customerId) || null : null,
                restaurantId: role === "RestaurantAdmin" ? Number(restaurantId) || null : null
            });
            nav("/", { replace: true });
        } catch (ex) {
            const data = ex?.data;
            if (data?.errors && Array.isArray(data.errors)) {
                setErr(data.message || "Registration failed");
                setErrList(data.errors);
            } else {
                setErr(data?.message || (typeof data === "string" ? data : ex.message || "Registration failed"));
            }
        } finally {
            setBusy(false);
        }
    }

    const Req = ({ ok, children }) => (
        <li className={`flex items-center gap-2 ${ok ? "text-green-700" : "text-red-700"}`}>
            <span className={`inline-block w-2 h-2 rounded-full ${ok ? "bg-green-500" : "bg-red-500"}`} />
            {children}
        </li>
    );

    return (
        <div className="max-w-md mx-auto">
            <Section title="Create account">
                <form className="space-y-3" onSubmit={onSubmit}>
                    <TextInput label="Email" value={email} onChange={setEmail} type="email" required />
                    <TextInput label="Password" value={pwd} onChange={setPwd} type="password" required />

                    {/* Password policy checklist */}
                    {policy && (
                        <div className="mt-1 rounded-lg border bg-gray-50 p-3 text-xs">
                            <div className="mb-1 font-medium text-gray-700">Password requirements</div>
                            <ul className="space-y-1">
                                <Req ok={checks.hasLen}>At least {policy.requiredLength ?? 6} characters</Req>
                                {policy.requireDigit && <Req ok={checks.hasDigit}>At least one digit</Req>}
                                {policy.requireLowercase && <Req ok={checks.hasLower}>At least one lowercase letter</Req>}
                                {policy.requireUppercase && <Req ok={checks.hasUpper}>At least one uppercase letter</Req>}
                                {policy.requireNonAlphanumeric && <Req ok={checks.hasSym}>At least one symbol</Req>}
                            </ul>
                        </div>
                    )}

                    <Select
                        label="Role"
                        value={role}
                        onChange={setRole}
                        options={[
                            { value: "Customer", label: "Customer" },
                            { value: "RestaurantAdmin", label: "Restaurant Admin" },
                            { value: "SuperAdmin", label: "Super Admin" },
                        ]}
                    />

                    {role === "Customer" && (
                        <Select
                            label="Link to Customer"
                            value={customerId}
                            onChange={setCustomerId}
                            options={customers.map(c => ({ value: c.id, label: `${c.name} (${c.email})` }))}
                            required
                        />
                    )}

                    {role === "RestaurantAdmin" && (
                        <Select
                            label="Link to Restaurant"
                            value={restaurantId}
                            onChange={setRestaurantId}
                            options={restaurants.map(r => ({ value: r.id, label: r.name }))}
                            required
                        />
                    )}

                    {/* Server error block */}
                    {(err || errList.length > 0) && (
                        <div className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm">
                            {err && <div className="font-medium text-red-700">{err}</div>}
                            {errList.length > 0 && (
                                <ul className="mt-2 list-disc pl-5 text-red-700">
                                    {errList.map((m, i) => <li key={i}>{m}</li>)}
                                </ul>
                            )}
                        </div>
                    )}

                    <button
                        disabled={busy || !meetsPolicy}
                        className="rounded-xl bg-indigo-600 text-white px-4 py-2 disabled:opacity-60 w-full"
                        title={!meetsPolicy ? "Password doesn’t meet requirements" : undefined}
                    >
                        {busy ? "Creating..." : "Create account"}
                    </button>

                    <p className="text-sm text-gray-600">
                        Already have an account? <Link to="/login" className="underline text-indigo-600">Sign in</Link>
                    </p>
                </form>
            </Section>
        </div>
    );
}
