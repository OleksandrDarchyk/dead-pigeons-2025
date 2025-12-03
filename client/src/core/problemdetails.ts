// src/core/problemdetails.ts
export interface ProblemDetails {
    title?: string;
    detail?: string;
    // Optional field for ASP.NET ValidationProblemDetails:
    // errors["Email"] = ["The Email field is not a valid e-mail address."]
    errors?: Record<string, string[]>;
}
