export const $ = (id) => document.getElementById(id);
export const esc = (value) => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export const date = (value) => value ? new Date(value).toLocaleString('pl-PL', {dateStyle:'short',timeStyle:'short'}) : 'brak daty';
export const statusLabels = {'unknown':'Brak danych','source-unverified':'Dane OSM · bez oględzin','unverified':'Niezweryfikowane',
  'community-confirmed':'Potwierdzone w sesjach','conflict':'Konflikt źródeł','stale':'Dane nieaktualne / bez daty','demo':'Dane demonstracyjne','expired':'Wygasło'};
export const categoryLabels = {hotel:'Hotel',cafe:'Kawiarnia',restaurant:'Restauracja',museum:'Muzeum',gallery:'Galeria',library:'Biblioteka',cinema:'Kino',theatre:'Teatr',community_centre:'Centrum społeczne'};
export function badge(status) { return `<span class="badge ${['conflict','stale','unverified','unknown'].includes(status)?'warning':status==='demo'?'demo':status==='source-unverified'?'source':''}">${esc(statusLabels[status]||status)}</span>`; }
let csrfToken;
export async function session() { const data = await api('/api/session'); csrfToken = data.csrfToken; return data; }
export async function api(path, options = {}) {
  const headers = {'Accept':'application/json', ...(options.body?{'Content-Type':'application/json'}:{}),
    ...(options.method==='POST'?{'X-CSRF-TOKEN':csrfToken}:{}), ...options.headers};
  const response = await fetch(path, {...options, headers, credentials:'same-origin'});
  const data = await response.json().catch(()=>null);
  if (!response.ok) throw new Error(data?.detail || (response.status===429?'Zbyt wiele żądań. Poczekaj chwilę.':`Nie udało się wykonać operacji (${response.status}).`));
  return data;
}
