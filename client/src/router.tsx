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


const router = createBrowserRouter(
    createRoutesFromElements(
        <Route path="/" element={<HomeLayout />}>
            <Route index element={<HomePage />} />

            {/* /login →  */}
            <Route path="login" element={<LoginPage />} />

            {/* /admin →  */}
            <Route path="admin" element={<AdminDashboardPage />} />

            {/*TODO /player → тимчасова сторінка для гравця */}
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
