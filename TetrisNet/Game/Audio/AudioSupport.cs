using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleSynth;

namespace TetrisNet.Classes {
    public partial class Board {
        private AudioMixer am;

        private struct ReleaseNote {
            public readonly long Tick;
            public readonly string Note;

            public ReleaseNote(string note, int duration) {
                Tick = DateTime.Now.Ticks + duration * 10000;
                Note = note;
            }
        }

        private void InitAudioService() {
            am = new AudioMixerSlimDX();

            for(int i = 1; i <= 6; i++) { // note polyphony
                // Multiple oscillators, panning and automation (SignalMixer)
                am.BufferProviders.Add(CreateInstrument2());
            }
            am.Volume = 0.1;

            Thread release = new Thread(() => {
                while(true) {
                    Thread.Sleep(5);

                    lock(am) {
                        foreach(BufferProvider bp in am.BufferProviders) {
                            if(bp.Frequency != 0) {
                                ReleaseNote rn = (ReleaseNote)bp.Tag;
                                if(DateTime.Now.Ticks >= rn.Tick) {
                                    bp.Frequency = 0;
                                    break;
                                }
                            }
                        }
                    }
                }
            }) { IsBackground = true };
            release.Start();
        }

        private SignalGenerator CreateInstrument1() {
            SignalGenerator sg = new SignalGenerator();

            sg.Envelop.Attack = new Envelope.EnvelopePoint(1, 10);
            sg.Envelop.Decay = new Envelope.EnvelopePoint(0.6, 10);
            sg.Envelop.Sustain = new Envelope.EnvelopePoint(0.6, int.MaxValue);
            sg.Envelop.Release = new Envelope.EnvelopePoint(0, 100);

            sg.Volume = 0.5;
            sg.WaveForm = Oscillator.WaveForms.Sinusoidal;

            return sg;
        }

        private SignalMixer CreateInstrument2() {
            SignalGenerator sg;
            SignalMixer m = new SignalMixer();

            sg = new SignalGenerator() {
                WaveForm = Oscillator.WaveForms.Pulse,
                PulseWidth = 0.3,
                Volume = 0.35,
                Panning = 0.8
            };
            sg.Envelop.Sustain = new Envelope.EnvelopePoint(1, 500);
            sg.Envelop.Release = new Envelope.EnvelopePoint(0, 300);
            m.SignalGenerators.Add(sg);

            sg = new SignalGenerator() {
                Volume = 0.2,
                WaveForm = Oscillator.WaveForms.Sinusoidal
            };
            sg.Envelop.Attack = new Envelope.EnvelopePoint(1, 300);
            sg.Envelop.Release = new Envelope.EnvelopePoint(0, 400);

            Oscillator osc = new Oscillator() {
                WaveForm = Oscillator.WaveForms.Sinusoidal,
                Frequency = 4
            };
            sg.Automation.Set("Panning", osc);

            m.SignalGenerators.Add(sg);

            sg = new SignalGenerator() {
                Volume = 0.4,
                Panning = -0.8
            };
            sg.NoteShiftOffset -= 12;
            sg.WaveForm = Oscillator.WaveForms.SawTooth;
            sg.Envelop.Sustain = new Envelope.EnvelopePoint(1, 800);
            sg.Envelop.Release = new Envelope.EnvelopePoint(0, 600);
            m.SignalGenerators.Add(sg);

            sg = new SignalGenerator() {
                WaveForm = Oscillator.WaveForms.Noise,
                Volume = 0.04
            };
            sg.Envelop.Sustain = new Envelope.EnvelopePoint(1, 500);
            sg.Envelop.Release = new Envelope.EnvelopePoint(0, 400);
            m.SignalGenerators.Add(sg);

            return m;
        }

        private void PlayNote(string note, int duration) {
            lock(am) {
                foreach(BufferProvider bp in am.BufferProviders) {
                    if(bp.Frequency == 0) {
                        ReleaseNote rn = new ReleaseNote(note, duration);
                        bp.Note = note;
                        bp.Tag = rn;
                        break;
                    }
                }
            }
        }
    }
}
