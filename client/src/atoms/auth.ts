// src/atoms/auth.ts
import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "../utilities/authApi";
import type { JwtClaims } from "@core/generated-client";

// Type for login
export type LoginCredentials = {
    email: string;
    password: string;
};

// Type for register
export type RegisterCredentials = {
    email: string;
    password: string;
    confirmPassword: string;
};

export const TOKEN_KEY = "jwt";

const tokenStorage = createJSONStorage<string | null>(() => sessionStorage);

export const jwtAtom = atomWithStorage<string | null>(
    TOKEN_KEY,
    null,
    tokenStorage
);

export const userAtom = atom(async (get): Promise<JwtClaims | null> => {
    const token = get(jwtAtom);
    if (!token) return null;

    try {
        const user = await authApi.whoAmI();
        return user;
    } catch (error) {
        console.error("Failed to load current user from WhoAmI", error);
        return null;
    }
});

export const useAuth = () => {
    const [token, setToken] = useAtom(jwtAtom);
    const [user] = useAtom(userAtom);
    const navigate = useNavigate();

    // Login: email + password
    const login = async (credentials: LoginCredentials) => {
        const result = await authApi.login(credentials);
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

    // Register: email + password + confirmPassword
    const register = async (data: RegisterCredentials) => {
        const result = await authApi.register({
            email: data.email,
            password: data.password,
            confirmPassword: data.confirmPassword,
        });

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
            console.error(
                "Failed to fetch current user info after registration",
                error
            );
            navigate("/login");
        }
    };

    const logout = () => {
        setToken(null);
        navigate("/login");
    };

    return {
        token,
        user,
        login,
        register,
        logout,
    };
};
