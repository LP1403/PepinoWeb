import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 const page=await browser.newPage();const errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{
  Storage.prototype.getItem=function(){throw new DOMException('Storage blocked','SecurityError');};
  Storage.prototype.setItem=function(){throw new DOMException('Storage blocked','SecurityError');};
 });
 await page.goto('http://127.0.0.1:5174/');
 await page.getByLabel('TU NOMBRE').fill('QA storage');
 await page.getByLabel('CÓDIGO DE SALA').fill(`QA-STORAGE-${Date.now()}`);
 await page.getByRole('button',{name:/JUGAR/}).click();
 await page.locator('.lobby-players').getByText('QA storage (vos)',{exact:true}).waitFor();
 await page.getByRole('button',{name:'SALIR DE LA SALA'}).click();
 await page.getByLabel('TU NOMBRE').waitFor();
 assert.deepEqual(errors,[]);
 console.log('PASS blocked storage: entry, SignalR join, leave, no JS errors');
} finally {await browser.close();}
