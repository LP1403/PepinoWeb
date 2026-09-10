import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
import {join} from 'node:path';
import {tmpdir} from 'node:os';

const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 for(const width of [390,1440]) {
  const page=await browser.newPage({viewport:{width,height:844}});
  const errors=[];
  page.on('pageerror',error=>errors.push(error.message));
  await page.goto('http://localhost:5174/');
  const logo=await page.locator('.wordmark').boundingBox();
  const actions=await page.locator('.lobby-top-actions').boundingBox();
  assert.ok(Math.abs(logo.x+logo.width/2-width/2)<2,'logo centered');
  assert.ok(width-actions.x-actions.width<=25,'controls at right edge');
  assert.ok(logo.y>=actions.y+actions.height || logo.x+logo.width<=actions.x,'logo does not intersect controls');
  await page.route('**/*.png',async route=>{
   await new Promise(resolve=>setTimeout(resolve,500));
   await route.continue();
  });
  await page.goto('http://localhost:5174/?demo=1&customCards=1');
  for(const id of ['custom-gold','custom-wildcard','custom-ace']) {
   const card=page.locator(`[data-card-id="${id}"]`);
   await card.focus();
   await page.keyboard.press('Space');
   await page.waitForTimeout(750);
   assert.equal(await card.getAttribute('aria-pressed'),'true');
   assert.ok(await card.locator('img').evaluate(img=>img.complete&&img.naturalWidth>0));
   await page.screenshot({path:join(tmpdir(),`pepino-custom-${id}-${width}.png`)});
   await page.keyboard.press('Space');
  }
  await page.goto('http://localhost:5174/?demo=1&players=8&turn=4');
  await page.getByLabel('Ver orden de turnos').click();
  assert.equal(await page.locator('.turn-order li').count(),8);
  assert.equal(await page.locator('.turn-order [aria-current]').count(),1);
  await page.keyboard.press('Escape');
  if(width===390) {
   await page.getByLabel('Ajustes gráficos').click();
   await page.getByRole('slider',{name:'Zoom de mesa'}).fill('130');
   assert.equal(await page.locator('.mobile-table-zoom output').textContent(),'130%');
   await page.getByRole('button',{name:'Restablecer',exact:true}).click();
   await page.keyboard.press('Escape');
  }
  assert.deepEqual(errors,[]);
  console.log(`PASS ${width}: header, custom card selections, turn order, zoom`);
  await page.close();
 }
} finally {await browser.close();}
