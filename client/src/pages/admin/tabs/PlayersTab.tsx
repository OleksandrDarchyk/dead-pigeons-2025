// src/components/admin/PlayersTab.tsx
import { useEffect, useState } from "react";
import type { Player } from "../../../core/generated-client";
import { playersApi } from "../../../utilities/playersApi";
import { transactionsApi } from "../../../utilities/transactionsApi";
import toast from "react-hot-toast";



type BalanceMap = Record<string, number>;

export default function PlayersTab() {
    const [players, setPlayers] = useState<Player[]>([]);
    const [balances, setBalances] = useState<BalanceMap>({});
    const [isLoading, setIsLoading] = useState(true);

    // завантажуємо список гравців + їх баланс
    const loadData = async () => {
        try {
            setIsLoading(true);

            const list = await playersApi.getPlayers(null);
            setPlayers(list);

            // паралельно тягнемо баланс для кожного гравця
            const entries = await Promise.all(
                list.map(async (p) => {
                    try {
                        const balanceDto = await transactionsApi.getPlayerBalance(
                            p.id
                        );
                        return [p.id, balanceDto.balance] as const;
                    } catch {
                        return [p.id, 0] as const;
                    }
                })
            );

            setBalances(Object.fromEntries(entries));
        } catch (err) {
            console.error(err);
            toast.error("Failed to load players");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        void loadData();
    }, []);

    const toggleActive = async (player: Player) => {
        try {
            if (player.isactive) {
                await playersApi.deactivatePlayer(player.id);
                toast.success("Player deactivated");
            } else {
                await playersApi.activatePlayer(player.id);
                toast.success("Player activated");
            }
            await loadData();
        } catch (err) {
            console.error(err);
            toast.error("Failed to update player status");
        }
    };

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-900">
                        Player Management
                    </h2>
                    <p className="text-xs text-slate-500">
                        Manage all registered players
                    </p>
                </div>

                {/* TODO: додати кнопку Add Player, коли зробиш форму */}
                {/* <button className="btn btn-sm rounded-full bg-slate-900 text-white">
                    Add Player
                </button> */}
            </div>

            {isLoading ? (
                <p className="text-sm text-slate-500">Loading players...</p>
            ) : players.length === 0 ? (
                <p className="text-sm text-slate-500">
                    No players yet. Add your first player to get started.
                </p>
            ) : (
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                        <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                            <th className="py-2 pr-4">Name</th>
                            <th className="py-2 pr-4">Phone</th>
                            <th className="py-2 pr-4">Email</th>
                            <th className="py-2 pr-4">Balance</th>
                            <th className="py-2 pr-4">Status</th>
                            <th className="py-2 pr-4">Joined</th>
                            <th className="py-2 pr-4 text-right">Actions</th>
                        </tr>
                        </thead>
                        <tbody>
                        {players.map((p) => {
                            const balance = balances[p.id] ?? 0;
                            const created =
                                p.createdat &&
                                new Date(p.createdat).toLocaleDateString();

                            return (
                                <tr
                                    key={p.id}
                                    className="border-b border-slate-100 last:border-0"
                                >
                                    <td className="py-3 pr-4 font-medium text-slate-900">
                                        {p.fullname}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-700">
                                        {p.phone}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-700">
                                        {p.email}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-900">
                                        {balance.toFixed(2)} DKK
                                    </td>
                                    <td className="py-3 pr-4">
                                        {p.isactive ? (
                                            <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                                                    Active
                                                </span>
                                        ) : (
                                            <span className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
                                                    Inactive
                                                </span>
                                        )}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-500">
                                        {created ?? "–"}
                                    </td>
                                    <td className="py-3 pl-4 text-right">
                                        {/* TODO: Edit (форма) */}
                                        <button
                                            type="button"
                                            onClick={() => toggleActive(p)}
                                            className={
                                                "ml-2 inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold " +
                                                (p.isactive
                                                    ? "border-red-300 text-red-700 hover:bg-red-50"
                                                    : "border-green-300 text-green-700 hover:bg-green-50")
                                            }
                                        >
                                            {p.isactive
                                                ? "Deactivate"
                                                : "Activate"}
                                        </button>
                                    </td>
                                </tr>
                            );
                        })}
                        </tbody>
                    </table>
                </div>
            )}
        </section>
    );
}
