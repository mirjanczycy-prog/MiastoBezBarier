import {$,api,esc,badge,date} from './common.js';
const id=new URLSearchParams(location.search).get('place');
try{
  if(!id)throw new Error('Brak identyfikatora miejsca.');
  const d=await api(`/api/places/${encodeURIComponent(id)}/accessibility`);
  const keys=['stepFreeEntrance','elevator','elevatorOperational','accessibleToilet'];
  $('widget').innerHTML=`<p class="eyebrow">INFORMACJE O DOSTĘPNOŚCI</p><h1>${esc(d.place.name)}</h1>${d.place.isDemo?badge('demo'):''}
    ${d.features.filter(f=>keys.includes(f.key)).map(f=>`<div class="widgetline"><span>${esc(f.name)}</span><strong>${esc(f.displayValue)}</strong></div><div class="widgetsource">${badge(f.status)} · ${esc([...new Set(f.evidence.map(e=>e.source))].join(', ')||'Brak źródła')}</div>`).join('')}
    <small>Wynik: ${date(d.generatedAt)}. Brak danych nie oznacza braku barier. Statusy nie są formalnym zapewnieniem dostępności.</small>
    <a href="${location.origin}/?place=${encodeURIComponent(id)}" target="_blank" rel="noopener">Wszystkie źródła i szczegóły w MiastoBezBarier ↗</a>`;
}catch(e){$('widget').textContent=e.message;}
