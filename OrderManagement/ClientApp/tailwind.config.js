/** @type {import('tailwindcss').Config} */
export default {
    content: ["./index.html", "./src/**/*.{js,jsx,ts,tsx}"],
    safelist: [
        // Row backgrounds
        "bg-gray-50", "bg-green-50", "bg-blue-50",

        // Badge classes (if you use the pale version)
        "bg-gray-100", "text-gray-700", "border-gray-300",
        "bg-green-100", "text-green-700", "border-green-300",
        "bg-blue-100", "text-blue-700", "border-blue-300",

        // OR if you switched to bold versions
        // "bg-green-500","text-white","border-green-600",
        // "bg-blue-500","text-white","border-blue-600",
    ],
    theme: { extend: {} },
    plugins: [],
}
