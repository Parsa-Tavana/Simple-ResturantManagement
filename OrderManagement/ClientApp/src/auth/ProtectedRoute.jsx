import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext.jsx";

/**
 * <ProtectedRoute roles={["RestaurantAdmin","SuperAdmin"]}>
 *   <YourPage />
 * </ProtectedRoute>
 *
 * - If not logged in -> go to /login
 * - If logged in but missing role -> go to /forbidden
 * - Else render children
 */
export default function ProtectedRoute({ roles, children }) {
    const auth = useAuth();
    const loc = useLocation();

    if (!auth.isAuthed) {
        return <Navigate to="/login" replace state={{ from: loc.pathname }} />;
    }

    if (roles && roles.length > 0 && !auth.isInRole(...roles)) {
        return <Navigate to="/forbidden" replace />;
    }

    return children;
}
