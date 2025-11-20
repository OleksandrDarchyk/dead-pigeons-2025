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

            {/* /login → наша форма логіну */}
            <Route path="login" element={<LoginPage />} />
            <Route path="admin" element={<AdminDashboardPage />} />

            {/* TODO: User  pages later */}
        </Route>
    )
);

export default router;
