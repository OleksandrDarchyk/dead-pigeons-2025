// src/utilities/authApi.ts
import {
    AuthClient,
    type JwtResponse,
    type LoginRequestDto,
    type RegisterRequestDto,
} from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

// Shared AuthClient instance
const client = new AuthClient(baseUrl, { fetch: customFetch });

export const authApi = {
    login(dto: LoginRequestDto): Promise<JwtResponse> {
        return client.login(dto);
    },

    register(dto: RegisterRequestDto): Promise<JwtResponse> {
        return client.register(dto);
    },

    whoAmI() {
        return client.whoAmI();
    },
};
