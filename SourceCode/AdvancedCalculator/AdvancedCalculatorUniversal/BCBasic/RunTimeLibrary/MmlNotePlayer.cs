﻿using BCBasic;
using MusicMacroLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Midi;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    class MmlNotePlayer : IMmlNotePlayer
    {
        public MmlNotePlayer (CancellationToken ct)
        {
            this.ct = ct;
        }
        public void SetCancellationToken (CancellationToken ct)
        {
            this.ct = ct;
        }
        CancellationToken ct;
        Queue<MmlNote> PlayingQueue = new Queue<MmlNote>();
        Task PlayTask = null;
        bool PlayTaskRunning = false;
        MidiSynthesizer MS = null;

        public void AddMmlString(string mmlString)
        {
            var notes = MusicMacroLanguage.MmlNote.Parse(mmlString, MusicMacroLanguage.MmlNote.MMLType.Modern);
            AddNotes(notes);
        }
        public void SetFunction(BCRunContext context, string functionName)
        {
            this.context = context;
            this.functionName = functionName;
        }

        public void AddNotes(IEnumerable<MmlNote> notes, MidiSynthesizer ms = null)
        {
            if (MS == null) MS = ms;
            foreach (var note in notes)
            {
                PlayingQueue.Enqueue(note);
            }
            lock (this)
            {
                if (!PlayTaskRunning && PlayingQueue.Count > 0)
                {
                    PlayTaskRunning = true;
                    PlayTask = DoPlaying(ct);
                }
            }
        }

        public bool IsPlaying ()
        {
            return PlayTaskRunning;
        }

        public string functionName = ""; 
        public BCRunContext context = null;
        int nexception = 0;
        private async Task DoPlaying(CancellationToken playCt)
        {
            int currInstrument = 0;
            var ms = MS;
            bool DidInstrument = false;
            Task rundown = null;
            while (PlayTaskRunning)
            {
                var note = PlayingQueue.Dequeue();
                try
                {
                    if (note.Pitch == 'I') // Instrument
                    {
                        currInstrument = note.Instrument;
                        DidInstrument = true;
                        await MmlNote.DoProgramChange(note.Instrument, ms);
                    }
                    else if (note.Pitch == 'R')
                    {
                        try
                        {
                            await Task.Delay(note.DurationInMilliseconds, playCt);
                        }
                        catch (Exception)
                        {
                            ; // Was asked to stop playing
                        }
                    }
                    else
                    {
                        int playDuration = (int)Math.Ceiling(note.DurationInMilliseconds * note.SilencePercent);
                        int silenceDuration = note.DurationInMilliseconds - playDuration;
                        if (functionName != "" && context != null)
                        {
                            var arglist = new List<IExpression>() {
                                new NumericConstant(note.MidiNote),
                                new NumericConstant(currInstrument),
                                new NumericConstant(note.DurationInMilliseconds),
                                new StringConstant(note.ToString())
                            };
                            context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        }
                        await MmlNote.NoteOnAsync(note, ms);
                        if (!playCt.IsCancellationRequested)
                        {
                            try
                            {
                                await Task.Delay(playDuration, playCt);
                            }
                            catch (Exception)
                            {
                                ; // asked to stop playing
                            }
                        }
                        await MmlNote.NoteOffAsync(note, ms);
                        if (silenceDuration > 0 && !playCt.IsCancellationRequested)
                        {
                            try
                            {
                                await Task.Delay(silenceDuration, playCt);
                            }
                            catch (Exception)
                            {
                                ; // asked to stop playing.
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    nexception++;
                }
                lock (this)
                {
                    if (PlayingQueue.Count == 0 || playCt.IsCancellationRequested)
                    {
                        if (DidInstrument)
                        {
                            rundown = MmlNote.DoProgramChange(0, ms); // Reset to Piano.
                        }
                        PlayTaskRunning = false;
                        PlayTask = null;

                        if (ct.IsCancellationRequested)
                        {
                            PlayingQueue.Clear();
                        }
                    }
                }
            } // End while (forever)
            if (rundown != null) await rundown;
        }
    }
}
