// client/src/pages/player/PlayerBuyBoardPage.tsx
import { useAuth } from "../../atoms/auth";
import PlayerGuard from "./PlayerGuard";
import { usePlayerBoards } from "../../hooks/usePlayerBoards";
import PlayerTabs from "./PlayerTabs";
import { usePlayerBalance } from "../../hooks/usePlayerBalance";

export default function PlayerBuyBoardPage() {
    const { user, token } = useAuth();
    const role = user?.role;
    const isPlayer = Boolean(token && user && role === "User");

    const {
        activeGame,
        form,
        toggleNumber,
        submitBoard,
        currentPrice,
        totalPrice,
        isSaving,
        setRepeatEnabled,
        setRepeatWeeks,
    } = usePlayerBoards(isPlayer);

    const {
        balance,
        isLoading: isBalanceLoading,
        reloadBalance, // used to refresh balance after buying a board
    } = usePlayerBalance(isPlayer);

    const numbersCount = form.selectedNumbers.length;

    // Repeat is invalid when the user enters something outside 1–52
    const isRepeatInvalid =
        form.repeatEnabled && (form.repeatWeeks < 1 || form.repeatWeeks > 52);

    // Do not allow buying a board if the total price (with repeat)
    // is higher than the current balance.
    const notEnoughBalance =
        totalPrice !== null &&
        !isBalanceLoading &&
        balance !== null &&
        totalPrice > balance;

    // After creating a board, also reload the balance from the server
    const handleConfirmBoard = async () => {
        await submitBoard();
        await reloadBalance();
    };

    return (
        <PlayerGuard>
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl space-y-6 px-4 py-8">
                    {/* HEADER WITH TABS (same look as Balance & Payments) */}
                    <header className="pb-1">
                        <h1 className="text-2xl font-bold text-slate-900">
                            Player Dashboard
                        </h1>
                        <p className="mt-1 text-sm text-slate-500">
                            Logged in as{" "}
                            <span className="font-medium">{user?.email}</span>
                        </p>

                        <PlayerTabs />
                    </header>

                    {/* BUY BOARD CARD */}
                    <section className="rounded-2xl border border-slate-200 bg-white px-6 py-5 shadow-sm">
                        <h2 className="mb-1 text-lg font-semibold text-slate-900">
                            Buy a Board
                        </h2>
                        <p className="mb-4 text-xs text-slate-500">
                            Pick between 5 and 8 numbers from 1–16 for this
                            week&apos;s round.
                        </p>

                        {!activeGame ? (
                            <p className="text-sm text-slate-500">
                                You can buy a board when there is an active
                                round.
                            </p>
                        ) : (
                            <>
                                {/* ACTIVE ROUND INFO */}
                                <p className="text-xs font-semibold uppercase tracking-wide text-slate-600">
                                    Active Round
                                </p>
                                <p className="mt-1 text-xs text-slate-500">
                                    Current week&apos;s lottery round
                                </p>
                                <p className="mt-2 text-sm font-semibold text-slate-900">
                                    Week {activeGame.weekNumber},{" "}
                                    {activeGame.year}
                                </p>
                                <div className="mt-2 inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                                    Active
                                </div>

                                {/* NUMBER GRID */}
                                <div className="mt-4 grid grid-cols-4 gap-2 sm:grid-cols-8">
                                    {Array.from({ length: 16 }, (_, i) => i + 1).map(
                                        (num) => {
                                            const isSelected =
                                                form.selectedNumbers.includes(num);

                                            return (
                                                <button
                                                    key={num}
                                                    type="button"
                                                    onClick={() =>
                                                        toggleNumber(num)
                                                    }
                                                    className={
                                                        "inline-flex h-10 w-10 items-center justify-center rounded-lg border text-sm font-semibold transition " +
                                                        (isSelected
                                                            ? "border-red-600 bg-red-600 text-white shadow-sm"
                                                            : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50")
                                                    }
                                                    disabled={isSaving}
                                                >
                                                    {num}
                                                </button>
                                            );
                                        }
                                    )}
                                </div>

                                {/* SUMMARY + REPEAT + TOTAL */}
                                <div className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
                                    <div className="w-full space-y-3 text-xs text-slate-600">
                                        <div>
                                            <p>
                                                Selected numbers:{" "}
                                                <span className="font-semibold text-slate-900">
                                                    {numbersCount}
                                                </span>{" "}
                                                / 8
                                            </p>
                                            <p>
                                                Price per week:{" "}
                                                <span className="font-semibold text-slate-900">
                                                    {currentPrice !== null
                                                        ? `${currentPrice.toFixed(
                                                            2
                                                        )} DKK`
                                                        : "–"}
                                                </span>
                                            </p>
                                        </div>

                                        <div className="h-px bg-slate-200" />

                                        {/* REPEAT BLOCK */}
                                        <label className="flex items-start gap-2">
                                            <input
                                                type="checkbox"
                                                className="mt-0.5 h-4 w-4 rounded border-slate-300"
                                                checked={form.repeatEnabled}
                                                onChange={(e) =>
                                                    setRepeatEnabled(
                                                        e.target.checked
                                                    )
                                                }
                                            />
                                            <div>
                                                <p className="text-xs font-semibold text-slate-900">
                                                    Repeat this board
                                                </p>
                                                <p className="text-[11px] text-slate-500">
                                                    Automatically join the game
                                                    for multiple weeks.
                                                </p>
                                            </div>
                                        </label>

                                        {form.repeatEnabled && (
                                            <div className="space-y-1">
                                                <div className="mt-2 flex items-center gap-3 text-xs">
                                                    <span>Number of weeks:</span>
                                                    <input
                                                        type="number"
                                                        min={1}
                                                        max={52}
                                                        value={form.repeatWeeks}
                                                        onChange={(e) => {
                                                            const value = Number(
                                                                e.target.value
                                                            );
                                                            setRepeatWeeks(
                                                                Number.isNaN(
                                                                    value
                                                                )
                                                                    ? 1
                                                                    : value
                                                            );
                                                        }}
                                                        className="w-20 rounded-lg border border-slate-300 px-2 py-1 text-sm"
                                                    />
                                                </div>
                                                {isRepeatInvalid && (
                                                    <p className="text-[11px] text-red-600">
                                                        Please choose between 1
                                                        and 52 weeks.
                                                    </p>
                                                )}
                                            </div>
                                        )}

                                        <div className="h-px bg-slate-200" />

                                        <p>
                                            Total price:{" "}
                                            <span className="font-semibold text-slate-900">
                                                {totalPrice !== null
                                                    ? `${totalPrice.toFixed(
                                                        2
                                                    )} DKK`
                                                    : "–"}
                                            </span>
                                        </p>

                                        <p>
                                            Your balance:{" "}
                                            <span className="font-semibold text-slate-900">
                                                {isBalanceLoading
                                                    ? "…"
                                                    : `${(
                                                        balance ?? 0
                                                    ).toFixed(2)} DKK`}
                                            </span>
                                        </p>

                                        {notEnoughBalance && (
                                            <p className="text-[11px] text-red-600">
                                                You do not have enough balance
                                                to buy this board with the
                                                selected repeat settings.
                                            </p>
                                        )}
                                    </div>

                                    {/* ACTION BUTTON */}
                                    <button
                                        type="button"
                                        onClick={() => void handleConfirmBoard()}
                                        disabled={
                                            isSaving ||
                                            !activeGame ||
                                            numbersCount < 5 ||
                                            numbersCount > 8 ||
                                            isRepeatInvalid ||
                                            notEnoughBalance
                                        }
                                        className="inline-flex items-center justify-center rounded-full bg-red-600 px-5 py-2 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-60 whitespace-nowrap"
                                    >
                                        {isSaving
                                            ? "Creating board..."
                                            : "Confirm board purchase"}
                                    </button>
                                </div>
                            </>
                        )}
                    </section>
                </div>
            </div>
        </PlayerGuard>
    );
}
