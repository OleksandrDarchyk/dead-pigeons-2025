// src/utilities/customFetch.ts
import type { ProblemDetails } from "@core/problemdetails.ts";
import toast from "react-hot-toast";

/**
 * HTTP client that:
 * - reads JWT from sessionStorage ("jwt")
 * - removes quotes around token if they exist
 * - adds Authorization: Bearer <token>
 * - shows toast if response is not ok and has ProblemDetails
 */
export async function customFetch(
    url: RequestInfo,
    init?: RequestInit,
): Promise<Response> {
    let token: string | null = null;

    if (typeof window !== "undefined") {
        let raw = sessionStorage.getItem("jwt");

        if (raw) {
            raw = raw.trim();

            if (raw.startsWith('"') && raw.endsWith('"')) {
                raw = raw.slice(1, -1);
            }

            token = raw;
        }
    }

    // 👉 Insert logs HERE
    console.log("customFetch → TOKEN:", token);
    console.log("customFetch → URL:", url);

    const headers = new Headers(init?.headers ?? {});

    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(url, {
        ...init,
        headers,
    });

    if (!response.ok) {
        try {
            const clone = response.clone();
            const problem = (await clone.json()) as ProblemDetails;

            if (problem?.title) {
                toast(problem.title);
            } else {
                toast("Unexpected error");
            }
        } catch {
            toast("Unexpected error");
        }
    }

    return response;
}
