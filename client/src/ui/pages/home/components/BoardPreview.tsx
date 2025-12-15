// src/pages/home/components/BoardPreview.tsx

export default function BoardPreview() {
    return (
        <div className="relative w-full max-w-xl mx-auto">
            {/* BACKGROUND (stadium / lights effect) */}
            <div
                className="absolute inset-0 rounded-3xl
                bg-gradient-to-br from-slate-900 via-indigo-900 to-slate-900
                opacity-60 blur-sm"
            />

            {/* BOARD CONTAINER */}
            <div className="relative rounded-3xl bg-white shadow-2xl overflow-hidden">
                {/* TOP TITLE BAR */}
                <div className="bg-slate-900 text-slate-50 text-center py-3 font-bold tracking-wide">
                    DEAD PIGEONS LOTTERY
                </div>

                {/* NUMBERS GRID */}
                <div className="grid grid-cols-4 gap-3 p-6 text-center text-slate-800 font-semibold">
                    {Array.from({ length: 16 }, (_, i) => (
                        <div
                            key={i}
                            className="rounded-lg bg-slate-50 py-2 shadow-sm border border-slate-200 hover:bg-slate-100 transition"
                        >
                            {i + 1}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
