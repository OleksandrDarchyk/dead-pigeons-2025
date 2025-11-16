import { createBrowserRouter, RouterProvider } from "react-router";
import Home from "@components/Home.tsx";
import { DevTools } from "jotai-devtools";
import "jotai-devtools/styles.css";
import { Toaster } from "react-hot-toast";
import Auth from "@components/routes/auth/Auth.tsx";

// Define all routes for the app
const router = createBrowserRouter([
    {
        path: "/",             // root route
        element: <Home />,     // layout component
        children: [
            {
                index: true,       // default child route
                element: <Auth />, // auth page as start screen
            },
            // later you can add more routes here (players, games, admin, etc.)
        ],
    },
]);

function App() {
    return (
        <>
            {/* Provides routing for the whole app */}
            <RouterProvider router={router} />

            {/* Jotai devtools for debugging atoms (optional but useful) */}
            <DevTools />

            {/* Global toast container for notifications */}
            <Toaster position="top-center" reverseOrder={false} />
        </>
    );
}

export default App;
