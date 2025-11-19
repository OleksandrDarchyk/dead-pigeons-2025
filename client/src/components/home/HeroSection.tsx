// src/components/home/HeroSection.tsx

// Small internal component used on the right side of the hero
function BoardPreview() {
    return (
        <div className="relative w-full max-w-xl mx-auto">
            {/* Background (stadium / lights) */}
            <div
                className="absolute inset-0 rounded-3xl
                bg-gradient-to-br from-black via-red-900 to-black
                opacity-60 blur-sm"
            />

            {/* Board container */}
            <div className="relative rounded-3xl bg-white shadow-2xl overflow-hidden">
                {/* Top title bar */}
                <div className="bg-red-600 text-white text-center py-3 font-bold tracking-wide">
                    DEAD PIGEONS LOTTERY
                </div>

                {/* Numbers grid */}
                <div className="grid grid-cols-4 gap-3 p-6 text-center text-gray-800 font-semibold">
                    {Array.from({ length: 16 }, (_, i) => (
                        <div
                            key={i}
                            className="rounded-lg bg-red-50 py-2 shadow-sm border border-red-200 hover:bg-red-100 transition"
                        >
                            {i + 1}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

export default function HeroSection() {
    return (
        <section className="bg-[#DC2626] text-white min-h-screen flex items-center">
            <div className="mx-auto flex max-w-5xl flex-col gap-10 px-4 py-16 md:flex-row md:items-center">

                {/* LEFT */}
                <div className="md:w-1/2">
                    <h1 className="text-4xl font-extrabold md:text-5xl">
                        Dead Pigeons
                    </h1>

                    <p className="mt-4 text-lg">
                        Digital lottery game supporting Jerne IF sports club.
                    </p>

                    <p className="mt-2 text-sm text-red-100">
                        Pick your numbers, join weekly rounds, and help support local
                        sports while having a chance to win.
                    </p>

                    {/* TODO: Later connect this button to real navigation (for example /login). */}
                    <button className="mt-8 rounded-md bg-white px-6 py-3 text-sm font-semibold text-red-600 shadow-md hover:bg-red-50">
                        Get Started
                    </button>
                </div>

                {/* RIGHT */}
                <div className="md:w-1/2">
                    {/* Reusable board preview component */}
                    <BoardPreview />
                </div>

            </div>
        </section>
    );
}
