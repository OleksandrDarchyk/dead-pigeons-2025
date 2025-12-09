// src/pages/auth/Register.tsx
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../atoms/auth";
import toast from "react-hot-toast";

type RegisterForm = {
    email: string;
    password: string;
    confirmPassword: string;
};

export default function Register() {
    const { register } = useAuth();

    const [form, setForm] = useState<RegisterForm>({
        email: "",
        password: "",
        confirmPassword: "",
    });

    const [isLoading, setIsLoading] = useState(false);

    // Handles all validation + API call for registration
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!form.email || !form.password || !form.confirmPassword) {
            toast.error("Please fill in all fields");
            return;
        }

        if (form.password.length < 8) {
            toast.error("Password must be at least 8 characters");
            return;
        }

        if (form.password !== form.confirmPassword) {
            toast.error("Passwords do not match");
            return;
        }

        try {
            setIsLoading(true);

            // Only email + passwords are sent to the API
            await register({
                email: form.email.trim(),
                password: form.password,
                confirmPassword: form.confirmPassword,
            });

            // On success, the auth hook will log the user in and redirect
        } catch (err) {
            console.error("Registration failed", err);

            let message = "Registration failed";

            try {
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
                console.error(
                    "Failed to parse registration error response",
                    parseError,
                );
            }

            const emailInput = document.querySelector<HTMLInputElement>(
                'input[type="email"]',
            );
            if (emailInput) {
                emailInput.setCustomValidity(message);
                emailInput.reportValidity();
            }

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
                    Create your Dead Pigeons account
                </p>

                <form
                    // Wrapper keeps React handler type as () => void, and we explicitly ignore the Promise from async handler
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
                                // Clear any previous native validation message
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

                    <div className="form-control">
                        <label className="label px-0 pb-1">
                            <span className="text-xs font-semibold text-slate-700">
                                Confirm password
                            </span>
                        </label>
                        <input
                            type="password"
                            className="input input-bordered w-full bg-slate-50 text-sm"
                            placeholder="********"
                            value={form.confirmPassword}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    confirmPassword: e.target.value,
                                })
                            }
                        />
                    </div>

                    <button
                        type="submit"
                        className="btn w-full rounded-full bg-slate-900 text-white border-none hover:bg-slate-800"
                        disabled={isLoading}
                    >
                        {isLoading ? "Creating account..." : "Create account"}
                    </button>
                </form>

                <div className="mt-4 text-center">
                    <span className="text-xs text-slate-500 mr-1">
                        Already have an account?
                    </span>
                    <Link
                        to="/login"
                        className="text-xs font-semibold text-slate-900 underline-offset-2 hover:underline"
                    >
                        Login
                    </Link>
                </div>

                <p className="mt-6 text-center text-xs text-slate-400">
                    You can only register if a Jerne IF admin has added you as a
                    player.
                </p>
            </div>
        </div>
    );
}
