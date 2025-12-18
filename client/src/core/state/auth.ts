
import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "@core/api/authApi";
import type { JwtClaims } from "@core/api/generated/generated-client";

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

        const me = await authApi.whoAmI();
        return me;
    } catch (err) {
        console.error("Failed to load user via WhoAmI", err);
        return null;
    }
});

export const useAuth = () => {
    const [token, setToken] = useAtom(jwtAtom);
    const [user] = useAtom(userAtom);
    const navigate = useNavigate();

    // Login + redirect based on role
    const login = async (creds: LoginCredentials): Promise<void> => {
        const result = await authApi.login(creds);

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

    const register = async (creds: RegisterCredentials): Promise<void> => {
        const result = await authApi.register(creds);

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

    const logout = (): void => {
        setToken(null);
        void navigate("/login");
    };

    return { token, user, login, register, logout };
};
