import { API_BASE } from "@/lib/api";

export const statusFetch = (
  path: string,
  init: RequestInit = {}
): Promise<Response> =>
  fetch(`${API_BASE.replace(/\/$/, "")}${path}`, {
    ...init,
    credentials: "omit",
  });
