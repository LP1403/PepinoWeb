export type SoundCue = 'select' | 'play' | 'pepino' | 'wildcard' | 'turn' | 'pass';
export type AudioSettings = { music: number; effects: number };
const KEY = 'pepino-audio-v1';
export function loadAudioSettings(): AudioSettings {
    try {
        const saved = JSON.parse(localStorage.getItem(KEY) ?? '{}');
        const level = (v: unknown, fallback: number) => typeof v === 'number' && Number.isFinite(v) ? Math.max(0, Math.min(1, v)) : fallback;
        return { music: level(saved.music, .22), effects: level(saved.effects, .35) };
    } catch { return { music: .22, effects: .35 }; }
}

// Original, quiet ambient score: slow extended chords and a sparse pentatonic melody.
// Generated locally, with no downloads, external tracks or looping-file seams.
export class GameAudio {
    private context: AudioContext | null = null;
    private music: GainNode | null = null;
    private effects: GainNode | null = null;
    private timer: ReturnType<typeof setInterval> | undefined;
    private nextBar = 0;
    private bar = 0;
    private disposed = false;
    private nodes = new Set<OscillatorNode>();
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
        const chords = [[48, 55, 59, 62], [45, 52, 55, 59], [41, 48, 52, 57], [43, 50, 57, 60]];
        const melody = [[72, 76, 79], [76, 74, 71], [69, 72, 76], [74, 71, 67]];
        const index = this.bar++ % 4;
        chords[index].forEach((note, i) => this.tone(note, this.nextBar + i * .09, 4.9, .027, this.music!, .65));
        melody[index].forEach((note, i) => this.tone(note, this.nextBar + .75 + i * 1.5, 1.9, .025, this.music!, .035));
        this.nextBar += 5; // 48 BPM, four beats per bar.
    }
    cue(cue: SoundCue) {
        if (!this.context || this.context.state !== 'running' || !this.effects || document.hidden) return;
        const notes: Record<SoundCue, number[]> = { select: [76], play: [60, 67], pepino: [76, 72, 67], wildcard: [67, 72, 79], turn: [72, 76], pass: [64, 60] };
        notes[cue].forEach((note, i) => this.tone(note, this.context!.currentTime + i * .085, cue === 'select' ? .07 : .28, cue === 'select' ? .035 : .07, this.effects!));
    }
    pause() { void this.context?.suspend().catch(() => {}); }
    dispose() {
        this.disposed = true;
        clearInterval(this.timer);
        this.nodes.forEach(node => { try { node.stop(); } catch { /* Already ended. */ } });
        void this.context?.close().catch(() => {});
    }
}
