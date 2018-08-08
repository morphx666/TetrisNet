using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TetrisNet.Classes {
    public partial class Board : IDisposable {
        public int GridWidth;
        public int GridHeight;
        public int BlockWidth;
        public int BlockHeight;

        private Tetromino activeTetromino;
        private readonly Form parent;
        private Random r = new Random();
        private int[] gameLoopsDelays = new int[] { 1000, 750, 500, 300, 150, 75 };
        private int gameLevel = 1;
        private int linesCounter = 0;
        private bool suspendGameLoop = false;
        private bool showBanner = false;
        private Font gameFont = new Font("Segoe UI", 38, FontStyle.Bold);
        private string banner = "";

        private bool gameOver;

        private struct Cell {
            public bool Value;
            public Color Color;
        }

        private readonly Cell[][] Cells;

        public Board(Form parent, int gridWidth, int gridHeight = -1, int blockWidth = 16, int blockHeight = 16) {
            this.parent = parent;
            GridWidth = gridWidth;
            if(gridHeight == -1) {
                GridHeight = parent.Height / blockHeight;
                GridHeight -= 4;
            } else {
                GridHeight = gridHeight;
            }
            BlockWidth = blockWidth;
            BlockHeight = blockHeight;

            //InitAudioService();

            Cells = new Cell[GridWidth][];
            for(int x = 0; x < GridWidth; x++) {
                Cells[x] = new Cell[GridHeight];
            }

            parent.KeyDown += (object s, KeyEventArgs e) => {
                if(suspendGameLoop || gameOver) return;

                switch(e.KeyCode) {
                    case Keys.Left:
                        if(CanMove(Tetromino.Directions.Left)) activeTetromino.Move(Tetromino.Directions.Left);
                        break;
                    case Keys.Right:
                        if(CanMove(Tetromino.Directions.Right)) activeTetromino.Move(Tetromino.Directions.Right);
                        break;
                    case Keys.Down:
                        MoveDown();
                        break;
                    case Keys.Space:
                        Task.Run(() => { while(MoveDown()) Thread.Sleep(30); });
                        break;
                    case Keys.Up:
                        activeTetromino.Move(Tetromino.Directions.Rotate);
                        bool isOutOfBounds;
                        do {
                            isOutOfBounds = false;
                            if(activeTetromino.X / BlockWidth + activeTetromino.Area.Left < 0) {
                                activeTetromino.Move(Tetromino.Directions.Right);
                                isOutOfBounds = true;
                            }
                            if(activeTetromino.X / BlockWidth + activeTetromino.Area.Right >= GridWidth) {
                                activeTetromino.Move(Tetromino.Directions.Left);
                                isOutOfBounds = true;
                            }
                            if(activeTetromino.Y / BlockHeight + activeTetromino.Area.Top < 0) {
                                activeTetromino.Move(Tetromino.Directions.Down);
                                isOutOfBounds = true;
                            }
                            if(activeTetromino.Y / BlockHeight + activeTetromino.Area.Bottom >= GridHeight) {
                                activeTetromino.Move(Tetromino.Directions.Up);
                                isOutOfBounds = true;
                            }
                        } while(isOutOfBounds);
                        break;
                }
            };

            Thread gameLoop = new Thread(() => {
                ShowBanner($"LEVEL {gameLevel}");
                //StartThemeMusic();

                while(!gameOver) {
                    Thread.Sleep(gameLoopsDelays[gameLevel - 1]);
                    if(!suspendGameLoop) MoveDown();
                }
            }) { IsBackground = true };
            gameLoop.Start();

            AddNewRandomTetromino();
        }

        private void AddNewRandomTetromino() {
            Array values = Enum.GetValues(typeof(Tetromino.TetrominoTypes));
            Tetromino.TetrominoTypes t = (Tetromino.TetrominoTypes)r.Next(values.Length);

            if(activeTetromino != null && activeTetromino.Type == t) t = (Tetromino.TetrominoTypes)r.Next(values.Length);

            activeTetromino = new Tetromino(t, BlockWidth, BlockHeight);
            activeTetromino.X = BlockWidth * (GridWidth - activeTetromino.Size) / 2;
            activeTetromino.X -= activeTetromino.X % BlockWidth;
            activeTetromino.Y -= activeTetromino.Area.Top * BlockHeight;

            if(!CanMove(Tetromino.Directions.Down)) {
                Task.Run(() => ShowBanner("GAME OVER", 5000));
                gameOver = true;
            }
        }

        private bool CanMove(Tetromino.Directions d) {
            Tetromino tmp = (Tetromino)activeTetromino.Clone();
            tmp.Move(d);

            if(tmp.X / BlockWidth + tmp.Area.Left < 0) return false;
            if(tmp.X / BlockWidth + tmp.Area.Right > GridWidth - 1) return false;
            if(tmp.Y / BlockWidth + tmp.Area.Bottom > GridHeight - 1) return false;

            for(int x = tmp.Area.Left; x <= tmp.Area.Right; x++) {
                for(int y = tmp.Area.Top; y <= tmp.Area.Bottom; y++) {
                    if(tmp.Blocks[x][y] && Cells[tmp.X / BlockWidth + x][tmp.Y / BlockHeight + y].Value) return false;
                }
            }

            return true;
        }

        private bool MoveDown() {
            if(activeTetromino == null) return false;
            if(CanMove(Tetromino.Directions.Down)) {
                activeTetromino.Move(Tetromino.Directions.Down);
                return true;
            } else {
                for(int x = activeTetromino.Area.Left; x <= activeTetromino.Area.Right; x++) {
                    for(int y = activeTetromino.Area.Top; y <= activeTetromino.Area.Bottom; y++) {
                        if(activeTetromino.Blocks[x][y]) {
                            Cells[activeTetromino.X / BlockWidth + x][activeTetromino.Y / BlockHeight + y].Value = true;
                            Cells[activeTetromino.X / BlockWidth + x][activeTetromino.Y / BlockHeight + y].Color = activeTetromino.Color;
                        }
                    }
                }

                AddNewRandomTetromino();
                Task.Run(() => CheckFullLines());
            }
            return false;
        }

        private void CheckFullLines() {
            suspendGameLoop = true;

            bool lineContainsTetrominos = false;
            bool lineIsComplete = true;

            for(int y = GridHeight - 1; y >= 0; y--) {
                lineContainsTetrominos = false;
                lineIsComplete = true;

                for(int x = 0; x < GridWidth; x++) {
                    if(Cells[x][y].Value) {
                        lineContainsTetrominos = true;
                    } else {
                        lineIsComplete = false;
                        if(lineContainsTetrominos) break;
                    }
                }

                if(!lineContainsTetrominos) {
                    break;
                } else if(lineIsComplete) {
                    for(int x = 0; x < GridWidth; x++) {
                        Cells[x][y].Color = Color.Black;
                    }
                    Thread.Sleep(250);

                    for(int y1 = y - 1; y1 >= 0; y1--) {
                        lineContainsTetrominos = false;
                        for(int x = 0; x < GridWidth; x++) {
                            Cells[x][y1 + 1] = Cells[x][y1];
                            if(Cells[x][y1].Value) lineContainsTetrominos = true;
                        }
                        Thread.Sleep(30);
                        if(!lineContainsTetrominos) break;
                    }

                    linesCounter += 1;
                    if(linesCounter == 10) {
                        linesCounter = 0;
                        if(gameLevel <= gameLoopsDelays.Length) {
                            gameLevel += 1;
                            ShowBanner($"LEVEL {gameLevel}");
                            suspendGameLoop = true;
                        }
                    }

                    y++;
                }
            }

            suspendGameLoop = false;
        }

        private void ShowBanner(string msg, int duration = 3000) {
            banner = msg;

            suspendGameLoop = true;
            showBanner = true;

            Thread.Sleep(duration);

            showBanner = false;
            suspendGameLoop = false;
        }

        public void Render(Graphics g) {
            g.Clear(Color.LightGray);
            int w = GridWidth * BlockWidth + 1;
            int h = GridHeight * BlockHeight + 1;
            int x = (parent.Width - w) / 2;
            int y = (parent.Height - h) / 2;
            Brush bk = new SolidBrush(Color.FromArgb(255, 33, 33, 33));
            g.FillRectangle(bk, x, y, w, h);

            g.TranslateTransform(x, y);
            g.BeginContainer();

            bool lineContainsTetrominos;
            Pen pd = new Pen(Color.Black);
            Pen ph = new Pen(Color.FromArgb(128, Color.White), 2);
            for(y = GridHeight - 1; y >= 0; y--) {
                lineContainsTetrominos = false;
                for(x = 0; x < GridWidth; x++) {
                    if(Cells[x][y].Value) {
                        Brush b = new SolidBrush(Cells[x][y].Color);
                        g.FillRectangle(b, x * BlockWidth, y * BlockHeight, BlockWidth, BlockHeight);

                        g.DrawLine(ph, x * BlockWidth + 2, y * BlockHeight + 2, x * BlockWidth + BlockWidth, y * BlockHeight + 2);
                        g.DrawLine(ph, x * BlockWidth + BlockWidth, y * BlockHeight, x * BlockWidth + BlockWidth, y * BlockHeight + BlockHeight);

                        g.DrawRectangle(pd, x * BlockWidth, y * BlockHeight, BlockWidth, BlockHeight);

                        b.Dispose();
                        lineContainsTetrominos = true;
                    } else {
                        g.FillRectangle(bk, x * BlockWidth, y * BlockHeight, BlockWidth, BlockHeight);
                    }
                }
                if(!lineContainsTetrominos) break;
            }
            pd.Dispose();
            ph.Dispose();
            bk.Dispose();

            activeTetromino.Render(g);

            if(showBanner) {
                SizeF s = g.MeasureString(banner, gameFont);
                x = (int)(w - s.Width) / 2;
                y = (int)(h - s.Height) / 2;

                using(SolidBrush sb = new SolidBrush(Color.FromArgb(160, Color.SlateBlue))) {
                    g.FillRectangle(sb, x - s.Width / 2, y - s.Height / 2, s.Width * 2, s.Height * 2);
                }
                g.DrawString(banner, gameFont, Brushes.Gainsboro, x, y);
            }
        }

        public void Dispose() {
            //amMelody.Close();
            //amBeat.Close();
        }
    }
}