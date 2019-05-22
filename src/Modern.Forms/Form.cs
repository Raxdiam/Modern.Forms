﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Win32.Input;
using SkiaSharp;

namespace Modern.Forms
{
    public class Form : ICloseable
    {
        private IWindowImpl window;
        private ControlAdapter adapter;
        private string text;

        public static ControlStyle DefaultStyle = new ControlStyle (Control.DefaultStyle,
         (style) => {
             style.BackgroundColor = Theme.FormBackgroundColor;
             style.Border.Color = Theme.RibbonColor;
             style.Border.Width = 1;
         });

        public ControlStyle Style { get; } = new ControlStyle (DefaultStyle);

        public Form ()
        {
            adapter = new ControlAdapter (this);

            window = AvaloniaGlobals.WindowingInterface.CreateWindow ();

            Screens = new Screens (window.Screen);

            window.SetSystemDecorations (false);
            window.Resize (new Size (DefaultSize.Width, DefaultSize.Height));
            window.Input = OnInput;
            window.Paint = OnPaint;
            window.Resized = OnResize;
            window.Closed = () => Closed?.Invoke (this, EventArgs.Empty);

            SetWindowStartupLocation ();
        }

        public ControlCollection Controls => adapter.Controls;

        protected virtual System.Drawing.Size DefaultSize => new System.Drawing.Size (1080, 720);

        public string Text {
            get => text;
            set {
                if (text != value) {
                    text = value;
                    window.SetTitle (text);
                }
            }
        }

        public FormWindowState WindowState {
            get => (FormWindowState)window.WindowState;
            set => window.WindowState = (Avalonia.Controls.WindowState)value;
        }

        public event EventHandler Closed;

        public void BeginMoveDrag () => window.BeginMoveDrag ();
        public void Close () => window.Dispose ();
        public System.Drawing.Rectangle DisplayRectangle => new System.Drawing.Rectangle (Style.Border.Left.GetWidth (), Style.Border.Top.GetWidth (), (int)window.ClientSize.Width - Style.Border.Right.GetWidth () - Style.Border.Left.GetWidth (), (int)window.ClientSize.Height - Style.Border.Top.GetWidth () - Style.Border.Bottom.GetWidth ());
        public void Invalidate () => window.Invalidate (new Rect (window.ClientSize));
        public PixelPoint Location {
            get => window.Position;
            set {
                if (window.Position != value) {
                    window.Position = value;
                }
            }
        }
        public Screens Screens { get; private set; }
        public void Show () => window.Show ();
        public void ShowDialog (Form parent) => window.ShowDialog (parent.window);

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public FormStartPosition StartPosition { get; set; } = FormStartPosition.CenterScreen;

        private DateTime last_click_time;

        private MouseEventArgs BuildMouseClickArgs (MouseButtons buttons, Point point)
        {
            var click_count = 1;

            if (DateTime.Now.Subtract (last_click_time).TotalMilliseconds < 500)
                click_count = 2;

            var e = new MouseEventArgs (buttons, click_count, (int)point.X, (int)point.Y, 0);

            last_click_time = click_count > 1 ? DateTime.MinValue : DateTime.Now;

            return e;
        }

        private void OnInput (RawInputEventArgs e)
        {
            if (e is RawMouseEventArgs me) {
                switch (me.Type) {
                    case RawMouseEventType.LeftButtonDown:
                        var lbd_e = new MouseEventArgs (MouseButtons.Left, 1, (int)me.Position.X, (int)me.Position.Y, 0);
                        adapter.RaiseMouseDown (lbd_e);
                        break;
                    case RawMouseEventType.LeftButtonUp:
                        var lbu_e = BuildMouseClickArgs (MouseButtons.Left, me.Position);

                        if (lbu_e.Clicks > 1)
                            adapter.RaiseDoubleClick (lbu_e);

                        adapter.RaiseClick (lbu_e);
                        adapter.RaiseMouseUp (lbu_e);
                        break;
                    case RawMouseEventType.MiddleButtonDown:
                        var mbd_e = new MouseEventArgs (MouseButtons.Middle, 1, (int)me.Position.X, (int)me.Position.Y, 0);
                        adapter.RaiseMouseDown (mbd_e);
                        break;
                    case RawMouseEventType.MiddleButtonUp:
                        var mbu_e = BuildMouseClickArgs (MouseButtons.Middle, me.Position);

                        if (mbu_e.Clicks > 1)
                            adapter.RaiseDoubleClick (mbu_e);

                        adapter.RaiseClick (mbu_e);
                        adapter.RaiseMouseUp (mbu_e);
                        break;
                    case RawMouseEventType.RightButtonDown:
                        var rbd_e = new MouseEventArgs (MouseButtons.Right, 1, (int)me.Position.X, (int)me.Position.Y, 0);
                        adapter.RaiseMouseDown (rbd_e);
                        break;
                    case RawMouseEventType.RightButtonUp:
                        var rbu_e = BuildMouseClickArgs (MouseButtons.Right, me.Position);

                        if (rbu_e.Clicks > 1)
                            adapter.RaiseDoubleClick (rbu_e);

                        adapter.RaiseClick (rbu_e);
                        adapter.RaiseMouseUp (rbu_e);
                        break;
                    case RawMouseEventType.LeaveWindow:
                        var lw_e = new MouseEventArgs (MouseButtons.None, 0, (int)me.Position.X, (int)me.Position.Y, 0);
                        adapter.RaiseMouseLeave (lw_e);
                        break;
                    case RawMouseEventType.Move:
                        var mea = new MouseEventArgs (MouseButtons.None, 0, (int)me.Position.X, (int)me.Position.Y, 0);
                        adapter.RaiseMouseMove (mea);
                        break;
                }
            } else if (e is RawKeyEventArgs ke) {
                switch (ke.Type) {
                    case RawKeyEventType.KeyDown:
                        var kd_e = new KeyEventArgs ((Keys)KeyInterop.VirtualKeyFromKey (ke.Key));
                        adapter.RaiseKeyDown (kd_e);
                        break;
                    //case RawKeyEventType.KeyUp:
                    //    var ku_e = new KeyEventArgs ((Keys)KeyInterop.VirtualKeyFromKey (ke.Key));
                    //    adapter.RaiseKeyUp (ku_e);
                    //    break;
                }
            } else if (e is RawTextInputEventArgs te) {
                var kp_e = new KeyPressEventArgs (te.Text[0]);
                adapter.RaiseKeyPress (kp_e);
            }
        }

        private void OnPaint (Rect r)
        {
            var skia_framebuffer = window.Surfaces.OfType<IFramebufferPlatformSurface> ().First ();
            var framebuffer = skia_framebuffer.Lock ();
            var framebufferImageInfo = new SKImageInfo (framebuffer.Size.Width, framebuffer.Size.Height,
                framebuffer.Format.ToSkColorType (),
                framebuffer.Format == PixelFormat.Rgb565 ? SKAlphaType.Opaque : SKAlphaType.Premul);

            using (var surface = SKSurface.Create (framebufferImageInfo, framebuffer.Address, framebuffer.RowBytes)) {
                var e = new SKPaintEventArgs (surface, framebufferImageInfo, surface.Canvas);
                e.Canvas.DrawBackground (new System.Drawing.Rectangle (0, 0, (int)window.ClientSize.Width, (int)window.ClientSize.Height), DefaultStyle);
                e.Canvas.DrawBorder (new System.Drawing.Rectangle (0, 0, (int)window.ClientSize.Width, (int)window.ClientSize.Height), DefaultStyle);

                e.Canvas.ClipRect (new SKRect (DisplayRectangle.Left, DisplayRectangle.Top, DisplayRectangle.Width + 1, DisplayRectangle.Height + 1));

                adapter.RaisePaintBackground (e);
                adapter.RaisePaint (e);
            }

            framebuffer.Dispose ();
        }

        private void OnResize (Size size)
        {
            adapter.SetBounds (DisplayRectangle.Left, DisplayRectangle.Top, DisplayRectangle.Width, DisplayRectangle.Height);
        }

        private void SetWindowStartupLocation (IWindowBaseImpl owner = null)
        {
            var scaling = owner?.Scaling ?? 1; // PlatformImpl?.Scaling ?? 1;

            // TODO: We really need non-client size here.
            var rect = new PixelRect (
                PixelPoint.Origin,
                PixelSize.FromSize (window.ClientSize, scaling));

            if (StartPosition == FormStartPosition.CenterScreen) {
                var screen = Screens.ScreenFromPoint (owner?.Position ?? Location);

                if (screen != null) {
                    Location = screen.WorkingArea.CenterRect (rect).Position;
                }
            } else if (StartPosition == FormStartPosition.CenterParent) {
                if (owner != null) {
                    // TODO: We really need non-client size here.
                    var ownerRect = new PixelRect (
                        owner.Position,
                        PixelSize.FromSize (owner.ClientSize, scaling));
                    Location = ownerRect.CenterRect (rect).Position;
                }
            }
        }
    }
}
