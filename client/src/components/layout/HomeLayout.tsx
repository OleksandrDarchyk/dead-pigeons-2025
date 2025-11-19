// src/components/HomeLayout.tsx
import { Outlet } from 'react-router-dom';
import AppHeader from '@components/layout/AppHeader.tsx';

export default function HomeLayout() {
    return (
        <div className="min-h-screen bg-white text-slate-900">
            {/* Global header for all pages */}
            <AppHeader />

            {/* Route content renders here */}
            <main>
                <Outlet />
            </main>
        </div>
    );
}
