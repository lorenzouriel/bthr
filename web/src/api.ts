export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
  }
}

export async function api<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers)

  if (options.body) headers.set("Content-Type", "application/json")

  let response: Response

  try {
    response = await fetch(
      `${(import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "")}${path}`,
      { ...options, headers, credentials: "include" },
    )
  } catch (error) {
    if ((error as Error).name === "AbortError") throw error
    throw new ApiError(
      "Cannot reach the API. Check the connection and try again.",
      0,
    )
  }

  const text = await response.text()

  let body
  try {
    body = text ? JSON.parse(text) : undefined
  } catch {
    body = undefined
  }

  if (!response.ok) {
    if (
      response.status === 401 &&
      ![
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/change-password",
      ].includes(path)
    )
      window.dispatchEvent(new Event("session-expired"))

    const errors = body?.errors
      ? Object.values(body.errors).flat().join(" ")
      : ""

    throw new ApiError(
      errors ||
        body?.message ||
        (response.status >= 500
          ? "The API could not complete this request. Please try again."
          : body?.title) ||
        (response.status === 403
          ? "Your account does not have access to this feature."
          : `Request failed (${response.status}).`),
      response.status,
    )
  }

  return body as T
}
