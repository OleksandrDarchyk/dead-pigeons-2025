import { useEffect, useState } from "react";
import {
    type LoginRequestDto,
    type RegisterRequestDto,
    type JwtClaims,
} from "@core/generated-client.ts";
import { authApi } from "@utilities/authApi.ts";
import toast from "react-hot-toast";

export default function Auth() {
    // state for register form
    const [registerForm, setRegisterForm] = useState<RegisterRequestDto>({
        email: "",
        password: "",
    });

    // state for login form
    const [loginForm, setLoginForm] = useState<LoginRequestDto>({
        email: "",
        password: "",
    });

    // holds WhoAmI result
    const [claims, setClaims] = useState<JwtClaims | null>(null);

    // call WhoAmI
    async function loadClaims() {
        try {
            const result = await authApi.whoAmI();
            setClaims(result);
        } catch {
            setClaims(null);
        }
    }

    // register
    async function handleRegister() {
        try {
            const res = await authApi.register(registerForm);
            localStorage.setItem("jwt", res.token); // save token
            toast.success("Registered");
            await loadClaims();
        } catch (e) {
            console.error(e);
            toast.error("Register failed");
        }
    }

    // login
    async function handleLogin() {
        try {
            const res = await authApi.login(loginForm);
            localStorage.setItem("jwt", res.token); // save token
            toast.success("Logged in");
            await loadClaims();
        } catch (e) {
            console.error(e);
            toast.error("Login failed");
        }
    }

    // on mount: if token exists, try WhoAmI
    useEffect(() => {
        const token = localStorage.getItem("jwt");
        if (token) {
            loadClaims().catch(() => {});
        }
    }, []);

    return (
        <div className="flex flex-col gap-4 max-w-md mx-auto">
            {/* Register */}
            <div className="flex flex-col gap-2 border rounded-xl p-4">
                <h2 className="font-semibold">Register</h2>
                <input
                    className="input"
                    type="email"
                    placeholder="email"
                    value={registerForm.email}
                    onChange={(e) =>
                        setRegisterForm({ ...registerForm, email: e.target.value })
                    }
                />
                <input
                    className="input"
                    type="password"
                    placeholder="password (min 8 chars)"
                    value={registerForm.password}
                    onChange={(e) =>
                        setRegisterForm({ ...registerForm, password: e.target.value })
                    }
                />
                <button
                    className="btn btn-primary"
                    disabled={registerForm.password.length < 8}
                    onClick={handleRegister}
                >
                    Register
                </button>
            </div>

            {/* Login */}
            <div className="flex flex-col gap-2 border rounded-xl p-4">
                <h2 className="font-semibold">Login</h2>
                <input
                    className="input"
                    type="email"
                    placeholder="email"
                    value={loginForm.email}
                    onChange={(e) =>
                        setLoginForm({ ...loginForm, email: e.target.value })
                    }
                />
                <input
                    className="input"
                    type="password"
                    placeholder="password"
                    value={loginForm.password}
                    onChange={(e) =>
                        setLoginForm({ ...loginForm, password: e.target.value })
                    }
                />
                <button className="btn btn-secondary" onClick={handleLogin}>
                    Login
                </button>
            </div>

            {/* Current user */}
            <div className="flex flex-col gap-2 border rounded-xl p-4">
                <h2 className="font-semibold">Current user</h2>
                <button className="btn" onClick={loadClaims}>
                    Refresh WhoAmI
                </button>
                {claims ? (
                    <div>user id: {claims.id}</div>
                ) : (
                    <div>No user loaded</div>
                )}
            </div>
        </div>
    );
}
