// client/src/pages/player/PlayerGuard.tsx
import type { ReactNode } from "react";
import { useAuth } from "../../atoms/auth";

type PlayerGuardProps = {
    children: ReactNode;
};

export default function PlayerGuard({ children }: PlayerGuardProps) {
    const { user, token } = useAuth();
    const role = user?.role;

    // 1) користувач взагалі не залогінений
    if (!token) {
        return (
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-10">
                    <h1 className="mb-2 text-2xl font-bold text-slate-900">
                        Please log in
                    </h1>
                    <p className="text-sm text-slate-500">
                        You must log in as a player to view this page.
                    </p>
                </div>
            </div>
        );
    }

    // 2) токен є, але whoAmI ще не повернув юзера
    if (token && !user) {
        return (
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-10">
                    <p className="text-sm text-slate-500">
                        Loading your player data...
                    </p>
                </div>
            </div>
        );
    }

    // 3) юзер залогінений, але роль не "User" (тобто не Player)
    if (role !== "User") {
        return (
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-10">
                    <h1 className="mb-2 text-2xl font-bold text-slate-900">
                        Access denied
                    </h1>
                    <p className="text-sm text-slate-500">
                        Only players can access this page.
                    </p>
                </div>
            </div>
        );
    }

    // 4) все ок — показуємо справжню сторінку
    return <>{children}</>;
}
