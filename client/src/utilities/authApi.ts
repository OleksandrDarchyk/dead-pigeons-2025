// Auth API client: handles login/register/whoAmI using baseUrl + customFetch.
// Ready-made AuthClient for all authentication requests.
// Auth API wrapper (login, register, whoAmI).



import {AuthClient} from "@core/generated-client.ts";
import {baseUrl} from "@core/baseUrl.ts";
import {customFetch} from "@utilities/customFetch.ts";


export const authApi = new AuthClient(baseUrl, customFetch);
