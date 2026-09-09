import { chromium } from '@playwright/test';
import assert from 'node:assert/strict';
import {join} from 'node:path';
import {tmpdir} from 'node:os';
const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 for(const width of [1440,390]) {
  const page=await browser.newPage({viewport:{width,height:900}});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:5174/?demo=1&turn=2&rivalHand=2&discard=20');
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.fps);
  const active=page.locator('[data-testid=opponent].active');
  assert.equal(await active.count(),1);
  assert.match(await active.innerText(),/2 cartas[\s\S]*JUGANDO/);
  assert.equal(await page.locator('.discard-count').innerText(),'20 cartas jugadas');
  const label=await active.boundingBox();
  const status=await active.locator('.seat-status').boundingBox();
  assert.ok(status.y>=label.y+label.height-1,'status extends below count without covering it');
  await page.getByLabel('Ajustes gráficos').click();
  await page.locator('.audio-settings-embedded').waitFor();
  const music=page.getByLabel('Volumen de música');
  const effects=page.getByLabel('Volumen de efectos');
  const before=[await music.inputValue(),await effects.inputValue()];
  await page.getByRole('button',{name:'SILENCIAR TODO'}).click();
  assert.deepEqual([await music.inputValue(),await effects.inputValue()],['0','0']);
  await page.getByRole('button',{name:'REACTIVAR AUDIO'}).click();
  assert.deepEqual([await music.inputValue(),await effects.inputValue()],before);
  await page.keyboard.press('Escape');
  await page.getByRole('button',{name:'Ajustes gráficos'}).click();
  const baseline=await page.locator('canvas').first().evaluate(e=>Number(e.dataset.geometries));
  await page.getByLabel('Calidad de imagen').selectOption('low');
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.quality==='low');
  assert.ok(await page.locator('canvas').first().evaluate(e=>e.width/e.clientWidth)<1,'saving mode reduces render resolution');
  assert.equal(await page.getByText('Mostrar FPS en este panel').count(),0,'FPS control is not shown');
  await page.getByRole('button',{name:'VOLVER A LA MESA'}).click();
  await page.reload();
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.quality==='low');
  await page.getByRole('button',{name:'Ajustes gráficos'}).click();
  assert.equal(await page.getByLabel('Calidad de imagen').inputValue(),'low');
  await page.getByLabel('Calidad de imagen').selectOption('high');
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.quality==='high');
  await page.getByLabel('Calidad de imagen').selectOption('auto');
  for(const quality of ['low','high','auto','low','auto']) {
   await page.getByLabel('Calidad de imagen').selectOption(quality);
   await page.waitForFunction(q=>document.querySelector('canvas')?.dataset.quality===q,quality);
  }
  await page.getByRole('button',{name:'VOLVER A LA MESA'}).click();
  if(await page.evaluate(()=>document.fullscreenEnabled)) {
   await page.getByRole('button',{name:'Pantalla completa',exact:true}).click();
   await page.waitForFunction(()=>!!document.fullscreenElement);
   await page.getByRole('button',{name:'Salir de pantalla completa',exact:true}).click();
   await page.waitForFunction(()=>!document.fullscreenElement);
  }
  await page.keyboard.press('Escape');
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.quality==='auto');
  const sample=await page.locator('canvas').first().evaluate(e=>Number(e.dataset.sampledAt ?? 0));
  await page.waitForFunction(s=>Number(document.querySelector('canvas')?.dataset.sampledAt)>s,sample);
  assert.ok(await page.locator('canvas').first().evaluate(e=>Number(e.dataset.fps))<=62,'normal quality caps rendering at 60 FPS');
  assert.ok(await page.locator('canvas').first().evaluate(e=>Number(e.dataset.geometries))<=baseline+1,'quality toggles keep geometry count bounded');
  await page.screenshot({path:join(tmpdir(),`pepino-polish-${width}.png`)});
  await page.goto('http://127.0.0.1:5174/?demo=1&turn=2&rivalHand=1&paused=1');
  await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.fps);
  assert.equal(await page.locator('[data-testid=opponent].active').count(),0);
  assert.match(await page.locator('[data-testid=opponent]').first().innerText(),/1 carta[\s\S]*En pausa/);
  assert.equal(await page.getByRole('button',{name:'JUGAR',exact:true}).isDisabled(),true);
  await page.goto('http://127.0.0.1:5174/?demo=1&demoRoom=ABCDEFGHIJKLMNOPQRSTUVWXYZ123456');
  await page.getByRole('button',{name:'SALIR',exact:true}).waitFor();
  const exit=await page.getByRole('button',{name:'SALIR',exact:true}).boundingBox();
  assert.ok(exit.x+exit.width<=width,'long room code cannot push exit off screen');
  const logo=await page.locator('.wordmark').boundingBox();
  const tag=await page.locator('.room-tag').boundingBox();
  assert.ok(tag.x>=logo.x+logo.width,'long room code does not overlap logo');
  assert.deepEqual(errors,[]);
  await page.close();console.log(`PASS ${width}: active count, low cards, paused state, mute/restore, long room header, JS errors`);
 }
} finally {await browser.close();}
