using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic; // Required namespace

[StructLayout(LayoutKind.Sequential)]
file struct POINT
{
    public int x;
    public int y;
}

file static class WinMsg
{
    public const int WM_POINTERDOWN = 0x0246;
    public const int WM_POINTERUP = 0x0247;
    public const int WM_POINTERUPDATE = 0x0245;
    public const int WM_POINTERENTER = 0x0249;
    public const int WM_POINTERLEAVE = 0x024A;
    public const int WM_POINTERCAPTURECHANGED = 0x024C;
    public const int WM_POINTERDEVICEINRANGE = 0X239;
    public const int WM_POINTERDEVICEOUTOFRANGE = 0X23A;
}

file static class Helpers
{
    public static Point GetClientPointFromPointerMsg(EveOPreview.View.ThumbnailOverlay _this, ref Message m)
    {
        var d = m.LParam.ToInt64();
        short x = (short)(d & 0xFFFF);
        short y = (short)(d & 0xFFFF0000);
        Point p1 = new Point(x, y);
        p1 = _this.PointToClient(p1);
        return p1;
    }

    public static uint GetIDFromPointerMsg(ref Message m)
    {
        return (uint)(m.WParam.ToInt64() & 0xFFFF);
    }
}

namespace EveOPreview.View
{
    partial class ThumbnailOverlay
    {
        private List<AddTouchCapability> _hooks;

        private bool HandleIfTouchInput(ref Message m)
        {

            switch (m.Msg)
            {
                case WinMsg.WM_POINTERUPDATE:
                    {
                        var TouchInteraction = ((ThumbnailView)(this.Owner)).TouchInteraction;
                        if (TouchInteraction.MouseDown || (TouchInteraction.MouseEnter && !TouchInteraction.IsPointerDown())) return true;
                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        if (TouchInteraction.PointerDown == id || (!TouchInteraction.IsPointerDown() && TouchInteraction.PointerEnter == id))
                        {
                            var p1 = Helpers.GetClientPointFromPointerMsg(this, ref m);
                            MouseEventArgs mouseEvent = new MouseEventArgs((TouchInteraction.IsPointerDown()) ? MouseButtons.Left : MouseButtons.None, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                            TouchInteraction.NextEventIsTouch = true;
                            OverlayArea_MouseMove(this, mouseEvent);
                        }
                    }
                    break;
                case WinMsg.WM_POINTERDOWN:
                    {
                        var TouchInteraction = ((ThumbnailView)(this.Owner)).TouchInteraction;
                        if (TouchInteraction.IsDown()) return true;

                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        TouchInteraction.PointerDown = id;

                        if (!TouchInteraction.IsEnter())
                        {
                            TouchInteraction.NextEventIsTouch = true;
                            OverlayArea_MouseEnter(this, EventArgs.Empty);
                        }

                        if (TouchInteraction.PointerEnter != id)
                            TouchInteraction.PointerEnter = id;

                        var p1 = Helpers.GetClientPointFromPointerMsg(this, ref m);
                        MouseEventArgs mouseEvent = new MouseEventArgs(MouseButtons.Left, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                        TouchInteraction.NextEventIsTouch = true;
                        OverlayArea_MouseDown(this, mouseEvent);
                    }
                    break;
                case WinMsg.WM_POINTERUP:
                    {
                        var TouchInteraction = ((ThumbnailView)(this.Owner)).TouchInteraction;
                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        if (TouchInteraction.PointerDown != id) return true;
                        TouchInteraction.PointerDown = uint.MaxValue;

                        var p1 = Helpers.GetClientPointFromPointerMsg(this, ref m);
                        MouseEventArgs mouseEvent = new MouseEventArgs(MouseButtons.Left, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                        TouchInteraction.NextEventIsTouch = true;
                        OverlayArea_MouseUp(this, mouseEvent);

                        if (TouchInteraction.PointerEnter == id)
                        {
                            TouchInteraction.PointerEnter = uint.MaxValue;
                            if (!TouchInteraction.MouseEnter)
                            {
                                TouchInteraction.NextEventIsTouch = true;
                                OverlayArea_MouseLeave(this, EventArgs.Empty);
                            }
                        }
                    }
                    break;
                case WinMsg.WM_POINTERENTER:
                    {
                        var TouchInteraction = ((ThumbnailView)(this.Owner)).TouchInteraction;
                        if (TouchInteraction.IsDown() || TouchInteraction.IsEnter()) return true;
                        TouchInteraction.PointerEnter = Helpers.GetIDFromPointerMsg(ref m);
                        TouchInteraction.NextEventIsTouch = true;
                        OverlayArea_MouseEnter(this, EventArgs.Empty);
                    }
                    break;
                case WinMsg.WM_POINTERLEAVE:
                    {
                        var TouchInteraction = ((ThumbnailView)(this.Owner)).TouchInteraction;
                        if (TouchInteraction.PointerEnter != Helpers.GetIDFromPointerMsg(ref m) || TouchInteraction.MouseEnter) return true;
                        TouchInteraction.NextEventIsTouch = true;
                        OverlayArea_MouseLeave(this, EventArgs.Empty);
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }

        private void InitializeTouch()
        {
            this._hooks = new System.Collections.Generic.List<AddTouchCapability>();
            foreach (Control c in this.Controls)
            {
                var hook = new AddTouchCapability(c, ((ThumbnailView)(this.Owner)).TouchInteraction,
                this._areaMouseEnterAction,
                this._areaMouseLeaveAction,
                this._areaMouseDownAction,
                this._areaMouseUpAction,
                this._areaMouseMoveAction);
                this._hooks.Add(hook);
            }
        }
    }
}