using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace ChessDroid.Services
{
    /// <summary>
    /// Handles chess board detection from screen captures using computer vision
    /// Extracted from MainForm to separate board detection concerns
    /// </summary>
    public class BoardDetectionService
    {
        private Rectangle? lastBoardRectCached = null;
        private DateTime lastBoardRectCachedAt = DateTime.MinValue;
        private const int CACHE_VALIDITY_SECONDS = 3;

        /// <summary>
        /// Detects board with caching and fallback strategies
        /// Returns both the detected board Mat and its rectangle in the original image
        /// </summary>
        public (Mat? boardMat, Rectangle boardRect) DetectBoardWithRectangle(Mat fullMat)
        {
            // 1) Fast path: if we had a recent boardRect, crop directly (without contours)
            if (lastBoardRectCached.HasValue &&
                (DateTime.Now - lastBoardRectCachedAt).TotalSeconds < CACHE_VALIDITY_SECONDS)
            {
                var quick = CaptureFixedRectangle(fullMat, lastBoardRectCached.Value);
                if (quick != null)
                {
                    // Refresh timestamp to keep it alive
                    lastBoardRectCachedAt = DateTime.Now;
                    return (quick, lastBoardRectCached.Value);
                }
                // If crop failed for some reason, continue to normal detection
            }

            // 2) Try to detect board automatically
            Mat? detectedBoard = DetectBoard(fullMat);
            if (detectedBoard != null)
            {
                using Mat gray = new Mat();
                using Mat canny = new Mat();

                CvInvoke.CvtColor(fullMat, gray, ColorConversion.Bgr2Gray);
                CvInvoke.GaussianBlur(gray, gray, new Size(5, 5), 0);
                CvInvoke.Canny(gray, canny, 50, 150);

                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(canny, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                    double maxArea = 0;
                    Rectangle boardRect = Rectangle.Empty;
                    Point[]? bestContour = null;

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double area = CvInvoke.ContourArea(contour);
                            if (area < 5000) continue;

                            using (VectorOfPoint approx = new VectorOfPoint())
                            {
                                CvInvoke.ApproxPolyDP(contour, approx, 0.02 * CvInvoke.ArcLength(contour, true), true);
                                if (approx.Size == 4)
                                {
                                    Rectangle rect = CvInvoke.BoundingRectangle(approx);
                                    double aspectRatio = (double)rect.Width / rect.Height;

                                    if (aspectRatio > 0.8 && aspectRatio < 1.2 && area > maxArea)
                                    {
                                        maxArea = area;
                                        boardRect = rect;
                                        bestContour = approx.ToArray();
                                    }
                                }
                            }
                        }
                    }

                    // 2.1) If we found valid rectangle: padding + cache + return
                    if (maxArea > 0 && boardRect != Rectangle.Empty)
                    {
                        int padding = Math.Min(100, Math.Min(fullMat.Width, fullMat.Height) / 10);
                        int nx = Math.Max(0, boardRect.X - padding);
                        int ny = Math.Max(0, boardRect.Y - padding);
                        int nW = Math.Min(fullMat.Width - nx, boardRect.Width + padding * 2);
                        int nH = Math.Min(fullMat.Height - ny, boardRect.Height + padding * 2);
                        boardRect = new Rectangle(nx, ny, nW, nH);

                        // Save cache
                        lastBoardRectCached = boardRect;
                        lastBoardRectCachedAt = DateTime.Now;

                        return (detectedBoard, boardRect);
                    }
                }
            }

            // 3) Fallback: centered proportional square + cache
            int side = Math.Min(fullMat.Width, fullMat.Height) * 8 / 10;
            side = Math.Clamp(side, 600, Math.Min(fullMat.Width, fullMat.Height));

            int x = (fullMat.Width - side) / 2;
            int y = (fullMat.Height - side) / 2;

            Rectangle fallbackRect = new Rectangle(x, y, side, side);
            Mat? fallbackBoard = CaptureFixedRectangle(fullMat, fallbackRect);

            // Cache the fallback so next click is instant
            if (fallbackBoard != null)
            {
                lastBoardRectCached = fallbackRect;
                lastBoardRectCachedAt = DateTime.Now;
            }

            return (fallbackBoard, fallbackRect);
        }

        /// <summary>
        /// Detects chess board using contour detection with multiple passes
        /// Returns perspective-corrected board image
        /// </summary>
        public Mat? DetectBoard(Mat fullMat)
        {
            // Try detection with multiple parameter sets (tight -> loose -> very loose)
            for (int pass = 0; pass < 3; pass++)
            {
                Mat gray = new Mat();
                CvInvoke.CvtColor(fullMat, gray, ColorConversion.Bgr2Gray);
                CvInvoke.GaussianBlur(gray, gray, new Size(5, 5), 0);
                Mat canny = new Mat();
                CvInvoke.Canny(gray, canny, 50, 150);

                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(canny, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                    double maxArea = 0;
                    Point[]? boardContour = null;

                    // Parameter tuning per pass: tight -> loose -> very loose
                    double minAreaThreshold;
                    double approxEpsFactor;
                    double aspectMin;
                    double aspectMax;

                    if (pass == 0)
                    {
                        minAreaThreshold = 5000;
                        approxEpsFactor = 0.02;
                        aspectMin = 0.8;
                        aspectMax = 1.2;
                    }
                    else if (pass == 1)
                    {
                        minAreaThreshold = 1500;
                        approxEpsFactor = 0.04;
                        aspectMin = 0.7;
                        aspectMax = 1.3;
                    }
                    else
                    {
                        // Very loose: allow relatively large contours and sloppier approximation
                        // Use image-relative min area so very large boards are included
                        minAreaThreshold = Math.Max(1000, (fullMat.Width * fullMat.Height) * 0.001); // ~0.1% of image area
                        approxEpsFactor = 0.06;
                        aspectMin = 0.6;
                        aspectMax = 1.5;
                    }

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double area = CvInvoke.ContourArea(contour);
                            if (area < minAreaThreshold)
                                continue;

                            VectorOfPoint approx = new VectorOfPoint();
                            CvInvoke.ApproxPolyDP(contour, approx, approxEpsFactor * CvInvoke.ArcLength(contour, true), true);

                            // Accept contours that approximate to 4+ points
                            if (approx.Size >= 4)
                            {
                                Rectangle rect = CvInvoke.BoundingRectangle(approx);
                                double aspectRatio = (double)rect.Width / rect.Height;
                                if (aspectRatio > aspectMin && aspectRatio < aspectMax && area > maxArea)
                                {
                                    maxArea = area;
                                    // Use rectangle corners as a robust fallback for warped boards
                                    boardContour = new Point[] {
                                        new Point(rect.X, rect.Y),
                                        new Point(rect.X + rect.Width, rect.Y),
                                        new Point(rect.X + rect.Width, rect.Y + rect.Height),
                                        new Point(rect.X, rect.Y + rect.Height)
                                    };
                                }
                            }
                        }
                    }

                    if (boardContour != null)
                    {
                        PointF[] srcPoints = ReorderPoints(boardContour);

                        // Calculate detected board size dynamically
                        Rectangle boundingRect = CvInvoke.BoundingRectangle(new VectorOfPoint(boardContour));
                        int detectedSize = Math.Max(boundingRect.Width, boundingRect.Height);

                        // Keep board at detected size for better quality
                        PointF[] dstPoints = new PointF[]
                        {
                            new PointF(0, 0),
                            new PointF(detectedSize, 0),
                            new PointF(detectedSize, detectedSize),
                            new PointF(0, detectedSize)
                        };

                        Mat transform = CvInvoke.GetPerspectiveTransform(srcPoints, dstPoints);
                        Mat boardRectificado = new Mat();
                        CvInvoke.WarpPerspective(fullMat, boardRectificado, transform, new Size(detectedSize, detectedSize));
                        return boardRectificado;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Crops a fixed rectangle from the full image
        /// </summary>
        public Mat? CaptureFixedRectangle(Mat fullMat, Rectangle rect)
        {
            if (rect.X < 0 || rect.Y < 0 ||
                rect.X + rect.Width > fullMat.Width ||
                rect.Y + rect.Height > fullMat.Height)
                return null;

            // Return board at native size - no forced resizing
            using (Mat boardMat = new Mat(fullMat, rect))
            {
                return boardMat.Clone();
            }
        }

        /// <summary>
        /// Reorders points to standard order: top-left, top-right, bottom-right, bottom-left
        /// </summary>
        private static PointF[] ReorderPoints(Point[] pts)
        {
            PointF[] ordered = new PointF[4];
            var sum = pts.Select(p => p.X + p.Y).ToArray();
            var diff = pts.Select(p => p.Y - p.X).ToArray();
            ordered[0] = pts[Array.IndexOf(sum, sum.Min())];      // top-left
            ordered[2] = pts[Array.IndexOf(sum, sum.Max())];      // bottom-right
            ordered[1] = pts[Array.IndexOf(diff, diff.Min())];    // top-right
            ordered[3] = pts[Array.IndexOf(diff, diff.Max())];    // bottom-left
            return ordered;
        }

        /// <summary>
        /// Clears the cached board rectangle (useful when screen layout changes)
        /// </summary>
        public void ClearCache()
        {
            lastBoardRectCached = null;
            lastBoardRectCachedAt = DateTime.MinValue;
        }
    }
}