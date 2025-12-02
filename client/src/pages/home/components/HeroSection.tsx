// src/pages/home/components/HeroSection.tsx
import { Link } from "react-router-dom";
import BoardPreview from "./BoardPreview.tsx";

export default function HeroSection() {
    return (
        <section className="bg-gradient-to-b from-[#e5f0ff] to-white text-slate-900 min-h-screen flex items-center">
            <div className="mx-auto flex max-w-5xl flex-col gap-10 px-4 py-16 md:flex-row md:items-center">
                {/* LEFT SIDE – text */}
                <div className="md:w-1/2">
                    <h1 className="text-4xl font-extrabold md:text-5xl text-slate-900">
                        Dead Pigeons
                    </h1>

                    <p className="mt-4 text-base md:text-lg text-slate-700">
                        Join the weekly lottery-style game and support Jerne IF sports club.
                    </p>

                    <p className="mt-2 text-sm text-slate-500">
                        Pick your numbers, join weekly rounds, and help support local
                        sports while having a chance to win.
                    </p>

                    <Link
                        to="/login"
                        className="mt-8 inline-block rounded-full bg-slate-900 px-7 py-3 text-sm font-semibold text-white shadow-lg hover:bg-slate-800"
                    >
                        Get Started
                    </Link>
                </div>

                {/* RIGHT SIDE – preview board */}
                <div className="md:w-1/2">
                    <BoardPreview />
                </div>
            </div>
        </section>
    );
}
