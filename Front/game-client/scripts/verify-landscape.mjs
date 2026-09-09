import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
import {join} from 'node:path';
import {tmpdir} from 'node:os';

const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 for(const viewport of [{width:844,height:390},{width:1280,height:500}]) {
  for(const players of [4,8]) {
   const page=await browser.newPage({viewport});
   const errors=[];page.on('pageerror',e=>errors.push(e.message));
   await page.goto(`http://127.0.0.1:5174/?demo=1&players=${players}&hand=48`);
   await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.fps);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'match must fit viewport vertically');
   for(const badge of await page.locator('[data-testid=opponent]').all()) {
    assert.equal(await badge.locator('small').first().isVisible(),true,'opponent count stays visible');
    const box=await badge.boundingBox();
    assert.ok(box.y>=0 && box.y+box.height<=viewport.height,'complete opponent badge stays inside viewport');
    if(players>4)assert.ok(box.y>=44,'crowded landscape badges stay below the header');
   }
   await page.getByRole('button',{name:'Cartas siguientes'}).click();
   await page.waitForFunction(()=>document.querySelector('.hand-scroll').scrollLeft>0);
   await page.locator('.hand-scroll').evaluate(e=>{e.scrollLeft=0;});
   await page.locator('.hand-card[data-value="2"]').last().click();
   const selectors=['.play-controls','.hand-navigation','.hand-helper'];
   for(const selector of selectors) {
    const box=await page.locator(selector).boundingBox();
    assert.ok(box.x>=0 && box.y>=0 && box.x+box.width<=viewport.width+1 && box.y+box.height<=viewport.height+1,`${selector} stays on screen`);
   }
   const controls=await page.locator('.play-controls').boundingBox();
   const pile=await page.locator('.pile-caption').boundingBox();
   assert.ok(controls.x>=pile.x+pile.width,'actions do not cover the last play');
   await page.getByRole('button',{name:'JUGAR',exact:true}).click();
   await page.waitForFunction(()=>Number(document.querySelector('[data-revision]')?.dataset.revision)>1);
   await page.screenshot({path:join(tmpdir(),`pepino-landscape-${viewport.width}-${players}.png`)});
   assert.deepEqual(errors,[]);
   await page.close();console.log(`PASS ${viewport.width}x${viewport.height}, ${players} players: counts, navigation and direct play`);
  }
 }
} finally {await browser.close();}
