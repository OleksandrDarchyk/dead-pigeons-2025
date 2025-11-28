import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "../utilities/authApi";
import type { JwtClaims } from "@core/generated-client";

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

// Safe choice for exams: sessionStorage (prevents long-term token theft)
const tokenStorage = createJSONStorage<string | null>(() => sessionStorage);

// JWT is kept in sessionStorage and loaded via atomWithStorage
export const jwtAtom = atomWithStorage<string | null>(
    TOKEN_KEY,
    null,
    tokenStorage
);

// A derived atom: loads user info only when token exists
export const userAtom = atom(async (get): Promise<JwtClaims | null> => {
    const token = get(jwtAtom);
    if (!token) return null;

    try {
        const me = await authApi.whoAmI(); // now uses customFetch -> token sent correctly
        return me;
    } catch (err) {
        console.error("Failed to load user via WhoAmI", err);
        return null;
    }
});

// Exposed hook for UI
export const useAuth = () => {
    const [token, setToken] = useAtom(jwtAtom);
    const [user] = useAtom(userAtom);
    const navigate = useNavigate();

    const login = async (creds: LoginCredentials) => {
        const result = await authApi.login(creds);

        // 1) Save token
        setToken(result.token);

        // 2) Load user
        try {
            const me = await authApi.whoAmI();
            if (me.role === "Admin") navigate("/admin");
            else navigate("/player");
        } catch {
            navigate("/login");
        }
    };

    const register = async (creds: RegisterCredentials) => {
        const result = await authApi.register(creds);

        setToken(result.token);

        try {
            const me = await authApi.whoAmI();
            if (me.role === "Admin") navigate("/admin");
            else navigate("/player");
        } catch {
            navigate("/login");
        }
    };

    const logout = () => {
        setToken(null); // removes from sessionStorage
        navigate("/login");
    };

    return { token, user, login, register, logout };
};
