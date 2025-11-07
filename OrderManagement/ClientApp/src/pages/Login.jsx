import React, { useState } from "react";
import { useNavigate, Link, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext.jsx";
import { Section, TextInput } from "../components/UI.jsx";

export default function Login() {
    const nav = useNavigate();
    const loc = useLocation();
    const { login } = useAuth();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState("");

    const redirectTo = (loc.state && loc.state.from) || "/";

    async function onSubmit(e) {
        e.preventDefault();
        if (busy) return;
        setBusy(true); setErr("");
        const ok = await login({ email: email.trim(), password });
        if (ok) nav(redirectTo, { replace: true });
        else setErr("Invalid email or password.");
        setBusy(false);
    }

    return (
        <div className="max-w-md mx-auto">
            <Section title="Sign in">
                <form className="space-y-3" onSubmit={onSubmit}>
                    <TextInput label="Email" value={email} onChange={setEmail} type="email" autoComplete="username" required />
                    <TextInput label="Password" value={password} onChange={setPassword} type="password" autoComplete="current-password" required />
                    {err && <p className="text-sm text-red-600">{err}</p>}
                    <button disabled={busy} className="rounded-xl bg-indigo-600 text-white px-4 py-2 disabled:opacity-60 w-full">
                        {busy ? "Signing in..." : "Sign in"}
                    </button>
                    <p className="text-sm text-gray-600">
                        No account? <Link to="/register" className="underline text-indigo-600">Register</Link>
                    </p>
                </form>
            </Section>
        </div>
    );
}
