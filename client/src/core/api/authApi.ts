// src/utilities/authApi.ts
import {
    AuthClient,
    type JwtResponse,
    type LoginRequestDto,
    type RegisterRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

// Shared AuthClient instance using our customFetch (JWT + ProblemDetails)
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

// Optional convenient type re-exports
export type { LoginRequestDto, RegisterRequestDto, JwtResponse };
