import { useState } from "react";
import { useAuth } from "@core/state/auth";

import PlayersTab from "./tabs/PlayersTab";
import PaymentsTab from "./tabs/PaymentsTab";
import WinningNumbersTab from "./tabs/WinningNumbersTab";
import BoardsStatsTab from "./tabs/BoardsStatsTab";

type AdminTab = "players" | "payments" | "winning" | "boards";

function TabButton(props: {
    label: string;
    isActive: boolean;
    onClick: () => void;
}) {
    const { label, isActive, onClick } = props;

    return (
        <button
            type="button"
            onClick={onClick}
            className={
                "px-4 py-2 text-sm font-medium border-b-2 -mb-px " +
                (isActive
                    ? "border-slate-900 text-slate-900"
                    : "border-transparent text-slate-500 hover:text-slate-800")
            }
        >
            {label}
        </button>
    );
}

export default function AdminDashboardPage() {
    const { user, token } = useAuth();
    const [tab, setTab] = useState<AdminTab>("players");

    const role = user?.role;

    if (!token) {
        return (
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-10">
                    <h1 className="text-2xl font-bold text-slate-900 mb-2">
                        Please log in
                    </h1>
                    <p className="text-sm text-slate-500">
                        You must log in as an administrator to view this page.
                    </p>
                </div>
            </div>
        );
    }

    if (token && !user) {
        return (
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-10">
                    <p className="text-sm text-slate-500">
                        Loading admin data...
                    </p>
                </div>
            </div>
        );
    }

    if (role !== "Admin") {
        return (
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-10">
                    <h1 className="text-2xl font-bold text-slate-900 mb-2">
                        Access denied
                    </h1>
                    <p className="text-sm text-slate-500">
                        You must be an administrator to view this page.
                    </p>
                </div>
            </div>
        );
    }

    // Admin view
    return (
        <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
            <div className="mx-auto max-w-5xl px-4 py-8">
                <h1 className="text-2xl font-bold text-slate-900 mb-6">
                    Admin Dashboard
                </h1>

                {/* Tabs */}
                <div className="flex gap-4 border-b border-slate-200 mb-6">
                    <TabButton
                        label="Players"
                        isActive={tab === "players"}
                        onClick={() => setTab("players")}
                    />
                    <TabButton
                        label="Payments"
                        isActive={tab === "payments"}
                        onClick={() => setTab("payments")}
                    />
                    <TabButton
                        label="Winning Numbers"
                        isActive={tab === "winning"}
                        onClick={() => setTab("winning")}
                    />
                    <TabButton
                        label="Boards & Stats"
                        isActive={tab === "boards"}
                        onClick={() => setTab("boards")}
                    />
                </div>

                {/* Tab content */}
                {tab === "players" && <PlayersTab />}
                {tab === "payments" && <PaymentsTab />}
                {tab === "winning" && <WinningNumbersTab />}
                {tab === "boards" && <BoardsStatsTab />}
            </div>
        </div>
    );
}
