import {HubConnectionBuilder,LogLevel} from '@microsoft/signalr';
import {chromium} from '@playwright/test';
import {randomUUID} from 'node:crypto';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import assert from 'node:assert/strict';
const room='QA-'+Date.now();
const browser=await chromium.launch({channel:'msedge',headless:true});
const bots=[];
const wait=async(f)=>{const until=Date.now()+30000;while(!f()){if(Date.now()>until)throw Error('State timeout');await new Promise(r=>setTimeout(r,20));}};
let ui;
try{
    const width=Number(process.env.QA_WIDTH)||1440;
    const page=await browser.newPage({viewport:{width,height:900}});
    await page.addInitScript(()=>{
        const NativeAudioContext=window.AudioContext;
        window.qaAudioContexts=[];
        window.AudioContext=class extends NativeAudioContext {
            constructor(...args){super(...args);window.qaAudioContexts.push(this);}
        };
    });
    const errors=[];page.on('pageerror',e=>errors.push(e.message));
    page.on('websocket',ws=>ws.on('framereceived',({payload})=>{
        for(const raw of payload.toString().split('\x1e')){if(!raw)continue;try{const m=JSON.parse(raw);if(m.target?.toLowerCase()==='gamestateupdated')ui=m.arguments[0];}catch{}}
    }));
    await page.goto('http://localhost:5174/?room='+room);
    await page.locator('#player-name').fill('QA visual');
    await page.locator('form button').click();
    await wait(()=>ui);
    for(let i=0;i<3;i++){
        const bot={token:randomUUID(),name:'QA bot '+i,state:null};
        bot.conn=new HubConnectionBuilder().withUrl('http://localhost:5264/gamehub').configureLogging(LogLevel.Error).build();
        bot.conn.on('GameStateUpdated',s=>{bot.state=s;});
        await bot.conn.start();await bot.conn.invoke('JoinSession',room,bot.name,bot.token);bots.push(bot);
    }
    await page.getByRole('button',{name:'1 MAZO',exact:true}).click();
    await page.getByRole('button',{name:'INICIAR PARTIDA',exact:true}).click();
    await wait(()=>ui.isGameStarted);
    const oldId=bots[0].state.yourPlayerId;
    await bots[0].conn.stop();await wait(()=>ui.isPaused);
    await bots[0].conn.start();await bots[0].conn.invoke('JoinSession',room,bots[0].name,bots[0].token);
    await wait(()=>!ui.isPaused && bots[0].state.yourPlayerId!==oldId);
    console.log('PASS reconnect: pause/resume, new connection, same seat');
    let moves=0;
    while(!ui.isGameFinished && moves<500){
        const turn=ui.players.find(p=>p.isCurrentTurn).connectionId;
        const bot=bots.find(b=>b.state?.yourPlayerId===turn);
        const state=bot?bot.state:ui;
        const rank=c=>c.value===1?13:c.value;
        const card=state.yourHand.filter(c=>c.value===2||!state.lastPlayedCards.length||rank(c)>=rank(state.lastPlayedCards[0])).sort((a,b)=>(a.value===2?99:rank(a))-(b.value===2?99:rank(b)))[0];
        const rev=ui.revision;
        if(bot){await bot.conn.invoke(card?'PlayCardIds':'PassTurn',room,...(card?[[card.id]]:[]));}
        else{
            await page.waitForFunction(r=>Number(document.querySelector('[data-revision]')?.dataset.revision)>=r,rev);
            if(card){
                await page.locator(`[data-card-id="${card.id}"]`).focus();await page.keyboard.press('Space');
                await page.getByRole('button',{name:'JUGAR',exact:true}).click();
            }else await page.getByRole('button',{name:'PASAR',exact:true}).click();
        }
        await wait(()=>ui.revision>rev);
        moves++;if(moves%20===0)console.log('Moves',moves);
    }
    assert.ok(ui.isGameFinished);
    await page.getByRole('heading',{name:'¡Partida terminada!'}).waitFor();
    assert.ok(await page.locator('dialog .confirm-cards img').count()>0);
    await page.screenshot({path:join(tmpdir(),`pepino-live-final-${width}.png`)});
    await page.getByRole('button',{name:'VOLVER AL LOBBY'}).click();
    await page.getByRole('button',{name:'VOLVER A JUGAR'}).click();
    await wait(()=>ui.isGameStarted);
    assert.equal(await page.getByRole('dialog').count(),0);
    assert.deepEqual(errors,[]);
    console.log(`PASS live game: ${moves} actions, final cards, winners, replay, no JS errors`);
    await page.getByRole('button',{name:'SALIR',exact:true}).click();
    await page.getByRole('dialog').getByRole('button',{name:'SALIR',exact:true}).click();
    await page.waitForFunction(()=>window.qaAudioContexts.some(context=>context.state==='running'));
    assert.equal(await page.evaluate(()=>window.qaAudioContexts.length),1,'one audio context across home, lobby, match, replay and exit');
    console.log('PASS audio continuity: same running context after leaving for home.');
}finally{
    for(const bot of bots){try{await bot.conn.invoke('LeaveRoom',room,bot.name);}catch{}await bot.conn.stop();}
    await browser.close();
}
