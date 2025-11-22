// src/atoms/auth.ts
import { atom } from "jotai";
import { atomWithStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "../utilities/authApi";

// Credentials type used by the login form
export type Credentials = {
    email: string;
    password: string;
};

export const TOKEN_KEY = "jwt";

// Simple sessionStorage adapter for atomWithStorage
const storage: {
    getItem: (key: string) => string | null;
    setItem: (key: string, value: string | null) => void;
    removeItem: (key: string) => void;
} = {
    getItem: (key) => {
        if (typeof window === "undefined") return null;
        return sessionStorage.getItem(key);
    },
    setItem: (key, value) => {
        if (typeof window === "undefined") return;
        if (value === null) {
            sessionStorage.removeItem(key);
        } else {
            sessionStorage.setItem(key, value);
        }
    },
    removeItem: (key) => {
        if (typeof window === "undefined") return;
        sessionStorage.removeItem(key);
    },
};

// JWT token atom stored in sessionStorage
export const jwtAtom = atomWithStorage<string | null>(TOKEN_KEY, null, storage);

// User atom: calls WhoAmI when token changes
export const userAtom = atom(async (get) => {
    const token = get(jwtAtom);

    if (!token) return null;

    try {
        const user = await authApi.whoAmI();
        return user;
    } catch {
        return null;
    }
});

export const useAuth = () => {
    const [token, setToken] = useAtom(jwtAtom);
    const [user] = useAtom(userAtom);
    const navigate = useNavigate();

    // Login: call API, save token, redirect by role from JWT
    const login = async (credentials: Credentials) => {
        const result = await authApi.login(credentials);

        // Save token in jotai atom (and sessionStorage via storage adapter)
        setToken(result.token);

        // Decode role directly from JWT payload
        try {
            const base64 = result.token
                .split(".")[1]
                .replace(/-/g, "+")
                .replace(/_/g, "/");

            const json = atob(base64);
            const payload = JSON.parse(json);

            const role: string | undefined = payload.Role ?? payload.role;

            if (role === "Admin") {
                navigate("/admin");
            } else {
                navigate("/player");
            }
        } catch (e) {
            console.error("Failed to decode JWT", e);
            navigate("/login");
        }
    };

    // Logout: clear token and redirect to login page
    const logout = () => {
        setToken(null);
        navigate("/login");
    };

    return {
        token,
        user,
        login,
        logout,
    };
};
