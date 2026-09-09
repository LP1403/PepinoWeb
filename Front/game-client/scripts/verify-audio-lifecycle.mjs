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
 console.log('PASS audio: real context suspends/resumes on emulated visibility without allocating another context.');
} finally {await browser.close();}
