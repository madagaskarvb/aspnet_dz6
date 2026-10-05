// wwwroot/js/api-client.js
// Обёртка над fetch с авто-обновлением access-токена через refresh.

const TOKEN_KEY = 'securetodo_access';
const REFRESH_KEY = 'securetodo_refresh';

export function setTokens(access, refresh) {
    if (access) localStorage.setItem(TOKEN_KEY, access);
    if (refresh) localStorage.setItem(REFRESH_KEY, refresh);
}

export function clearTokens() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
}

export function getAccess() { return localStorage.getItem(TOKEN_KEY); }
export function getRefresh() { return localStorage.getItem(REFRESH_KEY); }

let refreshPromise = null; // защита от параллельных refresh-запросов

async function tryRefresh() {
    if (refreshPromise) return refreshPromise;

    refreshPromise = (async () => {
        const refresh = getRefresh();
        if (!refresh) return null;

        const resp = await fetch('/api/auth/refresh', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: refresh })
        });

        if (!resp.ok) {
            clearTokens();
            return null;
        }

        const data = await resp.json();
        setTokens(data.accessToken, data.refreshToken);
        return data.accessToken;
    })();

    try {
        return await refreshPromise;
    } finally {
        refreshPromise = null;
    }
}

/**
 * fetch с авторизацией.
 * При 401 — один раз пытается обновить access через refresh и повторяет запрос.
 */
export async function apiFetch(url, options = {}) {
    const opts = { ...options, headers: { ...(options.headers || {}) } };

    const access = getAccess();
    if (access) opts.headers['Authorization'] = 'Bearer ' + access;

    let resp = await fetch(url, opts);

    if (resp.status === 401) {
        const newAccess = await tryRefresh();
        if (!newAccess) {
            // refresh не работает — на логин
            window.location.href = '/Account/Login';
            return resp;
        }

        opts.headers['Authorization'] = 'Bearer ' + newAccess;
        resp = await fetch(url, opts);
    }

    return resp;
}