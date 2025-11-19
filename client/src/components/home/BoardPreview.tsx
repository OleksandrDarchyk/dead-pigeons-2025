export default function BoardPreview() {
    return (
        <div className="relative w-full max-w-xl mx-auto">

            {/* BACKGROUND (stadium / lights) */}
            <div className="absolute inset-0 rounded-3xl
                bg-gradient-to-br from-black via-red-900 to-black
                opacity-60 blur-sm" />

            {/* BOARD CONTAINER */}
            <div className="relative rounded-3xl bg-white shadow-2xl overflow-hidden">

                {/* TOP TITLE BAR */}
                <div className="bg-red-600 text-white text-center py-3 font-bold tracking-wide">
                    DEAD PIGEONS LOTTERY
                </div>

                {/* NUMBERS GRID */}
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
