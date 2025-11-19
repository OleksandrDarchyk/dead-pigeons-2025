// src/components/auth/Login.tsx
import { useState, type FormEvent } from "react";
import { useAuth, type Credentials } from "../../atoms/auth";
import toast from "react-hot-toast";

export default function Login() {
    const { login } = useAuth();

    // local state for the form
    const [form, setForm] = useState<Credentials>({
        email: "",
        password: "",
    });

    const [isLoading, setIsLoading] = useState(false);

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!form.email || !form.password) {
            toast.error("Please enter email and password");
            return;
        }

        try {
            setIsLoading(true);

            // call global auth hook
            await login(form);

            // if no error – the hook will navigate to /admin or /player
        } catch (err) {
            console.error(err);
            toast.error("Login failed");
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

                <form onSubmit={handleSubmit} className="space-y-5">
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
                            onChange={(e) =>
                                setForm({ ...form, email: e.target.value })
                            }
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
                                setForm({ ...form, password: e.target.value })
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

                <p className="mt-6 text-center text-xs text-slate-400">
                    Access is provided by Jerne IF administrator.
                </p>
            </div>
        </div>
    );
}
