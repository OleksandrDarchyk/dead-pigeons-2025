// src/components/layout/AppHeader.tsx
import { Link } from "react-router-dom";
import { useAuth } from "@core/state/auth";


export default function AppHeader() {
    const { user, logout } = useAuth();

    const email = user?.email;

    const handleLogout = () => {
        logout();
    };

    return (
        <header className="bg-slate-900 text-white">
            <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
                {/* Logo + title */}
                <Link to="/" className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-red-600 font-bold">
                        <span className="text-sm">DP</span>
                    </div>
                    <div className="leading-tight">
                        <div className="text-sm font-semibold">Dead Pigeons</div>
                        <div className="text-xs text-slate-300">Jerne IF</div>
                    </div>
                </Link>

                {/* Navigation + auth */}
                <nav className="flex items-center gap-4 text-sm">
                    <Link
                        to="/"
                        className="font-medium text-slate-200 hover:text-white"
                    >
                        Home
                    </Link>

                    {email ? (
                        <>
                            <span className="hidden sm:inline text-xs text-slate-300">
                                {email}
                            </span>
                            <button
                                type="button"
                                onClick={handleLogout}
                                className="rounded-full bg-slate-100 px-4 py-1.5 text-xs font-semibold text-slate-900 hover:bg-white shadow-sm"
                            >
                                Logout
                            </button>
                        </>
                    ) : (
                        <Link
                            to="/login"
                            className="rounded-full bg-white px-4 py-1.5 text-xs font-semibold text-slate-900 hover:bg-slate-100 shadow-sm"
                        >
                            Login
                        </Link>
                    )}
                </nav>
            </div>
        </header>
    );
}
