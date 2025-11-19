// src/components/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Home from '@components/home/Home.tsx';

// TODO: Create these components later.
// import LoginPage from '@components/auth/LoginPage';
// import PlayerDashboard from '@components/player/PlayerDashboard';
// import AdminDashboard from '@components/admin/AdminDashboard';

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                {/* Root layout route */}
                <Route path="/" element={<Home />}>
                    {/* Index = main landing page for now */}
                    <Route
                        index
                        element={
                            <div>
                                {/* TODO: Replace this with a proper landing page (Dead Pigeons intro, logo, short text). */}
                                <p>Dead Pigeons – landing page placeholder.</p>
                            </div>
                        }
                    />

                    {/*
            TODO: Enable this route when LoginPage is implemented.
            <Route path="login" element={<LoginPage />} />
          */}

                    {/*
            TODO: Add player dashboard route when you implement it.
            <Route path="player" element={<PlayerDashboard />} />
          */}

                    {/*
            TODO: Add admin dashboard route when you implement it.
            <Route path="admin" element={<AdminDashboard />} />
          */}
                </Route>

                {/*
          TODO: Add a NotFound / 404 route later, e.g.:
          <Route path="*" element={<div>Page not found</div>} />
        */}
            </Routes>
        </BrowserRouter>
    );
}
