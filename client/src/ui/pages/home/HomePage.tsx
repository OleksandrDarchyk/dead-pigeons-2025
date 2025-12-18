import HeroSection from "./components/HeroSection.tsx";


function HowItWorksSection() {
    return (
        <section className="bg-white py-16">
            <div className="mx-auto max-w-5xl px-4">
                <h2 className="text-center text-3xl font-bold text-slate-900">
                    How It Works
                </h2>
                <p className="mt-2 text-center text-sm text-slate-500">
                    Simple weekly charity game where your numbers support Jerne IF.
                </p>

                <div className="mt-10 grid gap-6 md:grid-cols-3">
                    {/* Step 1 */}
                    <div className="rounded-2xl border border-slate-100 bg-white p-6 shadow-sm">
                        <div className="mb-3 text-3xl">🎯</div>
                        <h3 className="text-lg font-semibold text-slate-900">
                            Choose your numbers
                        </h3>
                        <p className="mt-2 text-sm text-slate-600">
                            Pick 5–8 numbers between 1 and 16 for your digital board.
                            You can buy one or many boards.
                        </p>
                    </div>

                    {/* Step 2 – UPDATED TEXT */}
                    <div className="rounded-2xl border border-slate-100 bg-white p-6 shadow-sm">
                        <div className="mb-3 text-3xl">📅</div>
                        <h3 className="text-lg font-semibold text-slate-900">
                            Weekly rounds
                        </h3>
                        <p className="mt-2 text-sm text-slate-600">
                            Every week an admin draws 3 winning numbers from a physical hat.
                            If your board contains all three winning numbers (in any order),
                            you win.
                        </p>
                    </div>

                    {/* Step 3 */}
                    <div className="rounded-2xl border border-slate-100 bg-white p-6 shadow-sm">
                        <div className="mb-3 text-3xl">❤️</div>
                        <h3 className="text-lg font-semibold text-slate-900">
                            Support Jerne IF
                        </h3>
                        <p className="mt-2 text-sm text-slate-600">
                            70% of all board revenue goes to prizes, 30% goes directly
                            to Jerne IF to support local sports activities.
                        </p>
                    </div>
                </div>
            </div>
        </section>
    );
}


function GameDetailsSection() {
    return (
        <section className="bg-slate-50 py-16">
            <div className="mx-auto max-w-5xl px-4">
                <h2 className="text-center text-3xl font-bold text-slate-900">
                    Game Details
                </h2>

                <div className="mt-8 rounded-2xl bg-white p-8 shadow-sm border border-slate-100">
                    <ul className="space-y-4 text-sm text-slate-700">
                        <li>
                            <span className="font-semibold">Number range: </span>
                            Choose from numbers 1–16. Each week the admin manually
                            enters the 3 winning numbers.
                        </li>
                        <li>
                            <span className="font-semibold">Board options: </span>
                            Each board has 5–8 numbers. Prices are 20 / 40 / 80 / 160 DKK
                            depending on how many numbers you pick.
                        </li>
                        <li>
                            <span className="font-semibold">Repeat boards: </span>
                            You can let a board repeat for several weeks in a row and
                            stop it any time before the weekly deadline.
                        </li>
                        <li>
                            <span className="font-semibold">Deadline: </span>
                            New boards can be bought until Saturday 17:00 (Danish time)
                            for the current week.
                        </li>
                        <li>
                            <span className="font-semibold">Balance: </span>
                            You first deposit money using MobilePay, then use your
                            balance to buy boards. The balance can never be negative.
                        </li>
                    </ul>
                </div>
            </div>
        </section>
    );
}

function SupportSplitSection() {
    return (
        <section className="bg-[#e5f0ff] py-16">
            <div className="mx-auto max-w-5xl px-4">
                <h2 className="text-center text-2xl md:text-3xl font-bold text-slate-900">
                    Your participation makes a difference
                </h2>

                <div className="mt-10 grid gap-6 md:grid-cols-2">
                    {/* 70% – prize pool */}
                    <div className="rounded-2xl bg-white p-8 shadow-sm text-center">
                        <p className="text-4xl font-extrabold text-emerald-500">70%</p>
                        <p className="mt-1 text-sm font-semibold text-slate-900">
                            Prize pool
                        </p>
                        <p className="mt-2 text-xs text-slate-600">
                            Goes back to the weekly winners of the Dead Pigeons game.
                        </p>
                    </div>

                    {/* 30% – club support */}
                    <div className="rounded-2xl bg-white p-8 shadow-sm text-center">
                        <p className="text-4xl font-extrabold text-sky-600">30%</p>
                        <p className="mt-1 text-sm font-semibold text-slate-900">
                            Club support
                        </p>
                        <p className="mt-2 text-xs text-slate-600">
                            Helps Jerne IF pay for facilities, equipment and youth
                            activities.
                        </p>
                    </div>
                </div>
            </div>
        </section>
    );
}

export default function HomePage() {
    return (
        <>
            <HeroSection />
            <HowItWorksSection />
            <GameDetailsSection />
            <SupportSplitSection />
        </>
    );
}
