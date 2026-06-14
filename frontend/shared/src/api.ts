import { getToken } from './auth';

// The backend API base. (In a real deployment this comes from build-time config.)
// Points at the microservices API gateway (YARP). Monolith was http://localhost:5287/api/v1.
export const API_BASE = 'http://localhost:5080/api';

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

function authHeaders(extra?: Record<string, string>): Record<string, string> {
  const headers: Record<string, string> = { ...extra };
  const token = getToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

async function parse<T>(res: Response): Promise<T> {
  const text = await res.text();
  const body = text ? JSON.parse(text) : null;
  if (!res.ok) {
    const message =
      body?.title ||
      (Array.isArray(body?.errors) ? body.errors.join('; ') : null) ||
      `Request failed (${res.status})`;
    throw new ApiError(res.status, message);
  }
  return body as T;
}

export const api = {
  async get<T>(path: string): Promise<T> {
    const res = await fetch(`${API_BASE}${path}`, { headers: authHeaders() });
    return parse<T>(res);
  },

  // Fetches a paged endpoint ({ items, page, pageSize, totalCount }) and returns just the items.
  async getItems<T>(path: string): Promise<T[]> {
    const res = await fetch(`${API_BASE}${path}`, { headers: authHeaders() });
    const body = await parse<{ items: T[] }>(res);
    return body?.items ?? [];
  },

  async post<T>(path: string, body?: unknown): Promise<T> {
    const res = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers: authHeaders({ 'Content-Type': 'application/json' }),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    return parse<T>(res);
  },

  async del<T>(path: string): Promise<T> {
    const res = await fetch(`${API_BASE}${path}`, { method: 'DELETE', headers: authHeaders() });
    return parse<T>(res);
  },

  async postForm<T>(path: string, form: FormData): Promise<T> {
    // Note: do NOT set Content-Type; the browser sets the multipart boundary.
    const res = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers: authHeaders(),
      body: form,
    });
    return parse<T>(res);
  },

  async getBlob(path: string): Promise<Blob> {
    const res = await fetch(`${API_BASE}${path}`, { headers: authHeaders() });
    if (!res.ok) {
      throw new ApiError(res.status, `Download failed (${res.status})`);
    }
    return res.blob();
  },
};

export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}
