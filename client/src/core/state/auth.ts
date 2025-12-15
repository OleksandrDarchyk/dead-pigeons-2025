// src/atoms/auth.ts

import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "@core/api/authApi";
import type { JwtClaims } from "@core/api/generated/generated-client";


// Simple DTOs for login / register data
export type LoginCredentials = {
    email: string;
    password: string;
};

export type RegisterCredentials = {
    email: string;
    password: string;
    confirmPassword: string;
};

export const TOKEN_KEY = "jwt";

// Store token only in sessionStorage (safer for exams than localStorage)
const tokenStorage = createJSONStorage<string | null>(() => sessionStorage);

// Atom that keeps JWT in storage and in memory
export const jwtAtom = atomWithStorage<string | null>(
    TOKEN_KEY,
    null,
    tokenStorage
);

// Derived atom: when we have token -> load current user via WhoAmI
export const userAtom = atom(async (get): Promise<JwtClaims | null> => {
    const token = get(jwtAtom);
    if (!token) return null;

    try {
        // Custom client already adds Authorization header
        const me = await authApi.whoAmI();
        return me;
    } catch (err) {
        console.error("Failed to load user via WhoAmI", err);
        return null;
    }
});

// Hook that UI components can use for auth
export const useAuth = () => {
    const [token, setToken] = useAtom(jwtAtom);
    const [user] = useAtom(userAtom);
    const navigate = useNavigate();

    // Login + redirect based on role
    const login = async (creds: LoginCredentials): Promise<void> => {
        const result = await authApi.login(creds);

        // 1) Save token (Jotai + sessionStorage)
        setToken(result.token);

        // 2) Try to load user and redirect
        try {
            const me = await authApi.whoAmI();

            if (me.role === "Admin") {
                // Await so we don't leave a "floating" promise
                await navigate("/admin");
            } else {
                await navigate("/player");
            }
        } catch {
            // If something goes wrong – send user back to login
            await navigate("/login");
        }
    };

    // Register behaves like login (token + redirect)
    const register = async (creds: RegisterCredentials): Promise<void> => {
        const result = await authApi.register(creds);

        // Save token from register response
        setToken(result.token);

        try {
            const me = await authApi.whoAmI();

            if (me.role === "Admin") {
                await navigate("/admin");
            } else {
                await navigate("/player");
            }
        } catch {
            await navigate("/login");
        }
    };

    // Simple logout: clear token and go to login page
    const logout = (): void => {
        setToken(null); // Remove from atom + sessionStorage
        // We intentionally ignore possible Promise from navigate
        void navigate("/login");
    };

    return { token, user, login, register, logout };
};
