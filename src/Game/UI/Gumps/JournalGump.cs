﻿#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps
{
    internal class JournalGump : Gump
    {
        private readonly ExpandableScroll _background;
        private readonly RenderedTextList _journalEntries;
        private readonly ScrollFlag _scrollBar;
        private const int _diffY = 22;
        private bool _isMinimized;
        private HitBox _hitBox;
        private GumpPic _gumpPic;

        public JournalGump() : base(Constants.JOURNAL_LOCALSERIAL, 0)
        {
            Height = 300;
            CanMove = true;
            CanBeSaved = true;

            Add(_gumpPic = new GumpPic(160, 0, 0x82D, 0));
            Add(_background = new ExpandableScroll(0, _diffY, Height - _diffY, 0x1F40)
            {
                TitleGumpID = 0x82A
            });

            const ushort DARK_MODE_JOURNAL_HUE = 903;

            string str = "Dark mode";
            int width = UOFileManager.Fonts.GetWidthASCII(6, str);

            Checkbox darkMode;
            Add(darkMode = new Checkbox(0x00D2, 0x00D3, str, 6, 0x0288, false)
            {
                X = _background.Width - width -2, 
                Y = _diffY + 7,
                IsChecked = ProfileManager.Current.JournalDarkMode
            });

            Hue = (ushort)(ProfileManager.Current.JournalDarkMode ? DARK_MODE_JOURNAL_HUE : 0);
            darkMode.ValueChanged += (sender, e) =>
            {
                var ok = ProfileManager.Current.JournalDarkMode = !ProfileManager.Current.JournalDarkMode;
                Hue = (ushort) (ok ? DARK_MODE_JOURNAL_HUE : 0);
            };

            _scrollBar = new ScrollFlag(-25, _diffY + 36, Height - _diffY, true);

            Add(_journalEntries = new RenderedTextList(25, _diffY + 36, _background.Width - (_scrollBar.Width >> 1) - 5, 200, _scrollBar));

            Add(_scrollBar);

            Add(_hitBox = new HitBox(160, 0, 23, 24));
            _hitBox.MouseUp += _hitBox_MouseUp;
            _gumpPic.MouseDoubleClick += _gumpPic_MouseDoubleClick;
        }

       

        public Hue Hue
        {
            get => _background.Hue;
            set => _background.Hue = value;
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _gumpPic.Graphic = value ? (Graphic) 0x830 : (Graphic) 0x82D;

                    if (value)
                    {
                        _gumpPic.X = 0;
                    }
                    else
                    {
                        _gumpPic.X = 160;
                    }

                    foreach (var c in Children)
                    {
                        if (!c.IsInitialized)
                            c.Initialize();
                        c.IsVisible = !value;
                    }

                    _gumpPic.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }

        protected override void OnMouseWheel(MouseEvent delta)
        {
            _scrollBar.InvokeMouseWheel(delta);
        }

        protected override void OnInitialize()
        {
            InitializeJournalEntries();
            World.Journal.EntryAdded += AddJournalEntry;
        }

        public override void Dispose()
        {
            World.Journal.EntryAdded -= AddJournalEntry;
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;
            _journalEntries.Height = Height - (98 + _diffY);
            base.Update(totalMS, frameMS);
        }

        private void AddJournalEntry(object sender, JournalEntry entry)
        {
            string text = $"{(entry.Name != string.Empty ? $"{entry.Name}: " : string.Empty)}{entry.Text}";
            //TransformFont(ref font, ref asUnicode);
            _journalEntries.AddEntry(text, entry.Font, entry.Hue, entry.IsUnicode, entry.Time);
        }

        //private void TransformFont(ref byte font, ref bool asUnicode)
        //{
        //    if (asUnicode)
        //        return;

        //    switch (font)
        //    {
        //        case 3:

        //        {
        //            font = 1;
        //            asUnicode = true;

        //            break;
        //        }
        //    }
        //}

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_background.SpecialHeight);
            writer.Write(IsMinimized);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            if (Configuration.Profile.GumpsVersion == 2)
            {
                reader.ReadUInt32();
                _isMinimized = reader.ReadBoolean();
            }
            _background.Height = _background.SpecialHeight = reader.ReadInt32();

            if (Profile.GumpsVersion >= 3)
            {
                _isMinimized = reader.ReadBoolean();
            }
        }

        private void InitializeJournalEntries()
        {
            foreach (JournalEntry t in World.Journal.Entries)
                AddJournalEntry(null, t);

            _scrollBar.MinValue = 0;
        }

        private void _gumpPic_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButton.Left && IsMinimized)
            {
                IsMinimized = false;
                e.Result = true;
            }
        }

        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        private class RenderedTextList : Control
        {
            private readonly Deque<RenderedText> _entries, _hours;
            private readonly IScrollBar _scrollBar;

            public RenderedTextList(int x, int y, int width, int height, IScrollBar scrollBarControl)
            {
                _scrollBar = scrollBarControl;
                _scrollBar.IsVisible = false;
                AcceptMouseInput = true;
                CanMove = true;
                X = x;
                Y = y;
                Width = width;
                Height = height;
                _entries = new Deque<RenderedText>();
                _hours = new Deque<RenderedText>();

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                int mx = x;
                int my = y;

                int height = 0;
                int maxheight = _scrollBar.Value + _scrollBar.Height;

                for (int i = 0; i < _entries.Count; i++)
                {
                    var t = _entries[i];
                    var hour = _hours[i];

                    if (height + t.Height <= _scrollBar.Value)
                    {
                        // this entry is above the renderable area.
                        height += t.Height;
                    }
                    else if (height + t.Height <= maxheight)
                    {
                        int yy = height - _scrollBar.Value;

                        if (yy < 0)
                        {
                            // this entry starts above the renderable area, but exists partially within it.
                            hour.Draw(batcher, hour.Width, hour.Height, mx, y, t.Width, t.Height + yy, 0, -yy);
                            t.Draw(batcher, t.Width, t.Height, mx + hour.Width, y, t.Width, t.Height + yy, 0, -yy);
                            my += t.Height + yy;
                        }
                        else
                        {
                            // this entry is completely within the renderable area.
                            hour.Draw(batcher, mx, my);
                            t.Draw(batcher, mx + hour.Width, my);
                            my += t.Height;
                        }

                        height += t.Height;
                    }
                    else
                    {
                        int yyy = maxheight - height;
                        hour.Draw(batcher, hour.Width, hour.Height, mx, y + _scrollBar.Height - yyy, t.Width, yyy, 0, 0);
                        t.Draw(batcher, t.Width, t.Height, mx + hour.Width, y + _scrollBar.Height - yyy, t.Width, yyy, 0, 0);

                        // can't fit any more entries - so we break!
                        break;
                    }
                }

                return true;
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);
                if (!IsVisible)
                    return;

                _scrollBar.X = X + Width - (_scrollBar.Width >> 1) + 5;
                _scrollBar.Height = Height;
                CalculateScrollBarMaxValue();
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }

            private void CalculateScrollBarMaxValue()
            {
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
                int height = 0;

                foreach (RenderedText t in _entries)
                    height += t.Height;

                height -= _scrollBar.Height;

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                        _scrollBar.Value = _scrollBar.MaxValue;
                }
                else
                {
                    _scrollBar.MaxValue = 0;
                    _scrollBar.Value = 0;
                }
            }

            public void AddEntry(string text, int font, Hue hue, bool isUnicode, DateTime time)
            {
                bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;

                while (_entries.Count > 199)
                {
                    _entries.RemoveFromFront().Destroy();
                    _hours.RemoveFromFront().Destroy();
                }

                RenderedText h = RenderedText.Create($"{time:t} ", 1150, 1, true, FontStyle.BlackBorder);

                _hours.AddToBack(h);

                _entries.AddToBack(RenderedText.Create(text, hue, (byte)font, isUnicode, FontStyle.Indention | FontStyle.BlackBorder, maxWidth: Width - (18 + h.Width)));

                _scrollBar.MaxValue += _entries[_entries.Count - 1].Height;
                if (maxScroll) _scrollBar.Value = _scrollBar.MaxValue;
            }


            public override void Dispose()
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    _entries[i].Destroy();
                    _hours[i].Destroy();
                }

                _entries.Clear();
                _hours.Clear();

                base.Dispose();
            }
        }

    }
}