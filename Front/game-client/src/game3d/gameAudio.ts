export type SoundCue = 'select' | 'play' | 'pepino' | 'wildcard' | 'turn' | 'pass' | 'win';
export type AudioSettings = { music: number; effects: number };
const KEY = 'pepino-audio-v1';
export function loadAudioSettings(): AudioSettings {
    try {
        const saved = JSON.parse(localStorage.getItem(KEY) ?? '{}');
        const level = (v: unknown, fallback: number) => typeof v === 'number' && Number.isFinite(v) ? Math.max(0, Math.min(1, v)) : fallback;
        return { music: level(saved.music, .22), effects: level(saved.effects, .35) };
    } catch { return { music: .22, effects: .35 }; }
}

// Original downtempo ambience: warm pads, rounded bass and brushed percussion.
// Generated locally, with no downloads, external tracks or looping-file seams.
export class GameAudio {
    private context: AudioContext | null = null;
    private music: GainNode | null = null;
    private effects: GainNode | null = null;
    private timer: ReturnType<typeof setInterval> | undefined;
    private nextBar = 0;
    private bar = 0;
    private disposed = false;
    private nodes = new Set<AudioScheduledSourceNode>();
    private noise: AudioBuffer | null = null;
    private settings: AudioSettings;
    constructor(settings: AudioSettings) { this.settings = settings; }
    async unlock() {
        if (this.disposed || document.hidden) return;
        try {
            if (!this.context) {
                this.context = new AudioContext();
                this.music = this.context.createGain();
                this.effects = this.context.createGain();
                this.music.connect(this.context.destination);
                this.effects.connect(this.context.destination);
                this.update(this.settings);
                this.nextBar = this.context.currentTime + .1;
            }
            await this.context.resume();
            if (this.disposed) return;
            if (document.hidden) { this.pause(); return; }
            if (!this.timer) this.timer = setInterval(() => this.schedule(), 200);
            this.schedule();
        } catch { /* Unsupported or browser-blocked audio must never block gameplay. */ }
    }
    update(settings: AudioSettings) {
        this.settings = settings;
        try { localStorage.setItem(KEY, JSON.stringify(settings)); } catch { /* Storage may be disabled. */ }
        if (!this.context) return;
        this.music?.gain.setTargetAtTime(settings.music, this.context.currentTime, .12);
        this.effects?.gain.setTargetAtTime(settings.effects, this.context.currentTime, .03);
    }
    private tone(midi: number, at: number, duration: number, volume: number, bus: GainNode, attack = .015) {
        const ctx = this.context!;
        const oscillator = ctx.createOscillator(), envelope = ctx.createGain();
        oscillator.type = 'sine';
        oscillator.frequency.value = 440 * 2 ** ((midi - 69) / 12);
        envelope.gain.setValueAtTime(0, at);
        envelope.gain.linearRampToValueAtTime(volume, at + attack);
        envelope.gain.exponentialRampToValueAtTime(.0001, at + duration);
        oscillator.connect(envelope); envelope.connect(bus);
        this.nodes.add(oscillator);
        oscillator.onended = () => { oscillator.disconnect(); envelope.disconnect(); this.nodes.delete(oscillator); };
        oscillator.start(at); oscillator.stop(at + duration + .03);
    }
    private schedule() {
        const ctx = this.context;
        if (!ctx || ctx.state !== 'running' || !this.music || document.hidden) return;
        if (this.nextBar < ctx.currentTime) this.nextBar = ctx.currentTime + .1;
        if (this.nextBar > ctx.currentTime + .4) return;
        if (this.settings.music===0) { this.nextBar=ctx.currentTime+.5; return; }
        const beat = 60 / 82;
        const chords = [[57,60,64,67], [53,57,60,64], [48,55,59,62], [55,59,62,69]];
        const roots = [33,29,36,31];
        const bar = this.bar++, index = Math.floor(bar / 2) % 4;
        const at = this.nextBar;
        // Long attacks, no piano-like lead or bright arpeggio.
        if (bar % 2 === 0) chords[index].forEach((note,i) => this.tone(note,at+i*.025,beat*8.5,.024,this.music!,1.3));
        [0,1.75,2.5].forEach((step,i) => this.tone(roots[index]+(i===2?12:0),at+step*beat,beat*.9,.065,this.music!,.045));
        [0,2].forEach(step => this.kick(at+step*beat));
        [1,3].forEach(step => this.brush(at+step*beat,.12,.032,1500));
        for(let i=0;i<8;i++) this.brush(at+(i*.5+(i%2?.045:0))*beat,.055,i%2?.012:.008,4300);
        this.nextBar += beat*4;
    }
    private kick(at: number) {
        const ctx=this.context!, oscillator=ctx.createOscillator(), gain=ctx.createGain();
        oscillator.frequency.setValueAtTime(105,at); oscillator.frequency.exponentialRampToValueAtTime(48,at+.16);
        gain.gain.setValueAtTime(0,at); gain.gain.linearRampToValueAtTime(.09,at+.008); gain.gain.exponentialRampToValueAtTime(.0001,at+.24);
        oscillator.connect(gain); gain.connect(this.music!); this.nodes.add(oscillator);
        oscillator.onended=()=>{oscillator.disconnect();gain.disconnect();this.nodes.delete(oscillator);};
        oscillator.start(at); oscillator.stop(at+.26);
    }
    private brush(at: number, duration: number, volume: number, frequency: number) {
        const ctx=this.context!;
        if(!this.noise) {
            this.noise=ctx.createBuffer(1,Math.ceil(ctx.sampleRate*.25),ctx.sampleRate);
            const data=this.noise.getChannelData(0);
            let seed=193;
            for(let i=0;i<data.length;i++) { seed=(1664525*seed+1013904223)>>>0; data[i]=seed/2147483648-1; }
        }
        const source=ctx.createBufferSource(), filter=ctx.createBiquadFilter(), gain=ctx.createGain();
        source.buffer=this.noise; filter.type='bandpass';filter.frequency.value=frequency;filter.Q.value=.6;
        gain.gain.setValueAtTime(0,at);gain.gain.linearRampToValueAtTime(volume,at+.004);gain.gain.exponentialRampToValueAtTime(.0001,at+duration);
        source.connect(filter);filter.connect(gain);gain.connect(this.music!);this.nodes.add(source);
        source.onended=()=>{source.disconnect();filter.disconnect();gain.disconnect();this.nodes.delete(source);};
        source.start(at);source.stop(at+duration+.01);
    }
    cue(cue: SoundCue) {
        if (!this.context || this.context.state !== 'running' || !this.effects || document.hidden || this.settings.effects===0) return;
        const notes: Record<SoundCue, number[]> = { select: [76], play: [60, 67], pepino: [76, 72, 67], wildcard: [67, 72, 79], turn: [72, 76], pass: [64, 60], win:[60,64,67,72] };
        notes[cue].forEach((note, i) => this.tone(note, this.context!.currentTime + i * .085, cue === 'select' ? .07 : .28, cue === 'select' ? .035 : .07, this.effects!));
    }
    pause() {
        clearInterval(this.timer); this.timer=undefined;
        void this.context?.suspend().catch(() => {});
    }
    dispose() {
        this.disposed = true;
        clearInterval(this.timer);
        this.nodes.forEach(node => { try { node.stop(); } catch { /* Already ended. */ } });
        void this.context?.close().catch(() => {});
    }
}
