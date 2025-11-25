// src/router.tsx
import {
    createBrowserRouter,
    createRoutesFromElements,
    Route,
} from "react-router-dom";

import HomeLayout from "./components/layout/HomeLayout";
import HomePage from "./pages/home/HomePage";
import LoginPage from "./pages/auth/LoginPage";
import AdminDashboardPage from "./pages/admin/AdminDashboardPage";
import RegisterPage from "./pages/auth/RegisterPage";

const router = createBrowserRouter(
    createRoutesFromElements(
        <Route path="/" element={<HomeLayout />}>
            <Route index element={<HomePage />} />

            {/* /login → login page */}
            <Route path="login" element={<LoginPage />} />

            {/* /register → registration page */}
            <Route path="register" element={<RegisterPage />} />

            {/* /admin → admin dashboard */}
            <Route path="admin" element={<AdminDashboardPage />} />

            {/* /player → temporary player area */}
            <Route
                path="player"
                element={
                    <div className="p-4 text-white">
                        Player area – TODO (here will be boards, balance, etc.)
                    </div>
                }
            />
        </Route>
    )
);

export default router;
