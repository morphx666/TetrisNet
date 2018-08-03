using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TetrisNet.Classes {
    public partial class Board {
        // https://www.piano-keyboard-guide.com/how-to-play-the-tetris-theme-song-easy-piano-tutorial-korobeiniki/

        string rightHand = "E 555.B 423.C 523.D 555.C 523.B 423.A 425." +
                           "A 423.C 523.E 555.D 523.C 523.B 458." +
                           "C 523.D 555.E 555.C 535.A 425.A 425." +
                           "@ @08." +
                           "D 555.F 523.A 555.G 523.F 523.E 547." +
                           "C 523.E 555.D 523.C 523.B 425.B 423." +
                           "C 523.D 555.E 555.C 535.A 425.A 425." +
                           "@ @09.";

        string leftHand =  "E 38B.E 38B.A 38B.A 38B.G#38B.E 38A.A 38F." +
                           "@ @06." +
                           "D 38A.D 38A.C 38B.C 38B.E 38B.E 38A.A 38F." +
                           "@ @0A.";

        private int[] musicDelays = new int[] { 75, 70, 65, 60, 50, 45 };
        private AutoResetEvent waiter1 = new AutoResetEvent(false);
        private AutoResetEvent waiter2 = new AutoResetEvent(false);

        private void StartThemeMusic() {
            Thread rightHandThread = new Thread(() => {
                int noteIndex = 0;
                string note = "";
                int duration = 0;
                int delay = 0;

                while(true) {
                    note = rightHand.Split('.')[noteIndex];
                    duration = Convert.ToByte(note[3].ToString(), 16) * musicDelays[gameLevel];
                    delay = Convert.ToByte(note[4].ToString(), 16) * musicDelays[gameLevel];
                    PlayNote(note.Substring(0, 3), duration);
                    noteIndex += 1;
                    if(noteIndex * 6 >= rightHand.Length) noteIndex = 0;

                    Thread.Sleep(delay);

                    if(noteIndex == 0) { //  Resync
                        waiter2.Set();
                        waiter1.WaitOne();
                    }
                }
            }) { IsBackground = true };

            Thread leftHandThread = new Thread(() => {
                int noteIndex = 0;
                string note = "";
                int duration = 0;
                int delay = 0;

                while(true) {
                    note = leftHand.Split('.')[noteIndex];
                    duration = Convert.ToByte(note[3].ToString(), 16) * musicDelays[gameLevel];
                    delay = Convert.ToByte(note[4].ToString(), 16) * musicDelays[gameLevel];
                    //System.Diagnostics.Debug.WriteLine(note);
                    PlayNote(note.Substring(0, 3), duration);
                    noteIndex += 1;
                    if(noteIndex * 6 >= leftHand.Length) noteIndex = 0;

                    Thread.Sleep(delay);

                    if(noteIndex == 0) {  //  Resync
                        waiter1.Set();
                        waiter2.WaitOne();
                    }
                }
            }) { IsBackground = true };

            leftHandThread.Start();
            rightHandThread.Start();
        }
    }
}
