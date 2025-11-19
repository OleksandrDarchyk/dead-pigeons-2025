// src/router.tsx
import {
    createBrowserRouter,
    createRoutesFromElements,
    Route,
} from "react-router-dom";

import HomeLayout from "./components/layout/HomeLayout";
import HomePage from "./pages/HomePage";
import LoginPage from "./pages/LoginPage"; // 👈 додали

const router = createBrowserRouter(
    createRoutesFromElements(
        <Route path="/" element={<HomeLayout />}>
            <Route index element={<HomePage />} />

            {/* /login → наша форма логіну */}
            <Route path="login" element={<LoginPage />} />

            {/* TODO: Player and admin pages later */}
        </Route>
    )
);

export default router;
