// src/pages/auth/Login.tsx
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useAuth, type LoginCredentials } from "../../atoms/auth";
import toast from "react-hot-toast";

export default function Login() {
    const { login } = useAuth();

    // Local state for the form
    const [form, setForm] = useState<LoginCredentials>({
        email: "",
        password: "",
    });

    const [isLoading, setIsLoading] = useState(false);

    // Async submit handler: validates form, calls global login hook and shows errors
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!form.email || !form.password) {
            toast.error("Please enter email and password");
            return;
        }

        try {
            setIsLoading(true);

            // Use global auth hook
            await login(form);
            // On success, the hook will navigate to /admin or /player
        } catch (err) {
            console.error("Login failed", err);

            let message = "Login failed";

            try {
                // NSwag ApiException usually має поле `response` з JSON-строкою
                const apiErr = err as { response?: string };

                if (apiErr.response) {
                    const body = JSON.parse(apiErr.response) as {
                        detail?: string;
                        title?: string;
                    };

                    const detail =
                        typeof body.detail === "string" &&
                        body.detail.trim()
                            ? body.detail.trim()
                            : null;

                    const title =
                        typeof body.title === "string" &&
                        body.title.trim()
                            ? body.title.trim()
                            : null;

                    if (detail) {
                        message = detail;
                    } else if (title) {
                        message = title;
                    }
                }
            } catch (parseError) {
                // Якщо не вийшло прочитати JSON – залишаємо дефолтне повідомлення
                console.error(
                    "Failed to parse login error response",
                    parseError,
                );
            }

            // Показуємо помилку як native bubble біля email
            const emailInput = document.querySelector<HTMLInputElement>(
                'input[type="email"]',
            );
            if (emailInput) {
                emailInput.setCustomValidity(message);
                emailInput.reportValidity();
            }

            // І плюс toast (як раніше)
            toast.error(message);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center bg-gradient-to-b from-[#e5f0ff] to-white">
            <div className="w-full max-w-md rounded-3xl bg-white/95 shadow-xl border border-slate-200 px-8 py-10 text-slate-900">
                <h1 className="text-3xl font-extrabold text-center mb-1">
                    Dead Pigeons
                </h1>
                <p className="text-center text-xs uppercase tracking-wide text-slate-400 mb-6">
                    Jerne IF Lottery Login
                </p>

                <form
                    onSubmit={(e: FormEvent<HTMLFormElement>) => {
                        void handleSubmit(e);
                    }}
                    className="space-y-5"
                >
                    <div className="form-control">
                        <label className="label px-0 pb-1">
                            <span className="text-xs font-semibold text-slate-700">
                                Email
                            </span>
                        </label>
                        <input
                            type="email"
                            className="input input-bordered w-full bg-slate-50 text-sm"
                            placeholder="you@example.com"
                            value={form.email}
                            onChange={(e) => {
                                // При зміні email забираємо старе повідомлення помилки
                                e.target.setCustomValidity("");
                                setForm({
                                    ...form,
                                    email: e.target.value,
                                });
                            }}
                        />
                    </div>

                    <div className="form-control">
                        <label className="label px-0 pb-1">
                            <span className="text-xs font-semibold text-slate-700">
                                Password
                            </span>
                        </label>
                        <input
                            type="password"
                            className="input input-bordered w-full bg-slate-50 text-sm"
                            placeholder="********"
                            value={form.password}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    password: e.target.value,
                                })
                            }
                        />
                    </div>

                    <button
                        type="submit"
                        className="btn w-full rounded-full bg-slate-900 text-white border-none hover:bg-slate-800"
                        disabled={isLoading}
                    >
                        {isLoading ? "Logging in..." : "Login"}
                    </button>
                </form>

                {/* Link to registration page */}
                <div className="mt-4 text-center">
                    <span className="text-xs text-slate-500 mr-1">
                        Need an account?
                    </span>
                    <Link
                        to="/register"
                        className="text-xs font-semibold text-slate-900 underline-offset-2 hover:underline"
                    >
                        Register
                    </Link>
                </div>

                <p className="mt-6 text-center text-xs text-slate-400">
                    Access is provided by Jerne IF administrator.
                </p>
            </div>
        </div>
    );
}
