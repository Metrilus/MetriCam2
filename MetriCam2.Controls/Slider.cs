using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MetriCam2.Controls
{
    public partial class Slider : PictureBox
    {
        public delegate void ValueChangedHandler(object sender, SliderValueEventArgs e);

        public event ValueChangedHandler ValueChanged;

        #region Private Fields
        private bool isMouseOver = false;
        private bool mouseIsDown = false;
        private bool mouseMoved = false;
        private Font font;
        private int value;
        private TextBox tb = new TextBox();
        private SizeF valueStringSize;
        #endregion

        #region Public Properties
        public int Minimum { get; set; }
        
        public int Maximum { get; set; }

        public override Font Font
        {
            get { return font; }
            set
            {
                font = value;
                this.ClientSize = new Size(this.ClientSize.Width, font.Height + 8);
                this.MinimumSize = new Size(1, font.Height + 8);
                this.MaximumSize = new Size(1920, font.Height + 8);
            }
        }

        public int Value
        {
            get { return value; }
            set 
            { 
                this.value = value; 
                RefreshValue(); 
            }
        }
        #endregion

        public Slider()
        {
            Maximum = 100;
            Minimum = 0;
            value = 50;

            InitializeComponent();

            this.Font = new Font("Segoe UI", 25);
            this.LostFocus += (object sender, EventArgs e) => { Invalidate(); };
            this.MouseDown += Slider_MouseDown;
            this.MouseMove += Slider_MouseMove;
            this.MouseUp += Slider_MouseUp;
            this.MouseEnter += Slider_MouseEnter;
            this.MouseLeave += Slider_MouseLeave;
            this.Resize += (object sender, EventArgs e) => { Invalidate(); };

            float fontSize = (this.Font.Size * 0.5f);
            if (fontSize < 12)
            {
                fontSize = this.Font.Size;
            }
            tb.Font = new Font("Segoe UI", fontSize);
            tb.KeyPress += tb_KeyPress;
            tb.MouseLeave += tb_MouseLeave;
            tb.PreviewKeyDown += tb_PreviewKeyDown;
        }

        private void tb_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                SetManualSliderValue(true);
            }
        }

        private void tb_MouseLeave(object sender, EventArgs e)
        {
            SetManualSliderValue(false);
            Invalidate();
        }

        private void Slider_MouseLeave(object sender, EventArgs e)
        {
            isMouseOver = false;
            Invalidate();
        }

        private void ShowTextBox()
        {
            tb.Text = value.ToString();
            int locationTbHeight = (this.font.Height - tb.Font.Height) / 2;
            tb.Location = new Point(this.ClientSize.Width / 2 - tb.Width / 2, locationTbHeight);
            this.Controls.Add(tb);
            Point mousePos = new Point(this.ClientSize.Width / 2, this.ClientSize.Height / 2);
            Cursor.Position = this.PointToScreen(mousePos);
            tb.Focus();
            mouseMoved = false;
        }

        private void tb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                e.Handled = true;
            switch (e.KeyChar)
            {
                case (char)27:
                    SetManualSliderValue(false);
                    break;
                case (char)13:
                    SetManualSliderValue(true);
                    break;
            }
        }

        private void SetManualSliderValue(bool update)
        {
            this.Controls.Remove(tb);
            if (update)
            {
                int v = Convert.ToInt32(tb.Text);
                SetValue(v);
            }
        }

        private void Slider_MouseEnter(object sender, EventArgs e)
        {
            isMouseOver = true;
            Invalidate();
        }

        private void RefreshValue()
        {
            if (ValueChanged != null)
            {
                SliderValueEventArgs args = new SliderValueEventArgs();
                args.Value = value;
                ValueChanged(this, args);
            }
            this.Refresh();
        }

        private void Slider_MouseUp(object sender, MouseEventArgs e)
        {
            mouseIsDown = false;
            if (IsInValueStringLocation(e.Location) && !mouseMoved)
            {
                ShowTextBox();
            }
        }

        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mouseIsDown) 
                return;
            mouseMoved = true;
            SetValue(XtoValue(e.X));
        }

        private void Slider_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();
            mouseIsDown = true;
            mouseMoved = false;
            if (!IsInValueStringLocation(e.Location))
            {
                SetValue(XtoValue(e.X));
            }
        }

        private bool IsInValueStringLocation(Point p)
        {
            return 
                p.X > ClientSize.Width / 2 - valueStringSize.Width / 2 &&
                p.X < ClientSize.Width / 2 + valueStringSize.Width / 2 &&
                p.Y > ClientSize.Height / 2 - valueStringSize.Height / 2 &&
                p.Y < ClientSize.Height / 2 + valueStringSize.Height / 2;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            float x = ValueToX(value);
            Color borderColor;
            Color textColor;
            Color backColor;
            if (this.Enabled)
            {
                backColor = Color.White;
                if (this.Focused || isMouseOver)
                {
                    borderColor = Color.FromArgb(86, 157, 229);
                }
                else
                {
                    borderColor = Color.FromArgb(171, 173, 179);
                }
                textColor = Color.Black;
            }
            else
            {
                backColor = Color.FromArgb(240, 240, 240);
                textColor = Color.DimGray;
                borderColor = Color.FromArgb(217, 217, 217);
            }

            pe.Graphics.FillRectangle(new SolidBrush(backColor), new Rectangle(0, 0, this.ClientSize.Width, font.Height + 8));
            pe.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(205, 205, 205)), new Rectangle(0, 0, (int)x, font.Height + 8));
            valueStringSize = pe.Graphics.MeasureString(value.ToString(), font);

            if (!this.Enabled)
            {
                pe.Graphics.DrawRectangle(new Pen(Color.White), new Rectangle(1, 1, this.ClientSize.Width - 3, font.Height + 5));
            }
            pe.Graphics.DrawString(value.ToString(), font, new SolidBrush(textColor), (this.ClientSize.Width / 2) - (valueStringSize.Width / 2), 2);
            pe.Graphics.DrawRectangle(new Pen(borderColor), new Rectangle(0, 0, this.ClientSize.Width - 1, font.Height + 7));
        }

        private void SetValue(int v)
        {
            if (v < Minimum)
            {
                v = Minimum;
            }
            if (v > Maximum)
            {
                v = Maximum;
            }

            if (v == value)
            {
                return;
            }

            value = v;
            RefreshValue();
        }

        private int XtoValue(int x)
        {
            return Minimum + (Maximum - Minimum) * x / (int)(this.ClientSize.Width - 1);
        }

        private float ValueToX(float value)
        {
            return (this.ClientSize.Width - 1) * (value - Minimum) / (float)(Maximum - Minimum);
        }
    }

    public class SliderValueEventArgs : EventArgs
    {
        public int Value { get; set; }
    }
}
