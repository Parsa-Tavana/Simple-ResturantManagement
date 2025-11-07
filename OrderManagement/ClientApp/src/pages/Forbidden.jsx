import React from "react";
import { Link } from "react-router-dom";

export default function Forbidden() {
    return (
        <div className="max-w-md mx-auto p-6 space-y-3">
            <h1 className="text-2xl font-semibold">Forbidden</h1>
            <p className="text-gray-700">
                You don’t have permission to view this page.
            </p>
            <Link to="/" className="underline text-indigo-600">Go home</Link>
        </div>
    );
}
