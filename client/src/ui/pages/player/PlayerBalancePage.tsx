import { useAuth } from "@core/state/auth";
import { usePlayerBalance } from "@hooks/usePlayerBalance";
import PlayerPaymentsSection from "./PlayerPaymentsSection";
import PlayerGuard from "./PlayerGuard";
import PlayerTabs from "./PlayerTabs";

export default function PlayerBalancePage() {
    const { user, token } = useAuth();
    const role = user?.role;

    const isPlayer = Boolean(token && user && role === "User");

    const {
        balance,
        isLoading: isBalanceLoading,
    } = usePlayerBalance(isPlayer);

    return (
        <PlayerGuard>
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl px-4 py-8 space-y-6">
                    {/* HEADLINE + TABS */}
                    <header className="pb-1">
                        <h1 className="text-2xl font-bold text-slate-900">
                            Player Dashboard
                        </h1>
                        <p className="mt-1 text-sm text-slate-500">
                            Logged in as{" "}
                            <span className="font-medium">
                                {user?.email}
                            </span>
                        </p>

                        <PlayerTabs />
                    </header>

                    {/* Balance card */}
                    <section className="rounded-2xl border border-red-100 bg-red-50 px-6 py-5 shadow-sm">
                        <p className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-red-500">
                            <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-white text-red-500 shadow-sm">
                                💳
                            </span>
                            Current Balance
                        </p>

                        <div className="mt-3">
                            {isBalanceLoading && balance === null ? (
                                <p className="text-sm text-slate-600">
                                    Loading balance...
                                </p>
                            ) : (
                                <p className="text-4xl font-extrabold text-red-600">
                                    {(balance ?? 0).toFixed(2)}{" "}
                                    <span className="text-2xl">DKK</span>
                                </p>
                            )}
                            <p className="mt-1 text-xs text-slate-700">
                                Approved MobilePay payments minus the cost of
                                your boards.
                            </p>
                        </div>
                    </section>

                    {/* Payments form + history */}
                    <PlayerPaymentsSection />
                </div>
            </div>
        </PlayerGuard>
    );
}
