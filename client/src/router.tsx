// src/router.tsx

import {
    createBrowserRouter,
    createRoutesFromElements,
    Route,
} from "react-router-dom";

// Correct paths
import Home from "./components/home/Home";
import LandingPage from "./components/home/LandingPage";

const router = createBrowserRouter(
    createRoutesFromElements(
        <Route path="/" element={<Home />}>
            <Route index element={<LandingPage />} />

            {/* TODO: Add login route later */}
            {/* <Route path="login" element={<LoginPage />} /> */}

            {/* TODO: Player and admin routes later */}
        </Route>
    )
);

export default router;
