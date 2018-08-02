using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TetrisNet.Classes;

namespace TetrisNet {
    public partial class FormMain : Form {
        private Board board;

        public FormMain() {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
        }

        private void FormMain_Load(object sender, EventArgs e) {
            board = new Board(this, 12, -1, 48, 48);

            Thread render = new Thread(() => {
                while(true) {
                    Thread.Sleep(30);
                    this.Invalidate();
                }
            }) { IsBackground = true };
            render.Start();

            this.Paint += (object s1, PaintEventArgs e1) => {
                Graphics g = e1.Graphics;
                board.Render(g);
            };
        }
    }
}
