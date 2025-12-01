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
import PlayerDashboardPage from "./pages/player/PlayerDashboardPage";
import PlayerBalancePage from "./pages/player/PlayerBalancePage";
import PlayerHistoryPage from "./pages/player/PlayerHistoryPage";
import PlayerBuyBoardPage from "./pages/player/PlayerBuyBoardPage";

const router = createBrowserRouter(
    createRoutesFromElements(
        <Route path="/" element={<HomeLayout />}>
            <Route index element={<HomePage />} />

            <Route path="login" element={<LoginPage />} />
            <Route path="register" element={<RegisterPage />} />
            <Route path="admin" element={<AdminDashboardPage />} />

            {/* player area */}
            <Route path="player" element={<PlayerDashboardPage />} />
            <Route path="buy-board" element={<PlayerBuyBoardPage />} />
            <Route path="balance" element={<PlayerBalancePage />} />
            <Route path="history" element={<PlayerHistoryPage />} />
        </Route>
    )
);

export default router;
