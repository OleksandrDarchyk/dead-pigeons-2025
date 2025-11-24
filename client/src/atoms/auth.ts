// src/atoms/auth.ts
import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "../utilities/authApi";
import type { JwtClaims } from "@core/generated-client";

// Types for login credentials
export type Credentials = {
    email: string;
    password: string;
};

// Storage key for JWT
export const TOKEN_KEY = "jwt";

// Use Jotai helper to store the token in sessionStorage
const tokenStorage = createJSONStorage<string | null>(() => sessionStorage);

// Atom for the JWT token (synced with sessionStorage)
export const jwtAtom = atomWithStorage<string | null>(
    TOKEN_KEY,
    null,
    tokenStorage
);

// Atom that loads current user (JwtClaims) when the token changes
export const userAtom = atom(async (get): Promise<JwtClaims | null> => {
    const token = get(jwtAtom);

    // No token => user is not logged in
    if (!token) return null;

    try {
        // Ask backend who is the current user (WhoAmI)
        const user = await authApi.whoAmI();
        return user;
    } catch (error) {
        console.error("Failed to load current user from WhoAmI", error);
        return null;
    }
});

// Hook that exposes auth functionality to React components
export const useAuth = () => {
    const [token, setToken] = useAtom(jwtAtom);
    const [user] = useAtom(userAtom);
    const navigate = useNavigate();

    // Login: call API, save token, then redirect by role
    const login = async (credentials: Credentials) => {
        const result = await authApi.login(credentials);

        // Save token in atom (and sessionStorage via tokenStorage)
        setToken(result.token);

        try {
            const me = await authApi.whoAmI();
            const role = me.role;

            if (role === "Admin") {
                navigate("/admin");
            } else {
                navigate("/player");
            }
        } catch (error) {
            console.error("Failed to fetch current user info after login", error);
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
