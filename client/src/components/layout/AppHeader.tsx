// src/components/layout/AppHeader.tsx
import { Link } from 'react-router-dom';

export default function AppHeader() {
    return (
        <header className="bg-white/80 backdrop-blur border-b border-slate-200 text-slate-900">
            <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
                {/* Logo + title */}
                <Link to="/" className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-900 font-bold text-white">
                        <span className="text-sm">DP</span>
                    </div>
                    <div className="leading-tight">
                        <div className="text-sm font-semibold text-slate-900">
                            Dead Pigeons
                        </div>
                        <div className="text-xs text-slate-400">
                            Jerne IF
                        </div>
                    </div>
                </Link>

                {/* Simple navigation */}
                <nav className="flex items-center gap-4">
                    {/* TODO: Replace with NavLink when you want active state styling. */}
                    <Link
                        to="/"
                        className="text-sm font-medium text-slate-600 hover:text-slate-900"
                    >
                        Home
                    </Link>

                    {/* TODO: Connect this button with the real /login route later. */}
                    <Link
                        to="/login"
                        className="rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800 shadow-sm"
                    >
                        Login
                    </Link>
                </nav>
            </div>
        </header>
    );
}
