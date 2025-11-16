import type {ProblemDetails} from "@core/problemdetails.ts";
import toast from "react-hot-toast";

/**
 * This fetch http client attaches JWT from localstorage
 * and toasts if http requests fail.
 */
export const customFetch = {
    fetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
        const token = localStorage.getItem('jwt');
        const headers = new Headers(init?.headers);

        // Only necessary change → add "Bearer"
        if (token) {
            headers.set('Authorization', `Bearer ${token}`);
        }

        return fetch(url, {
            ...init,
            headers
        }).then(async (response) => {

            if (!response.ok) {
                try {
                    const errorClone = response.clone();
                    const problemDetails = (await errorClone.json()) as ProblemDetails;

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
        });
    }
};
