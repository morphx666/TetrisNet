﻿using System;
using System.Drawing;

namespace TetrisNet.Classes {
    public class Tetromino : ICloneable {
        public enum TetrominoTypes {
            Line,
            StraightL,
            InvertedL,
            Square,
            StraightSkew,
            InvertedSkew,
            Triangle
        }

        public enum Directions {
            Left,
            Right,
            Down,
            Up,
            Rotate
        }

        public int X;
        public int Y;
        public int Size;
        public struct Bounds {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        public Bounds Area;
        public readonly int BlockWidth;
        public readonly int BlockHeight;
        public readonly Color Color;
        public bool[][] Blocks;
        public readonly TetrominoTypes Type;

        private readonly Pen pd = new Pen(Color.Black);
        private readonly Pen ph = new Pen(Color.FromArgb(128, Color.White), 2);

        public Tetromino(TetrominoTypes type, int blockWidth, int blockHeight) {
            BlockWidth = blockWidth;
            BlockHeight = blockHeight;
            Type = type;

            switch(type) {
                case TetrominoTypes.Line:
                    Color = Color.Cyan;
                    Size = 4;
                    Blocks = CreateLine();
                    break;
                case TetrominoTypes.StraightL:
                    Color = Color.Orange;
                    Size = 3;
                    Blocks = CreateStraightL();
                    break;
                case TetrominoTypes.InvertedL:
                    Color = Color.Blue;
                    Size = 3;
                    Blocks = CreateInvertedL();
                    break;
                case TetrominoTypes.Square:
                    Color = Color.Yellow;
                    Size = 3;
                    Blocks = CreateSquare();
                    break;
                case TetrominoTypes.StraightSkew:
                    Color = Color.Green;
                    Size = 3;
                    Blocks = CreateStraightSkew();
                    break;
                case TetrominoTypes.InvertedSkew:
                    Color = Color.Red;
                    Size = 3;
                    Blocks = CreateInvertedSkew();
                    break;
                case TetrominoTypes.Triangle:
                    Color = Color.Purple;
                    Size = 3;
                    Blocks = CreateTriangle();
                    break;
            }
            UpdateArea();
        }

        public void Move(Directions d) {
            switch(d) {
                case Directions.Left:
                    X -= BlockWidth;
                    break;
                case Directions.Right:
                    X += BlockWidth;
                    break;
                case Directions.Down:
                    Y += BlockHeight;
                    break;
                case Directions.Up:
                    Y -= BlockHeight;
                    break;
                case Directions.Rotate:
                    if(Type == TetrominoTypes.Square) break;
                    // https://www.geeksforgeeks.org/inplace-rotate-square-matrix-by-90-degrees/
                    for(int x = 0; x < Size / 2; x++) {
                        // Consider elements in group of matrixSize in 
                        // current square
                        for(int y = x; y < Size - x - 1; y++) {
                            // store current cell in temp variable
                            bool temp = Blocks[x][y];

                            // move values from right to top
                            Blocks[x][y] = Blocks[y][Size - 1 - x];

                            // move values from bottom to right
                            Blocks[y][Size - 1 - x] = Blocks[Size - 1 - x][Size - 1 - y];

                            // move values from left to bottom
                            Blocks[Size - 1 - x][Size - 1 - y] = Blocks[Size - 1 - y][x];

                            // assign temp to left
                            Blocks[Size - 1 - y][x] = temp;
                        }
                    }
                    UpdateArea();
                    break;
            }
        }

        public void UpdateArea() {
            Area.Left = Size;
            Area.Top = Size;
            Area.Right = 0;
            Area.Bottom = 0;

            for(int x = 0; x < 4; x++) {
                for(int y = 0; y < 4; y++) {
                    if(Blocks[x][y]) {
                        if(x < Area.Left) Area.Left = x;
                        if(x > Area.Right) Area.Right = x;
                        if(y < Area.Top) Area.Top = y;
                        if(y > Area.Bottom) Area.Bottom = y;
                    }
                }
            }
        }

        public void Render(Graphics g) {
            using(Brush b = new SolidBrush(Color)) {
                for(int x = 0; x < Blocks.Length; x++) {
                    for(int y = 0; y < Blocks.Length; y++) {
                        if(Blocks[x][y]) {
                            g.TranslateTransform(X, Y);

                            int x1 = x * BlockWidth;
                            int y1 = y * BlockHeight;

                            g.FillRectangle(b, x1, y1, BlockWidth, BlockHeight);

                            g.DrawLine(ph, x1 + 2, y1 + 2,      x1 + BlockWidth, y1 + 2);
                            g.DrawLine(ph, x1 + BlockWidth, y1, x1 + BlockWidth, y1 + BlockHeight);

                            g.DrawRectangle(pd, x1, y1, BlockWidth, BlockHeight);

                            g.ResetTransform();
                        }
                    }
                }
            }
        }

        #region Tetrominos Creation
        private bool[][] CreateLine() {
            // ◌◌◌◌
            // ●●●●
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            for(int x = 0; x < 4; x++) {
                b[x][1] = true;
            }
            return b;
        }

        private bool[][] CreateStraightL() {
            // ◌◌●◌
            // ●●●◌
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            for(int x = 0; x < 3; x++) {
                b[x][1] = true;
            }
            b[2][0] = true;
            return b;
        }

        private bool[][] CreateInvertedL() {
            // ●◌◌◌
            // ●●●◌
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            for(int x = 0; x < 3; x++) {
                b[x][1] = true;
            }
            b[0][0] = true;
            return b;
        }

        private bool[][] CreateSquare() {
            // ◌●●◌
            // ◌●●◌
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            for(int x = 1; x < 3; x++) {
                for(int y = 0; y < 2; y++) {
                    b[x][y] = true;
                }
            }
            return b;
        }

        private bool[][] CreateStraightSkew() {
            // ◌●●◌
            // ●●◌◌
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            b[1][0] = true;
            b[2][0] = true;
            b[0][1] = true;
            b[1][1] = true;
            return b;
        }

        private bool[][] CreateInvertedSkew() {
            // ●●◌◌
            // ◌●●◌
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            b[0][0] = true;
            b[1][0] = true;
            b[1][1] = true;
            b[2][1] = true;
            return b;
        }

        private bool[][] CreateTriangle() {
            // ◌●◌◌
            // ●●●◌
            // ◌◌◌◌
            // ◌◌◌◌
            bool[][] b = Init();
            b[1][0] = true;
            b[0][1] = true;
            b[1][1] = true;
            b[2][1] = true;
            return b;
        }

        private bool[][] Init() {
            bool[][] b = new bool[4][];
            for(int i = 0; i < 4; i++) {
                b[i] = new bool[4];
                for(int j = 0; j < 4; j++) {
                    b[i][j] = false;
                }
            }
            return b;
        }
        #endregion

        public object Clone() {
            Tetromino p = new Tetromino(Type, BlockWidth, BlockHeight) {
                X = this.X,
                Y = this.Y
            };
            p.Blocks = (bool[][])this.Blocks.Clone();
            p.UpdateArea();
            return p;
        }
    }
}