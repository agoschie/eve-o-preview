using System.Windows.Forms;

namespace EveOPreview.View
{
    partial class ThumbnailView
    {
        //Put in any low level message handling for ThumbnailView
        protected override void WndProc(ref Message m) //add more to this "if statement" if handling touch, example: HandleIfTouchInput(ref m) || TouchProc2(ref m) || TouchProc3(ref m) ...
        {
            if (HandleIfTouchInput(ref m))
                return; //if handled, officially handle the message and return

            base.WndProc(ref m);
        }
    }
}