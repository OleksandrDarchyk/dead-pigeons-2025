const isProduction = import.meta.env.PROD;

// TODO: change this to your real Fly backend URL when you deploy
const prod = "https://deadpigeons-client-one.fly.dev";
const dev = "http://localhost:5284"; // keep this equal to your local .NET API port

export const baseUrl = isProduction ? prod : dev;
