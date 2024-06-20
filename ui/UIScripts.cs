using System.Runtime.InteropServices;
using System.Timers;

namespace iterate.ui
{
    public static class UIScripts
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        //NOT WORKING
        public static async void Lerp(Action<double> action, int time, Action onFinish)
        {
            float progress = 0f;
            float increment = 0.01f;
            int step = 1;
            if (time < 100)
            {
                increment = time / 100;
            }
            else
            {
                step = time / 100;
            }
            System.Timers.Timer timer = new System.Timers.Timer(step);
            timer.Elapsed += (s, e) =>
            {
                progress += increment;
                action(progress);
                if (progress > 0)
                {
                    timer.Close();
                    onFinish();
                }
            };
            timer.Enabled = true;
        }
    }
}
