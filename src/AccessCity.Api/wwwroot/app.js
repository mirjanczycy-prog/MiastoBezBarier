import {$,esc,date,api,session,badge,categoryLabels} from './common.js';
let places = [], selectedId, current, definitions=[], map, markers, requestVersion=0, listVersion=0, tiles;
const prefsForm = $('preferences'), dialog = $('report-dialog');
let applied = new URLSearchParams();
function notify(message, error=false) { $('notice').textContent=message; $('notice').classList.toggle('error',error); }
function getPreferences() {
  const params = new URLSearchParams();
  for (const input of prefsForm.elements) {
    if (!input.name || input.name==='data') continue;
    if (input.type==='checkbox') { if (input.checked) params.set(input.name,'true'); }
    else if (input.value) params.set(input.name,input.value);
  }
  if (prefsForm.elements.data.value) params.set('demo',prefsForm.elements.data.value);
  return params;
}
function storePreferences() {
  const saved={}; for (const i of prefsForm.elements) if(i.name) saved[i.name]=i.type==='checkbox'?i.checked:i.value;
  try { localStorage.setItem('accesscity.preferences',JSON.stringify(saved)); } catch { /* storage optional */ }
}
function restorePreferences() {
  try { const saved=JSON.parse(localStorage.getItem('accesscity.preferences')||'{}');
    for (const [key,value] of Object.entries(saved)) { const input=prefsForm.elements.namedItem(key); if(input) input.type==='checkbox'?input.checked=value:input.value=value; }
  } catch { /* no stored preferences */ }
  applied=getPreferences();
}
function icon(category){ return {hotel:'H',cafe:'K',restaurant:'R',museum:'M',gallery:'G',library:'B'}[category]||'•'; }
function renderPlaces() {
  $('count').textContent=String(places.length);
  $('place-list').innerHTML=places.length?places.map(({place:p,match,knownFeatures,conflicts,staleFeatures})=>`
    <button type="button" class="placecard ${p.id===selectedId?'selected':''}" data-place="${esc(p.id)}" aria-pressed="${p.id===selectedId}">
    <div class="cardhead"><span class="placeicon" aria-hidden="true">${icon(p.category)}</span><div><h3>${esc(p.name)}</h3><p>${esc(categoryLabels[p.category]||p.category)} · ${esc(p.city)}</p></div></div>
    <p class="address">${esc(p.address)}</p><div class="badges">${p.isDemo?'<span class="badge demo">DEMO · fikcyjne miejsce</span>':'<span class="badge source">OpenStreetMap</span>'}
    ${conflicts?'<span class="badge warning">Konflikt źródeł</span>':''}${staleFeatures?'<span class="badge warning">Starsze dane</span>':''}</div>
    <div class="cardfoot"><span>${match.requirements.length?`${match.met} wskazań zgodności · ${match.notMet} barier · ${match.uncertain} niewiadomych`:`${knownFeatures}/10 cech z informacją`}</span><span aria-hidden="true">↗</span></div></button>`).join(''):'<p class="empty">Nie znaleziono miejsc. Zmień filtry lub pobierz dane z OSM.</p>';
  $('place-list').querySelectorAll('[data-place]').forEach(button=>button.addEventListener('click',()=>selectPlace(button.dataset.place,true)));
}
function renderMap() {
  if (!map) return;
  markers.clearLayers(); const bounds=[];
  for (const {place:p} of places) {
    const marker=L.marker([p.latitude,p.longitude],{title:p.name,icon:L.divIcon({className:`map-pin ${p.isDemo?'demo':''}`,html:icon(p.category),iconSize:[30,30]})}).addTo(markers);
    marker.on('click',()=>selectPlace(p.id,true)); bounds.push([p.latitude,p.longitude]);
  }
  if(bounds.length) map.fitBounds(bounds,{padding:[30,30],maxZoom:15});
}
async function loadPlaces() {
  const version=++listVersion;
  const params=new URLSearchParams(applied); if($('query').value.trim()) params.set('q',$('query').value.trim());
  try {
    const data=await api(`/api/search?${params}`); if(version!==listVersion) return;
    places=data; if(!places.some(x=>x.place.id===selectedId)) selectedId=places.find(x=>x.place.id==='demo-hotel')?.place.id||places[0]?.place.id;
    renderPlaces(); renderMap();
    if(selectedId) await selectPlace(selectedId,false);
    else {++requestVersion; $('detail').innerHTML='<p class="empty">Wybierz inne kryteria, aby zobaczyć miejsce.</p>';}
  } catch(error) {notify(error.message,true);}
}
function renderEvidence(feature) {
  return `<p class="muted">${esc(feature.explanation)}</p>`+feature.evidence.map(e=>`<div class="evidence"><strong>${esc(e.source)}</strong>
    <p>Wartość: ${esc(e.displayValue)}</p><p>${e.kind==='community'?'Zgłoszono':'Edycja źródła'}: ${date(e.updatedAt)}</p>
    <p>Pozyskano: ${date(e.retrievedAt)}</p>${e.expiresAt?`<p>Ważne do: ${date(e.expiresAt)}</p><p>Potwierdzenia sesji: ${e.confirmations}</p>`:''}
    ${badge(e.status)} ${e.url?`<p><a href="${esc(e.url)}" target="_blank" rel="noopener">Zobacz rekord źródłowy ↗</a></p>`:''}</div>`).join('');
}
function renderDetail(data,reports) {
  const p=data.place; const requirements=data.match.requirements;
  $('detail').innerHTML=`<div class="detailtop"><div><div class="badges">${p.isDemo?'<span class="badge demo">DEMO · dane fikcyjne</span>':'<span class="badge source">Rzeczywiste miejsce · OSM</span>'}<span class="badge">${esc(categoryLabels[p.category]||p.category)}</span></div>
    <h2 id="place-title" tabindex="-1">${esc(p.name)}</h2><p class="address">${esc(p.address)} · ${esc(p.city)}</p></div><button id="add-report" type="button" class="primary">+ Dodaj obserwację</button></div>
    ${requirements.length?`<div class="matchbox"><h3>Wskazania względem Twoich potrzeb</h3><p>${data.match.met} zgodności w danych · ${data.match.notMet} wskazanych barier · ${data.match.uncertain} niewiadomych</p>
    <ul class="matchrows">${requirements.map(r=>`<li>${esc(r.label)} — ${r.status==='met'?'dane wskazują zgodność':r.status==='not-met'?'wskazana bariera':'wymaga sprawdzenia'} (${esc(r.detail)})</li>`).join('')}</ul><p>To porównanie informacji z preferencjami, bez gwarancji stanu miejsca.</p></div>`:''}
    <div class="detailintro">${esc(data.disclaimer)}</div><h3>Bariery i udogodnienia</h3>
    <div class="featuregrid">${data.features.map(f=>`<article class="feature ${f.hasConflict?'conflict':''}"><h3>${esc(f.name)}</h3><p class="featurevalue">${esc(f.displayValue)}</p>${badge(f.status)}
    <details ${f.hasConflict?'open':''}><summary>Źródła i aktualność (${f.evidence.length})</summary>${renderEvidence(f)}</details><button class="textbtn" type="button" data-report="${esc(f.key)}">Zgłoś lub popraw informację →</button></article>`).join('')}</div>
    <section class="reports"><div class="sectiontitle"><h3>Obserwacje społeczności (${reports.length})</h3><button id="refresh-detail" class="textbtn" type="button">Odśwież</button></div><p class="muted small">Dwie niezależne sesje poza autorem dają status społecznościowy. Nie jest to weryfikacja osób.</p>
    ${reports.length?reports.map(r=>`<article class="reportitem"><div class="sectiontitle"><strong>${esc(r.featureName)}: ${esc(r.displayValue)}</strong>${badge(r.status)}</div><p>${esc(r.comment)}</p><p class="muted small">Zgłoszono ${date(r.createdAt)} · ważne do ${date(r.expiresAt)} · ${r.confirmations} potwierdzeń sesji</p>
    ${r.isExpired?'<span class="muted small">Wygasło — nie wpływa na bieżący wynik</span>':r.isMine?'<span class="muted small">Twoje zgłoszenie</span>':r.hasConfirmed?'<span class="muted small">Ta sesja już potwierdziła</span>':`<button class="secondary" data-confirm="${esc(r.id)}" type="button">Potwierdzam tę obserwację</button>`}</article>`).join(''):'<p class="muted small">Brak zgłoszeń. Twoja obserwacja może pomóc kolejnym osobom.</p>'}</section>
    <div class="actions"><a class="secondary" href="/api/places/${encodeURIComponent(p.id)}/accessibility" target="_blank" rel="noopener">Dane JSON ↗</a><a class="secondary" href="/developers.html?place=${encodeURIComponent(p.id)}">Udostępnij w swojej aplikacji →</a></div>`;
  $('add-report').onclick=()=>openReport('elevatorOperational');
  $('refresh-detail').onclick=()=>selectPlace(selectedId,false);
  $('detail').querySelectorAll('[data-report]').forEach(b=>b.onclick=()=>openReport(b.dataset.report));
  $('detail').querySelectorAll('[data-confirm]').forEach(b=>b.onclick=async()=>{
    b.disabled=true;try{await api(`/api/reports/${b.dataset.confirm}/confirm`,{method:'POST'});notify('Potwierdzenie zapisane.');await loadPlaces();}catch(e){notify(e.message,true);b.disabled=false;}
  });
}
async function selectPlace(id, focus=false) {
  selectedId=id; const version=++requestVersion;renderPlaces();
  try { const [data,reports]=await Promise.all([api(`/api/places/${encodeURIComponent(id)}/accessibility?${applied}`),api(`/api/places/${encodeURIComponent(id)}/reports`)]);
    if(version!==requestVersion)return; current=data;renderDetail(data,reports);history.replaceState(null,'',`/?place=${encodeURIComponent(id)}`);
    if(focus)$('place-title').focus({preventScroll:false});
  }catch(e){if(version===requestVersion)notify(e.message,true);}
}
function reportValue() {
  const key=$('report-feature').value, definition=definitions.find(d=>d.key===key);
  let html;
  if(definition.kind==='boolean')html='<select id="report-value" required><option value="true">Tak</option><option value="false">Nie</option></select>';
  else if(definition.kind==='number')html='<input id="report-value" type="number" min="0" max="500" step="0.1" required placeholder="Wartość w centymetrach">';
  else if(definition.kind==='surface')html='<select id="report-value" required><option value="asphalt">Asfalt</option><option value="paving_stones">Płyty / kostka betonowa</option><option value="cobblestone">Bruk kamienny</option><option value="gravel">Żwir</option><option value="ground">Grunt</option><option value="concrete">Beton</option></select>';
  else html='<select id="report-value" required><option value="yes">Tak</option><option value="no">Nie</option><option value="limited">Ograniczona dostępność</option></select>';
  $('report-value-slot').innerHTML=html;
}
function openReport(key) {
  $('report-feature').value=key;reportValue();if(key==='elevatorOperational')$('report-value').value='false';
  $('report-comment').value='';$('report-error').textContent='';dialog.showModal();$('report-feature').focus();
}
$('report-feature').onchange=reportValue;
$('close-report').onclick=()=>dialog.close();
$('report-form').onsubmit=async e=>{
  e.preventDefault();const button=e.target.querySelector('button[type="submit"]');button.disabled=true;
  const id=selectedId;try{await api(`/api/places/${encodeURIComponent(id)}/reports`,{method:'POST',body:JSON.stringify({feature:$('report-feature').value,value:$('report-value').value,comment:$('report-comment').value})});
    dialog.close();notify('Obserwacja zapisana jako niezweryfikowana. Sprawdź jej źródła i ewentualny konflikt.');await loadPlaces();
  }catch(error){$('report-error').textContent=error.message;}finally{button.disabled=false;}
};
prefsForm.onsubmit=e=>{e.preventDefault();applied=getPreferences();storePreferences();loadPlaces();};
$('search').onsubmit=e=>{e.preventDefault();loadPlaces();};
$('reset').onclick=()=>{prefsForm.reset();applied=getPreferences();storePreferences();loadPlaces();};
$('import').onclick=async()=>{
  $('import').disabled=true;notify('Pobieranie publicznych danych OpenStreetMap…');
  try{const result=await api('/api/admin/import/osm',{method:'POST'});notify(`${result.places} rzeczywistych miejsc zaimportowano. ${result.message}`);await providerStatus();await loadPlaces();}
  catch(e){notify(e.message,true);}finally{$('import').disabled=false;}
};
async function providerStatus(){const s=await api('/api/providers/status');$('provider-status').textContent=s.lastSuccessfulImport?`OSM: pobrano ${date(s.lastSuccessfulImport)}`:'OSM: import jeszcze niewykonany';}
$('tiles').onclick=()=>{
  if(!map){notify('Mapa nie jest dostępna. Skorzystaj z listy miejsc.',true);return;}
  if(tiles){map.removeLayer(tiles);tiles=null;$('tiles').textContent='Włącz mapę OSM';$('map-label').textContent='Widok lokalizacji · bez podkładu';return;}
  tiles=L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19,attribution:'© OpenStreetMap contributors'}).addTo(map);
  tiles.on('tileerror',()=>{$('map-label').textContent='Podkład niedostępny · lista nadal działa';});
  $('tiles').textContent='Wyłącz podkład';$('map-label').textContent='Mapa OpenStreetMap · online';
};
try {
  restorePreferences();const s=await session();definitions=await api('/api/features');
  $('report-feature').innerHTML=definitions.map(d=>`<option value="${esc(d.key)}">${esc(d.name)}</option>`).join('');
  if(!s.canImport){$('import').hidden=true;$('provider-status').textContent='Import obsługuje administrator.';}
  if(window.L){map=L.map('map',{scrollWheelZoom:false}).setView([50.062,19.938],14);markers=L.layerGroup().addTo(map);}
  selectedId=new URLSearchParams(location.search).get('place');await providerStatus();await loadPlaces();
}catch(e){notify(`Nie udało się uruchomić widoku: ${e.message}`,true);}
