import {HubConnectionBuilder,LogLevel} from '@microsoft/signalr';
import {chromium} from '@playwright/test';
import {randomUUID} from 'node:crypto';
import assert from 'node:assert/strict';

// Integration test: deliberately waits through the real 60-second server grace.
const room='QA-EXP-'+Date.now(),emptyRoom='QA-EMPTY-'+Date.now();
const create=()=>new HubConnectionBuilder().withUrl('http://localhost:5264/gamehub').configureLogging(LogLevel.Error).build();
const owner=create(),solo=create();
const browser=await chromium.launch({channel:'msedge',headless:true});
const wait=async(predicate,timeout=30000)=>{
 const until=Date.now()+timeout;
 while(!predicate()) {if(Date.now()>until)throw Error('State timeout');await new Promise(resolve=>setTimeout(resolve,50));}
};
let state;
const watchdog=setTimeout(()=>{console.error('Expiry test exceeded 150 seconds.',state);process.exit(1);},150000);
try {
 await owner.start();await owner.invoke('JoinSession',room,'QA owner',randomUUID());
 await solo.start();await solo.invoke('JoinSession',emptyRoom,'QA solo',randomUUID());
 const page=await browser.newPage({viewport:{width:390,height:844}});
 const errors=[];page.on('pageerror',error=>errors.push(error.message));
 page.on('websocket',socket=>socket.on('framereceived',({payload})=>{
  for(const raw of payload.toString().split('\x1e')) {
   if(!raw)continue;
   try {const message=JSON.parse(raw);if(message.target?.toLowerCase()==='gamestateupdated')state=message.arguments[0];}catch{}
  }
 }));
 await page.goto('http://localhost:5174/?room='+room);
 await page.locator('#player-name').fill('QA survivor');await page.locator('form button').click();
 await wait(()=>state?.players.length===2);
 await owner.invoke('SelectGameMode',room,1);await owner.invoke('StartGame',room);
 await wait(()=>state.isGameStarted);
 await solo.stop();await owner.stop();
 await wait(()=>state.isPaused);
 console.log('Waiting for the real 60-second disconnect grace; match is paused.');
 await wait(()=>!state.isGameStarted && state.players.length===1,75000);
 console.log('Expired owner removed; validating reset and lobby.');
 assert.equal(state.isRoomCreator,true);
 assert.equal(state.isPaused,false);
 assert.deepEqual(state.yourHand,[]);
 assert.deepEqual(state.tableCards,[]);
 assert.match(state.notice,/no volvió a conectarse/);
 await page.getByRole('heading',{name:'Tu mesa, tus amigos.'}).waitFor();
 await page.getByRole('button',{name:'3 MAZOS',exact:true}).click();
 await wait(()=>state.gameMode?.deckCount===3);
 console.log('Surviving owner can change decks; checking empty room.');
 // An independent connection can distinguish a deleted room from a retained
 // room it does not belong to, without creating that room again.
 await solo.start();
 console.log('Empty room retained during grace; waiting for its expiry.');
 await solo.stop();
 await new Promise(resolve=>setTimeout(resolve,62000));
 const deleted=await fetch(`http://localhost:5264/api/rooms/${emptyRoom}`);
 assert.equal(deleted.status,404,'empty room is removed after its grace period');
 assert.deepEqual(errors,[]);
 console.log('PASS expiry: cancelled match, empty hands, surviving player becomes owner, mobile lobby usable, empty room deleted.');
 await page.getByRole('button',{name:'SALIR DE LA SALA',exact:true}).click();
} catch(error) {
 console.error('Expiry test failed:',error);
 throw error;
} finally {
 // Disconnected clients need no LeaveRoom request. A fresh probe is not a
 // member of the deleted room; only stop its transport.
 await owner.stop();await solo.stop();await browser.close();clearTimeout(watchdog);
}
