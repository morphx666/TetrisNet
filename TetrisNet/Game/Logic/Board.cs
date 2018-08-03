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

        private Piece activePiece;
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

            InitAudioService();

            Cells = new Cell[GridWidth][];
            for(int x = 0; x < GridWidth; x++) {
                Cells[x] = new Cell[GridHeight];
            }

            parent.KeyDown += (object s, KeyEventArgs e) => {
                if(suspendGameLoop || gameOver) return;

                switch(e.KeyCode) {
                    case Keys.Left:
                        if(CanMove(Piece.Directions.Left)) activePiece.Move(Piece.Directions.Left);
                        break;
                    case Keys.Right:
                        if(CanMove(Piece.Directions.Right)) activePiece.Move(Piece.Directions.Right);
                        break;
                    case Keys.Down:
                        MoveDown();
                        break;
                    case Keys.Space:
                        Task.Run(() => { while(MoveDown()) Thread.Sleep(30); });
                        break;
                    case Keys.Up:
                        activePiece.Move(Piece.Directions.Rotate);
                        bool isOutOfBounds;
                        do {
                            isOutOfBounds = false;
                            if(activePiece.X / BlockWidth + activePiece.Area.Left < 0) {
                                activePiece.Move(Piece.Directions.Right);
                                isOutOfBounds = true;
                            }
                            if(activePiece.X / BlockWidth + activePiece.Area.Right >= GridWidth) {
                                activePiece.Move(Piece.Directions.Left);
                                isOutOfBounds = true;
                            }
                            if(activePiece.Y / BlockHeight + activePiece.Area.Top < 0) {
                                activePiece.Move(Piece.Directions.Down);
                                isOutOfBounds = true;
                            }
                            if(activePiece.Y / BlockHeight + activePiece.Area.Bottom >= GridHeight) {
                                activePiece.Move(Piece.Directions.Up);
                                isOutOfBounds = true;
                            }
                        } while(isOutOfBounds);
                        break;
                }
            };

            Thread gameLoop = new Thread(() => {
                ShowBanner($"LEVEL {gameLevel}");
                StartThemeMusic();

                while(!gameOver) {
                    Thread.Sleep(gameLoopsDelays[gameLevel - 1]);
                    if(!suspendGameLoop) MoveDown();
                }
            }) { IsBackground = true };
            gameLoop.Start();

            AddNewRandomPiece();
        }

        private void AddNewRandomPiece() {
            Array values = Enum.GetValues(typeof(Piece.PieceTypes));
            Piece.PieceTypes t = (Piece.PieceTypes)r.Next(values.Length);

            if(activePiece != null && activePiece.Type == t) t = (Piece.PieceTypes)r.Next(values.Length);

            activePiece = new Piece(t, BlockWidth, BlockHeight);
            activePiece.X = BlockWidth * (GridWidth - activePiece.Size) / 2;
            activePiece.X -= activePiece.X % BlockWidth;
            activePiece.Y -= activePiece.Area.Top * BlockHeight;

            if(!CanMove(Piece.Directions.Down)) {
                Task.Run(() => ShowBanner("GAME OVER", 5000));
                gameOver = true;
            }
        }

        private bool CanMove(Piece.Directions d) {
            Piece tmp = (Piece)activePiece.Clone();
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
            if(activePiece == null) return false;
            if(CanMove(Piece.Directions.Down)) {
                activePiece.Move(Piece.Directions.Down);
                return true;
            } else {
                for(int x = activePiece.Area.Left; x <= activePiece.Area.Right; x++) {
                    for(int y = activePiece.Area.Top; y <= activePiece.Area.Bottom; y++) {
                        if(activePiece.Blocks[x][y]) {
                            Cells[activePiece.X / BlockWidth + x][activePiece.Y / BlockHeight + y].Value = true;
                            Cells[activePiece.X / BlockWidth + x][activePiece.Y / BlockHeight + y].Color = activePiece.Color;
                        }
                    }
                }

                AddNewRandomPiece();
                Task.Run(() => CheckFullLines());
            }
            return false;
        }

        private void CheckFullLines() {
            suspendGameLoop = true;

            bool lineContainsPieces = false;
            bool lineIsComplete = true;

            for(int y = GridHeight - 1; y >= 0; y--) {
                lineContainsPieces = false;
                lineIsComplete = true;

                for(int x = 0; x < GridWidth; x++) {
                    if(Cells[x][y].Value) {
                        lineContainsPieces = true;
                    } else {
                        lineIsComplete = false;
                        if(lineContainsPieces) break;
                    }
                }

                if(!lineContainsPieces) {
                    break;
                } else if(lineIsComplete) {
                    for(int x = 0; x < GridWidth; x++) {
                        Cells[x][y].Color = Color.Black;
                    }
                    Thread.Sleep(250);

                    for(int y1 = y - 1; y1 >= 0; y1--) {
                        lineContainsPieces = false;
                        for(int x = 0; x < GridWidth; x++) {
                            Cells[x][y1 + 1] = Cells[x][y1];
                            if(Cells[x][y1].Value) lineContainsPieces = true;
                        }
                        Thread.Sleep(30);
                        if(!lineContainsPieces) break;
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

            bool lineContainsPieces;
            Pen pd = new Pen(Color.Black);
            Pen ph = new Pen(Color.FromArgb(128, Color.White), 2);
            for(y = GridHeight - 1; y >= 0; y--) {
                lineContainsPieces = false;
                for(x = 0; x < GridWidth; x++) {
                    if(Cells[x][y].Value) {
                        Brush b = new SolidBrush(Cells[x][y].Color);
                        g.FillRectangle(b, x * BlockWidth, y * BlockHeight, BlockWidth, BlockHeight);

                        g.DrawLine(ph, x * BlockWidth + 2, y * BlockHeight + 2, x * BlockWidth + BlockWidth, y * BlockHeight + 2);
                        g.DrawLine(ph, x * BlockWidth + BlockWidth, y * BlockHeight, x * BlockWidth + BlockWidth, y * BlockHeight + BlockHeight);

                        g.DrawRectangle(pd, x * BlockWidth, y * BlockHeight, BlockWidth, BlockHeight);

                        b.Dispose();
                        lineContainsPieces = true;
                    } else {
                        g.FillRectangle(bk, x * BlockWidth, y * BlockHeight, BlockWidth, BlockHeight);
                    }
                }
                if(!lineContainsPieces) break;
            }
            pd.Dispose();
            ph.Dispose();
            bk.Dispose();

            activePiece.Render(g);

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
            am.Close();
        }
    }
}