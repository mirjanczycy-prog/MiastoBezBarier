import {$,api,esc} from './common.js';
let version=0;
async function render(){const request=++version,id=$('partner-place').value;if(!id)return;
  const url=`/api/places/${encodeURIComponent(id)}/accessibility`;
  try{const data=await api(url);if(request!==version)return;$('api-response').textContent=JSON.stringify(data,null,2);$('api-link').href=url;
    const widgetUrl=new URL(`/widget.html?place=${encodeURIComponent(id)}`,location.origin);$('widget-preview').src=widgetUrl.href;
    $('embed-code').value=`<iframe src="${widgetUrl.href}" title="Informacje o dostępności miejsca" width="100%" height="560" style="border:0"></iframe>`;
    $('partner-error').textContent='';
  }catch(e){$('partner-error').textContent=e.message;}
}
$('refresh-api').onclick=render;$('partner-place').onchange=render;
$('copy-embed').onclick=async()=>{try{await navigator.clipboard.writeText($('embed-code').value);$('copy-status').textContent='Kod skopiowany.';}catch{$('embed-code').focus();$('embed-code').select();$('copy-status').textContent='Zaznaczony kod skopiuj skrótem Ctrl+C.';}};
try{const places=await api('/api/places');$('partner-place').innerHTML=places.map(p=>`<option value="${esc(p.id)}">${esc(p.name)}</option>`).join('');
  const desired=new URLSearchParams(location.search).get('place')||'demo-hotel';if(places.some(p=>p.id===desired))$('partner-place').value=desired;await render();
}catch(e){$('partner-error').textContent=e.message;}
