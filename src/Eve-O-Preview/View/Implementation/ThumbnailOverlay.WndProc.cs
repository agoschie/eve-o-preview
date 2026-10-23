using System.Windows.Forms;

namespace EveOPreview.View
{
    partial class ThumbnailOverlay
    {
        //Put in any low level message handling for ThumbnailOverlay
        protected override void WndProc(ref Message m)
        {
            if (HandleIfTouchInput(ref m)) //add more to this "if statement" if handling touch, example: HandleIfTouchInput(ref m) || TouchProc2(ref m) || TouchProc3(ref m) ...
                return; //if handled, officially handle the message and return

            base.WndProc(ref m);
        }
    }
}