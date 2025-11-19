// src/components/layout/AppHeader.tsx
import { Link } from 'react-router-dom';

export default function AppHeader() {
    return (
        <header className="bg-[#111827] text-white">
            <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
                {/* Logo + title */}
                <Link to="/" className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-red-600 font-bold">
                        <span className="text-sm">DP</span>
                    </div>
                    <div className="leading-tight">
                        <div className="text-sm font-semibold">Dead Pigeons</div>
                        <div className="text-xs text-gray-300">Jerne IF</div>
                    </div>
                </Link>

                {/* Simple navigation */}
                <nav className="flex items-center gap-4">
                    {/* TODO: Replace with NavLink when you want active state styling. */}
                    <Link
                        to="/"
                        className="text-sm font-medium text-gray-200 hover:text-white"
                    >
                        Home
                    </Link>

                    {/* TODO: Connect this button with the real /login route later. */}
                    <Link
                        to="/login"
                        className="rounded-md bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-500"
                    >
                        Login
                    </Link>
                </nav>
            </div>
        </header>
    );
}
