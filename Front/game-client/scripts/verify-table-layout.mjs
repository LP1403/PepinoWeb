import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 for(const width of [1440,390]) {
  const page=await browser.newPage({viewport:{width,height:900}});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const backs=[];page.on('response',r=>{if(r.url().includes('mazo'))backs.push(r.status());});
  await page.goto('http://127.0.0.1:5174/?demo=1&players=3&turn=1&played=12&discard=24');
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.fps);
  await page.screenshot({path:join(tmpdir(),`pepino-table-${width}.png`)});
  await page.getByRole('button',{name:'Acercar mesa',exact:true}).click();
  await page.getByRole('button',{name:'Acercar mesa',exact:true}).click();
  assert.equal(await page.getByRole('button',{name:'Restablecer zoom'}).innerText(),'130%');
  await page.getByRole('button',{name:'Restablecer zoom'}).click();
  assert.equal(await page.getByRole('button',{name:'Restablecer zoom'}).innerText(),'100%');
  assert.ok(backs.length && backs.every(s=>s===200));assert.deepEqual(errors,[]);
  console.log(`PASS ${width}: long play, discard back loads, zoom and reset, no JS errors`);
  await page.close();
 }
} finally {await browser.close();}
