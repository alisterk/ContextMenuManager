﻿using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class ReadOnlyTextBox : TextBox
    {
        public ReadOnlyTextBox()
        {
            this.ReadOnly = true;
            this.Multiline = true;
            this.ShortcutsEnabled = false;
            this.BackColor = Color.White;
            this.ForeColor = Color.FromArgb(80, 80, 80);
            this.Font = SystemFonts.MenuFont;
            this.Font = new Font(this.Font.FontFamily, this.Font.Size + 1F);
        }

        const int WM_SETFOCUS = 0x0007;
        const int WM_KILLFOCUS = 0x0008;
        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case WM_SETFOCUS:
                    m.Msg = WM_KILLFOCUS; break;
            }
            base.WndProc(ref m);
        }

        private bool firstEnter = true;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if(firstEnter) this.Focus();
            firstEnter = false;
        }
    }

    public sealed class ReadOnlyRichTextBox : RichTextBox
    {
        public ReadOnlyRichTextBox()
        {
            this.ReadOnly = true;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.ForeColor = Color.FromArgb(80, 80, 80);
            this.Font = SystemFonts.MenuFont;
            this.Font = new Font(this.Font.FontFamily, this.Font.Size + 1F);
        }

        const int WM_SETFOCUS = 0x0007;
        const int WM_KILLFOCUS = 0x0008;

        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case WM_SETFOCUS:
                    m.Msg = WM_KILLFOCUS; break;
            }
            base.WndProc(ref m);
        }

        private bool firstEnter = true;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if(firstEnter) this.Focus();
            firstEnter = false;
        }

        protected override void OnLinkClicked(LinkClickedEventArgs e)
        {
            base.OnLinkClicked(e);
            ExternalProgram.OpenWebUrl(e.LinkText);
        }
    }
}