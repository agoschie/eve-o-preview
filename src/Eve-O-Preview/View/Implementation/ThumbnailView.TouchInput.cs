using System;
using System.CodeDom;
using System.Configuration;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Policy;
using System.Windows.Forms;

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
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_MOUSELEAVE = 0x02A3;
}

file static class Helpers
{
    public static Point GetClientPointFromPointerMsg(EveOPreview.View.ThumbnailView _this, ref Message m)
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
    partial class ThumbnailView
    {
        public SingleTouchInteraction TouchInteraction { get; } = new SingleTouchInteraction();

        private bool HandleIfTouchInput(ref Message m)
        {

            switch (m.Msg)
            {
                case WinMsg.WM_POINTERUPDATE:
                    {
                        if (TouchInteraction.MouseDown || (TouchInteraction.MouseEnter && !TouchInteraction.IsPointerDown())) return true;
                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        if (TouchInteraction.PointerDown == id || (!TouchInteraction.IsPointerDown() && TouchInteraction.PointerEnter == id))
                        {
                            var p1 = Helpers.GetClientPointFromPointerMsg(this, ref m);
                            MouseEventArgs mouseEvent = new MouseEventArgs((TouchInteraction.IsPointerDown()) ? MouseButtons.Left : MouseButtons.None, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                            TouchInteraction.NextEventIsTouch = true;
                            MouseMove_Handler(this, mouseEvent);
                        }
                    }
                    break;
                case WinMsg.WM_POINTERDOWN:
                    {
                        if (TouchInteraction.IsDown()) return true;

                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        TouchInteraction.PointerDown = id;

                        if (!TouchInteraction.IsEnter())
                        {
                            TouchInteraction.NextEventIsTouch = true;
                            MouseEnter_Handler(this, EventArgs.Empty);
                        }

                        if (TouchInteraction.PointerEnter != id)
                            TouchInteraction.PointerEnter = id;

                        var p1 = Helpers.GetClientPointFromPointerMsg(this, ref m);
                        MouseEventArgs mouseEvent = new MouseEventArgs(MouseButtons.Left, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                        TouchInteraction.NextEventIsTouch = true;
                        MouseDown_Handler(this, mouseEvent);
                    }
                    break;
                case WinMsg.WM_POINTERUP:
                    {
                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        if (TouchInteraction.PointerDown != id) return true;
                        TouchInteraction.PointerDown = uint.MaxValue;

                        var p1 = Helpers.GetClientPointFromPointerMsg(this, ref m);
                        MouseEventArgs mouseEvent = new MouseEventArgs(MouseButtons.Left, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                        TouchInteraction.NextEventIsTouch = true;
                        MouseUp_Handler(this, mouseEvent);

                        if (TouchInteraction.PointerEnter == id)
                        {
                            TouchInteraction.PointerEnter = uint.MaxValue;
                            if (!TouchInteraction.MouseEnter)
                            {
                                TouchInteraction.NextEventIsTouch = true;
                                MouseLeave_Handler(this, EventArgs.Empty);
                            }
                        }
                    }
                    break;
                case WinMsg.WM_POINTERENTER:
                    if (TouchInteraction.IsDown() || TouchInteraction.IsEnter()) return true;
                    TouchInteraction.PointerEnter = Helpers.GetIDFromPointerMsg(ref m);
                    TouchInteraction.NextEventIsTouch = true;
                    MouseEnter_Handler(this, EventArgs.Empty);
                    break;
                case WinMsg.WM_POINTERLEAVE:
                    if (TouchInteraction.PointerEnter != Helpers.GetIDFromPointerMsg(ref m) || TouchInteraction.MouseEnter) return true;
                    TouchInteraction.NextEventIsTouch = true;
                    MouseLeave_Handler(this, EventArgs.Empty);
                    break;
            
                default:
                    return false;
            }

            return true;
        }

        private void InitializeTouch()
        {
        }
    }
}