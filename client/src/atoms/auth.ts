import { atom } from "jotai";
import { atomWithStorage, createJSONStorage } from "jotai/utils";
import { useAtom } from "jotai";
import { useNavigate } from "react-router-dom";
import { authApi } from '../utilities/authApi';


// Types for login credentials
export type Credentials = {
    email: string;
    password: string;
};

// Where the token will be stored (sessionStorage is safer for exams)
export const TOKEN_KEY = "jwt";
const storage = createJSONStorage<string | null>(() => sessionStorage);

// Atom for the JWT token
export const jwtAtom = atomWithStorage<string | null>(TOKEN_KEY, null, storage);

// Atom that fetches "WhoAmI" when token changes
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

    // LOGIN LOGIC
    const login = async (credentials: Credentials) => {
        const result = await authApi.login(credentials);

        // save token to Jotai (sessionStorage)
        setToken(result.token);

        // also save plain token to localStorage for customFetch
        localStorage.setItem(TOKEN_KEY, result.token);

        // get user info
        const claims = await authApi.whoAmI();

        // redirect by role
        if (claims.role === "Admin") {
            navigate("/admin");
        } else {
            navigate("/player");
        }
    };

    const logout = () => {
        setToken(null);
        localStorage.removeItem(TOKEN_KEY); // очистити localStorage теж
        navigate("/login");
    };


    return {
        token,
        user,
        login,
        logout,
    };
};
