// client/src/pages/player/PlayerTabs.tsx
import { NavLink } from "react-router-dom";

export default function PlayerTabs() {
    const tabs = [
        { to: "/player", label: "Dashboard" },
        { to: "/balance", label: "Balance & Payments" },
        { to: "/history", label: "Games History" },
    ];

    return (
        <nav className="mt-4 border-b border-slate-200">
            <div className="flex gap-8 text-sm">
                {tabs.map((tab) => (
                    <NavLink
                        key={tab.to}
                        to={tab.to}
                        end={tab.to === "/player"}
                        className={({ isActive }) =>
                            "pb-3 -mb-px border-b-2 font-medium transition-colors " +
                            (isActive
                                ? "border-slate-900 text-slate-900"
                                : "border-transparent text-slate-500 hover:text-slate-800")
                        }
                    >
                        {tab.label}
                    </NavLink>
                ))}
            </div>
        </nav>
    );
}
