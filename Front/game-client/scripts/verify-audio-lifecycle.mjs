import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 const page=await browser.newPage();
 await page.addInitScript(()=>{
  const NativeAudioContext=window.AudioContext;
  window.qaAudioContexts=[];
  window.AudioContext=class extends NativeAudioContext {
   constructor(...args){super(...args);window.qaAudioContexts.push(this);}
  };
 });
 await page.goto('http://localhost:5174/?demo=1');
 await page.getByLabel('Ajustes gráficos').click();
 await page.locator('.audio-settings-embedded').waitFor();
 await page.getByLabel('Volumen de música').click();
 await page.waitForFunction(()=>window.qaAudioContexts.some(context=>context.state==='running'));
 // Emulate document visibility while keeping a real browser AudioContext.
 await page.evaluate(()=>{
  Object.defineProperty(document,'hidden',{configurable:true,get:()=>true});
  document.dispatchEvent(new Event('visibilitychange'));
 });
 await page.waitForFunction(()=>window.qaAudioContexts.every(context=>context.state==='suspended'));
 await page.evaluate(()=>{
  delete document.hidden;
  document.dispatchEvent(new Event('visibilitychange'));
 });
 await page.waitForFunction(()=>window.qaAudioContexts.some(context=>context.state==='running'));
 assert.equal(await page.evaluate(()=>window.qaAudioContexts.length),1,'visibility does not allocate new audio contexts');
 await page.getByLabel('Volumen de música').fill('37');
 await page.getByRole('button',{name:'VOLVER A LA MESA'}).click();
 assert.equal(await page.evaluate(()=>window.qaAudioContexts[0].state),'running','closing settings keeps music running');
 await page.getByLabel('Ajustes gráficos').click();
 assert.equal(await page.getByLabel('Volumen de música').inputValue(),'37');
 await page.mouse.click(2,2);
 assert.equal(await page.locator('dialog').count(),0,'outside click closes configuration');
 assert.equal(await page.evaluate(()=>window.qaAudioContexts[0].state),'running');
 console.log('PASS audio: real context suspends/resumes on emulated visibility without allocating another context.');
} finally {await browser.close();}
