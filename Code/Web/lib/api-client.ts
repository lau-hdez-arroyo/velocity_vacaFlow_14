// Minimal API client. All calls go through the Next.js /api proxy to the ASP.NET Core backend,
// carrying the auth cookie (same browser origin) and the anti-forgery token.

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly errors?: Record<string, string[]>;
  readonly detail?: string;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? 'Request failed');
    this.status = status;
    this.errors = problem.errors;
    this.detail = problem.detail;
  }
}

function getCookie(name: string): string | undefined {
  const row = document.cookie.split('; ').find((r) => r.startsWith(`${name}=`));
  // slice from the first '=' — token values (base64) may themselves contain '='.
  return row === undefined ? undefined : decodeURIComponent(row.slice(row.indexOf('=') + 1));
}

const XSRF_COOKIE = 'XSRF-TOKEN';
const XSRF_HEADER = 'X-XSRF-TOKEN';

async function ensureXsrfToken(): Promise<void> {
  if (getCookie(XSRF_COOKIE)) return;
  await fetch('/api/antiforgery/token', { credentials: 'include' });
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  await ensureXsrfToken();

  const response = await fetch(path, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      [XSRF_HEADER]: getCookie(XSRF_COOKIE) ?? '',
    },
    credentials: 'include',
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      /* non-JSON error body */
    }
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
