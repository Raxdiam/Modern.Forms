﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SkiaSharp;

namespace Modern.Forms
{
    public class RibbonItemGroup : ILayoutable
    {
        public string Text { get; set; } = string.Empty;

        public RibbonItemCollection Items { get; }

        public Rectangle Bounds { get; private set; }

        public Padding Margin => Padding.Empty;

        public RibbonTabPage? Owner { get; set; }

        public Padding Padding => new Padding (3, 3, 4, 3);

        public RibbonItemGroup ()
        {
            Items = new RibbonItemCollection (this);
        }

        public void SetBounds (int x, int y, int width, int height, BoundsSpecified specified = BoundsSpecified.All)
        {
            Bounds = new Rectangle (x, y, width, height);

            // Lay out RibbonItems
            StackLayoutEngine.HorizontalExpand.Layout (PaddedBounds, Items.Cast<ILayoutable> ());
        }

        public void DrawGroup (SKCanvas canvas)
        {
            // Draw each ribbon item
            foreach (var item in Items)
                item.FireEvent (new PaintEventArgs(SKImageInfo.Empty, canvas, Owner?.Owner?.Scaling ?? 1), ItemEventType.Paint);

            // Right border (group separator)
            canvas.DrawLine (Bounds.Right - 1, Bounds.Top + 4, Bounds.Right - 1, Bounds.Bottom - 4, Theme.BorderGray);
        }

        public Size GetPreferredSize (Size proposedSize)
        {
            var width = Padding.Horizontal;

            foreach (var item in Items)
                width += item.GetPreferredSize (Size.Empty).Width;

            return new Size (width, 0);
        }

        private Rectangle PaddedBounds {
            get {
                var x = Bounds.Left + Padding.Left;
                var y = Bounds.Top + Padding.Top;
                var w = Bounds.Width - Padding.Horizontal;
                var h = Bounds.Height - Padding.Vertical;
                return new Rectangle (x, y, w, h);
            }
        }
    }
}
