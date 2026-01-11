using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace ChessDroid.Services
{
    /// <summary>
    /// Handles screen capture and image format conversion
    /// Extracted from MainForm to separate concerns
    /// </summary>
    public class ScreenCaptureService
    {
        /// <summary>
        /// Captures the full primary screen as a Bitmap
        /// </summary>
        public Bitmap? CaptureFullScreen()
        {
            try
            {
                Rectangle screenBounds = Screen.PrimaryScreen!.Bounds;
                Bitmap bmp = new Bitmap(screenBounds.Width, screenBounds.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screen: {ex.Message}",
                    "Screen Capture Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Converts a Bitmap to OpenCV Mat format
        /// Optimized to avoid unnecessary copying when already in correct format
        /// </summary>
        public Mat BitmapToMat(Bitmap bmp)
        {
            // Fast path: if already 24bpp, avoid copy
            if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Mat matView = new Mat(bmp.Height, bmp.Width, DepthType.Cv8U, 3, data.Scan0, data.Stride);
                    return matView.Clone(); // clone to release lock
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
            else
            {
                // Only convert if necessary
                using (Bitmap work = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(work))
                        g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

                    Rectangle rect = new Rectangle(0, 0, work.Width, work.Height);
                    BitmapData data = work.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        Mat matView = new Mat(work.Height, work.Width, DepthType.Cv8U, 3, data.Scan0, data.Stride);
                        return matView.Clone();
                    }
                    finally
                    {
                        work.UnlockBits(data);
                    }
                }
            }
        }

        /// <summary>
        /// Captures screen and converts to Mat in one call
        /// </summary>
        public Mat? CaptureScreenAsMat()
        {
            Bitmap? bmp = CaptureFullScreen();
            if (bmp == null)
                return null;

            try
            {
                return BitmapToMat(bmp);
            }
            finally
            {
                bmp.Dispose();
            }
        }
    }
}
