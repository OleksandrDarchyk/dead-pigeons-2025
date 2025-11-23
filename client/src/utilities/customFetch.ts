import type { ProblemDetails } from "@core/problemdetails.ts";
import toast from "react-hot-toast";

/**
 * HTTP client that:
 * - attaches JWT from storage to Authorization header
 * - shows toast if HTTP response is not ok
 */
export async function customFetch(
    url: RequestInfo,
    init?: RequestInit
): Promise<Response> {
    // Prefer sessionStorage (safer for exams),
    // but also try localStorage as a fallback
    const token =
        (typeof window !== "undefined" && sessionStorage.getItem("jwt")) ??
        (typeof window !== "undefined" && localStorage.getItem("jwt"));

    // Start with existing headers (if any)
    const headers = new Headers(init?.headers);

    // Attach Authorization header if token is present
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(url, {
        ...init,
        headers,
    });

    if (!response.ok) {
        try {
            const errorClone = response.clone();
            const problemDetails =
                (await errorClone.json()) as ProblemDetails;

            if (problemDetails?.title) {
                toast(problemDetails.title);
            } else {
                toast("Unexpected error");
            }
        } catch {
            toast("Unexpected error");
        }
    }

    return response;
}
