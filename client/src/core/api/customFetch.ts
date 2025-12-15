// src/utilities/customFetch.ts
import type { ProblemDetails } from "@core/api/generated/problemdetails.ts";
import toast from "react-hot-toast";

/**
 * HTTP client that:
 * - reads JWT from sessionStorage ("jwt")
 * - trims and removes quotes around token if they exist
 * - adds Authorization: Bearer <token>
 * - sets Accept: application/json if not provided
 * - shows toast if response is not ok and has ProblemDetails
 */
export async function customFetch(
    url: RequestInfo,
    init?: RequestInit,
): Promise<Response> {
    let token: string | null = null;

    if (typeof window !== "undefined") {
        let raw = window.sessionStorage.getItem("jwt");

        if (raw) {
            raw = raw.trim();

            if (raw.startsWith('"') && raw.endsWith('"')) {
                raw = raw.slice(1, -1);
            }

            token = raw;
        }
    }

    const headers = new Headers(init?.headers ?? {});

    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    if (!headers.has("Accept")) {
        headers.set("Accept", "application/json");
    }

    let response: Response;

    try {
        response = await fetch(url, {
            ...init,
            headers,
        });
    } catch (error) {
        toast.error("Network error. Please try again.");
        throw error;
    }

    if (!response.ok) {
        try {
            const clone = response.clone();
            const problem = (await clone.json()) as ProblemDetails | null;

            let message = "Unexpected error";

            if (problem) {
                // Try to read first validation error from "errors"
                const firstErrorFromErrors =
                    problem.errors &&
                    Object.values(problem.errors)
                        .flat()
                        .find(
                            (e) =>
                                typeof e === "string" &&
                                e.trim().length > 0,
                        );

                // Fallback chain: errors -> detail -> title -> default
                message =
                    firstErrorFromErrors ||
                    (problem.detail && problem.detail.trim()) ||
                    (problem.title && problem.title.trim()) ||
                    message;
            }

            toast.error(message);
        } catch {
            toast.error("Unexpected error");
        }
    }

    return response;
}
