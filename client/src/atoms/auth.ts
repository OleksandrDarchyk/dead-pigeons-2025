import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from "../utilities/authApi";

export type Credentials = {
    email: string;
    password: string;
};

export const TOKEN_KEY = "jwt";
const storage = createJSONStorage<string | null>(() => sessionStorage);

// Token зберігається у sessionStorage (одне місце)
export const jwtAtom = atomWithStorage<string | null>(TOKEN_KEY, null, storage);

// User атом — запит WhoAmI коли токен змінюється
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

    const login = async (credentials: Credentials) => {
        const result = await authApi.login(credentials);

        // Зберігаємо ТІЛЬКИ в sessionStorage
        setToken(result.token);
        sessionStorage.setItem(TOKEN_KEY, result.token);

        const claims = await authApi.whoAmI();

        if (claims.role === "Admin") {
            navigate("/admin");
        } else {
            navigate("/player");
        }
    };

    const logout = () => {
        setToken(null);
        sessionStorage.removeItem(TOKEN_KEY);
        navigate("/login");
    };

    return {
        token,
        user,
        login,
        logout,
    };
};
