using System;
using System.CodeDom;
using System.Configuration;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.RightsManagement;
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
}

file static class Helpers
{
    public static Point GetClientPointFromPointerMsg(Control _this, ref Message m)
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
    public class SingleTouchInteraction
    {
        public uint PointerDown { get; set; }
        public uint PointerEnter { get; set; }
        public bool MouseDown { get; set; }
        public bool MouseEnter { get; set; }
        public bool NextEventIsTouch { get; set; }

        public SingleTouchInteraction()
        {
            this.PointerDown = uint.MaxValue;
            this.PointerEnter = uint.MaxValue;
            this.MouseDown = false;
            this.MouseEnter = false;
        }

        public bool IsPointerDown()
        {
            return PointerDown != 0xffffffff;
        }

        public bool IsPointerEnter()
        {
            return PointerEnter != 0xffffffff;
        }

        public bool IsDown()
        {
            return IsPointerDown() || MouseDown;
        }

        public bool IsEnter()
        {
            return IsPointerEnter() || MouseEnter;
        }
    }
    public class AddTouchCapability : NativeWindow
    {
        private SingleTouchInteraction TouchInteraction { get; set; }
        private readonly Control _form;
        private readonly Action<object, EventArgs> _areaMouseEnterAction;
        private readonly Action<object, EventArgs> _areaMouseLeaveAction;
        private readonly Action<object, MouseEventArgs> _areaMouseDownAction;
        private readonly Action<object, MouseEventArgs> _areaMouseUpAction;
        private readonly Action<object, MouseEventArgs> _areaMouseMoveAction;

        public AddTouchCapability(Control form, SingleTouchInteraction touchInteraction,
                Action<object, EventArgs> areaMouseEnterAction,
                Action<object, EventArgs> areaMouseLeaveAction,
                Action<object, MouseEventArgs> areaMouseDownAction,
                Action<object, MouseEventArgs> areaMouseUpAction,
                Action<object, MouseEventArgs> areaMouseMoveAction)
        {
            _form = form;
            TouchInteraction = touchInteraction;
            _areaMouseEnterAction = areaMouseEnterAction;
            _areaMouseLeaveAction = areaMouseLeaveAction;
            _areaMouseDownAction = areaMouseDownAction;
            _areaMouseUpAction = areaMouseUpAction;
            _areaMouseMoveAction = areaMouseMoveAction;

            // Hook when handle is ready or already created
            if (form.IsHandleCreated)
            {
                AssignHandle(form.Handle);
            }

            form.HandleCreated += (s, e) => AssignHandle(((Control)s).Handle);
            form.HandleDestroyed += (s, e) => ReleaseHandle();
        }

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
                            var p1 = Helpers.GetClientPointFromPointerMsg(_form, ref m);
                            MouseEventArgs mouseEvent = new MouseEventArgs((TouchInteraction.IsPointerDown()) ? MouseButtons.Left : MouseButtons.None, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                            TouchInteraction.NextEventIsTouch = true;
                            _areaMouseMoveAction(_form, mouseEvent);
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
                            _areaMouseEnterAction(_form, EventArgs.Empty);
                        }

                        if (TouchInteraction.PointerEnter != id)
                            TouchInteraction.PointerEnter = id;

                        var p1 = Helpers.GetClientPointFromPointerMsg(_form, ref m);
                        MouseEventArgs mouseEvent = new MouseEventArgs(MouseButtons.Left, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                        TouchInteraction.NextEventIsTouch = true;
                        _areaMouseDownAction(_form, mouseEvent);
                    }
                    break;
                case WinMsg.WM_POINTERUP:
                    {
                        uint id = Helpers.GetIDFromPointerMsg(ref m);
                        if (TouchInteraction.PointerDown != id) return true;
                        TouchInteraction.PointerDown = uint.MaxValue;

                        var p1 = Helpers.GetClientPointFromPointerMsg(_form, ref m);
                        MouseEventArgs mouseEvent = new MouseEventArgs(MouseButtons.Left, 0, p1.X, p1.Y, 0); //should not be creating these too frequently
                        TouchInteraction.NextEventIsTouch = true;
                        _areaMouseUpAction(_form, mouseEvent);

                        if (TouchInteraction.PointerEnter == id)
                        {
                            TouchInteraction.PointerEnter = uint.MaxValue;
                            if (!TouchInteraction.MouseEnter)
                            {
                                TouchInteraction.NextEventIsTouch = true;
                                _areaMouseLeaveAction(_form, EventArgs.Empty);
                            }
                        }
                    }
                    break;
                case WinMsg.WM_POINTERENTER:
                    if (TouchInteraction.IsDown() || TouchInteraction.IsEnter()) return true;
                    TouchInteraction.PointerEnter = Helpers.GetIDFromPointerMsg(ref m);
                    TouchInteraction.NextEventIsTouch = true;
                    _areaMouseEnterAction(_form, EventArgs.Empty);
                    break;
                case WinMsg.WM_POINTERLEAVE:
                    if (TouchInteraction.PointerEnter != Helpers.GetIDFromPointerMsg(ref m) || TouchInteraction.MouseEnter) return true;
                    TouchInteraction.NextEventIsTouch = true;
                    _areaMouseLeaveAction(_form, EventArgs.Empty);
                    break;

                default:
                    return false;
            }

            return true;
        }

        protected override void WndProc(ref Message m)
        {
            if (HandleIfTouchInput(ref m)) //add more to this "if statement" if handling touch, example: HandleIfTouchInput(ref m) || TouchProc2(ref m) || TouchProc3(ref m) ...
                return; //if handled, officially handle the message and return

            base.WndProc(ref m);
        }
    }
}