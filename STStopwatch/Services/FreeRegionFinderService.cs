using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Point = System.Windows.Point;

namespace STStopwatch.Services
{
    public class FreeRegionFinderService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        private const uint GW_HWNDNEXT = 2;

        public const uint WDA_NONE = 0x00000000;
        public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private int[] _prevMotionSamples;
        private int _lastMotionDetectedTick;
        private int _lastMoveTick;
        private Point? _currentParkedPosition;
        private const int MotionSampleGridSize = 8; // 8x8 probe grid
        private const int MovementCooldownMs = 4500; // Stay put for at least 4.5 seconds after finding a spot

        public void NotifyMovedTo(double left, double top)
        {
            _lastMoveTick = Environment.TickCount;
            _currentParkedPosition = new Point(left, top);
        }

        public void ResetCooldown()
        {
            _lastMoveTick = 0;
            _currentParkedPosition = null;
        }

        /// <summary>
        /// Finds the region that is the most free (empty of text, buttons, and clickable controls)
        /// within the active underlying window or screen work area.
        /// </summary>
        public Point FindMostFreeRegion(double widgetWidth, double widgetHeight, IntPtr excludeHwnd)
        {
            try
            {
                RECT searchRect = DetermineSearchRectangle(excludeHwnd);

                int w = searchRect.Width;
                int h = searchRect.Height;

                if (w < widgetWidth + 40 || h < widgetHeight + 40)
                {
                    return GetFallbackParkingPosition(widgetWidth, widgetHeight);
                }

                int captureW = Math.Min(w, 2560);
                int captureH = Math.Min(h, 1600);

                using (var bmp = new Bitmap(captureW, captureH, PixelFormat.Format32bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(searchRect.Left, searchRect.Top, 0, 0, new System.Drawing.Size(captureW, captureH), CopyPixelOperation.SourceCopy);
                    }

                    Point best = EvaluateBestPosition(bmp, searchRect.Left, searchRect.Top, captureW, captureH, (int)widgetWidth, (int)widgetHeight, null);
                    NotifyMovedTo(best.X, best.Y);
                    return best;
                }
            }
            catch
            {
                return GetFallbackParkingPosition(widgetWidth, widgetHeight);
            }
        }

        /// <summary>
        /// Continuously called by background watcher. If the screen is moving (scrolling or video),
        /// or if the widget is already settled in a clean, free area, it returns null (stays put).
        /// Only moves when the screen is settled and the current location is genuinely cluttered.
        /// </summary>
        public Point? CheckAndFindTargetIfCluttered(double currentLeft, double currentTop, double widgetWidth, double widgetHeight, IntPtr excludeHwnd)
        {
            try
            {
                int now = Environment.TickCount;

                // 1. Enforce cooldown: once moved, stay put for at least 4.5 seconds!
                if (_lastMoveTick != 0 && Math.Abs(now - _lastMoveTick) < MovementCooldownMs)
                {
                    return null;
                }

                RECT searchRect = DetermineSearchRectangle(excludeHwnd);
                int w = searchRect.Width;
                int h = searchRect.Height;

                if (w < widgetWidth + 40 || h < widgetHeight + 40)
                {
                    return null;
                }

                int captureW = Math.Min(w, 2560);
                int captureH = Math.Min(h, 1600);

                using (var bmp = new Bitmap(captureW, captureH, PixelFormat.Format32bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(searchRect.Left, searchRect.Top, 0, 0, new System.Drawing.Size(captureW, captureH), CopyPixelOperation.SourceCopy);
                    }

                    // 2. If screen is in motion (scrolling, video, animations), DO NOT MOVE! Stay where it is.
                    if (IsScreenInMotion(bmp, 800))
                    {
                        return null;
                    }

                    int relX = (int)(currentLeft - searchRect.Left);
                    int relY = (int)(currentTop - searchRect.Top);

                    // 3. Inspect the collar immediately surrounding the widget's perimeter.
                    // This inspects the real underlying page/background without being tricked by the stopwatch's own text or numbers!
                    double surroundingClutter = EvaluateSurroundingCollarClutter(bmp, relX, relY, (int)widgetWidth, (int)widgetHeight);

                    // If the area surrounding the widget is clean (score < 120, meaning whitespace/margin with zero text), STAY THERE!
                    if (surroundingClutter < 120.0)
                    {
                        return null;
                    }

                    // 4. Content (text/buttons) has scrolled under the widget. Find the best vacant area!
                    Point bestPoint = EvaluateBestPosition(bmp, searchRect.Left, searchRect.Top, captureW, captureH, (int)widgetWidth, (int)widgetHeight, new Point(currentLeft, currentTop));

                    // Require at least 50px distance to prevent micro-jitter
                    double dist = Math.Sqrt(Math.Pow(bestPoint.X - currentLeft, 2) + Math.Pow(bestPoint.Y - currentTop, 2));
                    if (dist > 50.0)
                    {
                        NotifyMovedTo(bestPoint.X, bestPoint.Y);
                        return bestPoint;
                    }

                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private bool IsScreenInMotion(Bitmap bmp, int quiescentWindowMs = 700)
        {
            int now = Environment.TickCount;
            int w = bmp.Width;
            int h = bmp.Height;

            int[] currentSamples = new int[MotionSampleGridSize * MotionSampleGridSize];
            int stepX = w / (MotionSampleGridSize + 1);
            int stepY = h / (MotionSampleGridSize + 1);

            int index = 0;
            for (int gy = 1; gy <= MotionSampleGridSize; gy++)
            {
                int py = gy * stepY;
                for (int gx = 1; gx <= MotionSampleGridSize; gx++)
                {
                    int px = gx * stepX;
                    var c = bmp.GetPixel(px, py);
                    currentSamples[index++] = (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
                }
            }

            bool motionDetectedNow = false;
            if (_prevMotionSamples != null && _prevMotionSamples.Length == currentSamples.Length)
            {
                int changedCount = 0;
                for (int i = 0; i < currentSamples.Length; i++)
                {
                    if (Math.Abs(currentSamples[i] - _prevMotionSamples[i]) > 14)
                    {
                        changedCount++;
                    }
                }

                // If 3 or more sample points changed significantly, motion is occurring (scrolling, video)
                if (changedCount >= 3)
                {
                    motionDetectedNow = true;
                    _lastMotionDetectedTick = now;
                }
            }

            _prevMotionSamples = currentSamples;

            if (motionDetectedNow)
            {
                return true;
            }

            if (_lastMotionDetectedTick != 0 && Math.Abs(now - _lastMotionDetectedTick) < quiescentWindowMs)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluates clutter on an outer ring (collar) immediately outside the 200px widget (radius 105 to 125).
        /// This accurately detects if the widget is seated in an empty margin or on top of text,
        /// completely ignoring the widget's own internal numbers ("00:00:00") and graphics.
        /// </summary>
        private double EvaluateSurroundingCollarClutter(Bitmap bmp, int relX, int relY, int targetW, int targetH)
        {
            int bmpW = bmp.Width;
            int bmpH = bmp.Height;

            double centerX = relX + (targetW / 2.0);
            double centerY = relY + (targetH / 2.0);
            double collarRadius = (targetW / 2.0) + 14.0; // 14px outside circle

            int edgeCount = 0;
            int sampleCount = 36;
            double lumSum = 0;
            double lumSqSum = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                double angle = (i * 2.0 * Math.PI) / sampleCount;
                int sx = (int)(centerX + collarRadius * Math.Cos(angle));
                int sy = (int)(centerY + collarRadius * Math.Sin(angle));

                if (sx < 2 || sx >= bmpW - 2 || sy < 2 || sy >= bmpH - 2)
                    continue;

                Color c = bmp.GetPixel(sx, sy);
                double lum = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                lumSum += lum;
                lumSqSum += lum * lum;

                // Probe 3px outward for edge gradient
                int ox = (int)(centerX + (collarRadius + 3.0) * Math.Cos(angle));
                int oy = (int)(centerY + (collarRadius + 3.0) * Math.Sin(angle));
                if (ox >= 0 && ox < bmpW && oy >= 0 && oy < bmpH)
                {
                    Color cOut = bmp.GetPixel(ox, oy);
                    double lumOut = 0.299 * cOut.R + 0.587 * cOut.G + 0.114 * cOut.B;
                    if (Math.Abs(lum - lumOut) > 30.0)
                    {
                        edgeCount++;
                    }
                }
            }

            if (sampleCount == 0) return 0;
            double mean = lumSum / sampleCount;
            double variance = Math.Max(0, (lumSqSum / sampleCount) - (mean * mean));

            return (edgeCount * 45.0) + (Math.Sqrt(variance) * 3.0);
        }

        private RECT DetermineSearchRectangle(IntPtr excludeHwnd)
        {
            var workArea = SystemParameters.WorkArea;
            var defaultRect = new RECT
            {
                Left = (int)workArea.Left,
                Top = (int)workArea.Top,
                Right = (int)(workArea.Left + workArea.Width),
                Bottom = (int)(workArea.Top + workArea.Height)
            };

            try
            {
                IntPtr currentHwnd = GetForegroundWindow();

                if (currentHwnd == excludeHwnd || currentHwnd == IntPtr.Zero)
                {
                    IntPtr testHwnd = GetWindow(excludeHwnd, GW_HWNDNEXT);
                    while (testHwnd != IntPtr.Zero)
                    {
                        if (IsWindowVisible(testHwnd) && !IsIconic(testHwnd))
                        {
                            RECT r;
                            GetWindowRect(testHwnd, out r);
                            if (r.Width >= 400 && r.Height >= 300)
                            {
                                var sbClass = new StringBuilder(256);
                                GetClassName(testHwnd, sbClass, 256);
                                string cls = sbClass.ToString();
                                if (cls != "Shell_TrayWnd" && cls != "Progman" && cls != "WorkerW")
                                {
                                    currentHwnd = testHwnd;
                                    break;
                                }
                            }
                        }
                        testHwnd = GetWindow(testHwnd, GW_HWNDNEXT);
                    }
                }

                if (currentHwnd != IntPtr.Zero && currentHwnd != excludeHwnd)
                {
                    RECT winRect;
                    if (GetWindowRect(currentHwnd, out winRect))
                    {
                        int left = Math.Max(defaultRect.Left, winRect.Left);
                        int top = Math.Max(defaultRect.Top, winRect.Top);
                        int right = Math.Min(defaultRect.Right, winRect.Right);
                        int bottom = Math.Min(defaultRect.Bottom, winRect.Bottom);

                        if (right - left >= 300 && bottom - top >= 300)
                        {
                            return new RECT { Left = left, Top = top, Right = right, Bottom = bottom };
                        }
                    }
                }
            }
            catch
            {
            }

            return defaultRect;
        }

        private Point EvaluateBestPosition(Bitmap bmp, int screenOriginX, int screenOriginY, int bmpWidth, int bmpHeight, int targetW, int targetH, Point? currentPos)
        {
            int margin = 16;
            int stepX = Math.Max(24, targetW / 5);
            int stepY = Math.Max(24, targetH / 5);

            int maxPosX = bmpWidth - targetW - margin;
            int maxPosY = bmpHeight - targetH - margin;

            if (maxPosX < margin || maxPosY < margin)
            {
                return new Point(screenOriginX + margin, screenOriginY + margin);
            }

            double lowestClutterScore = double.MaxValue;
            Point bestPos = new Point(screenOriginX + maxPosX, screenOriginY + margin);

            // Keep every automatic placement in a narrow band at the screen/work-area
            // perimeter. In particular, never consider interior rows or columns where
            // the widget could land in the middle of the screen.
            var candidates = new List<System.Drawing.Point>();
            for (int x = margin; x <= maxPosX; x += stepX)
            {
                candidates.Add(new System.Drawing.Point(x, margin));
                candidates.Add(new System.Drawing.Point(x, maxPosY));
            }

            for (int y = margin; y <= maxPosY; y += stepY)
            {
                candidates.Add(new System.Drawing.Point(margin, y));
                candidates.Add(new System.Drawing.Point(maxPosX, y));
            }

            // Include exact far endpoints when the stepping interval does not land on them.
            candidates.Add(new System.Drawing.Point(maxPosX, margin));
            candidates.Add(new System.Drawing.Point(maxPosX, maxPosY));
            candidates.Add(new System.Drawing.Point(margin, maxPosY));

            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmpWidth, bmpHeight),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            try
            {
                int stride = data.Stride;
                IntPtr scan0 = data.Scan0;

                unsafe
                {
                    byte* pBase = (byte*)scan0.ToPointer();

                    foreach (var pt in candidates)
                    {
                        if (pt.X < 0 || pt.X + targetW >= bmpWidth || pt.Y < 0 || pt.Y + targetH >= bmpHeight)
                            continue;

                        // If this candidate heavily overlaps current widget position, skip it to prevent seeing old widget position
                        if (currentPos.HasValue)
                        {
                            double dist = Math.Sqrt(Math.Pow(screenOriginX + pt.X - currentPos.Value.X, 2) + Math.Pow(screenOriginY + pt.Y - currentPos.Value.Y, 2));
                            if (dist < 45.0) continue;
                        }

                        double clutter = ComputeRegionClutterScore(pBase, stride, pt.X, pt.Y, targetW, targetH);

                        double edgeDistanceX = Math.Min(pt.X - margin, maxPosX - pt.X);
                        double edgeDistanceY = Math.Min(pt.Y - margin, maxPosY - pt.Y);
                        double placementCost = (edgeDistanceX + edgeDistanceY) * 0.05;

                        double totalScore = clutter + placementCost;

                        if (totalScore < lowestClutterScore)
                        {
                            lowestClutterScore = totalScore;
                            bestPos = new Point(screenOriginX + pt.X, screenOriginY + pt.Y);
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            return bestPos;
        }

        private unsafe double ComputeRegionClutterScore(byte* pBase, int stride, int startX, int startY, int width, int height)
        {
            int sampleStep = 8;
            int highContrastEdgeCount = 0;
            int totalSamples = 0;
            double luminanceSum = 0;
            double luminanceSqSum = 0;

            for (int y = startY + 4; y < startY + height - 4; y += sampleStep)
            {
                byte* row = pBase + (y * stride);
                byte* nextRow = pBase + ((y + 2) * stride);

                for (int x = startX + 4; x < startX + width - 4; x += sampleStep)
                {
                    int offset = x * 4;
                    int b = row[offset];
                    int g = row[offset + 1];
                    int r = row[offset + 2];
                    double lum = 0.299 * r + 0.587 * g + 0.114 * b;

                    luminanceSum += lum;
                    luminanceSqSum += lum * lum;
                    totalSamples++;

                    int nextOffset = (x + 2) * 4;
                    int bNext = row[nextOffset];
                    int gNext = row[nextOffset + 1];
                    int rNext = row[nextOffset + 2];
                    double lumNext = 0.299 * rNext + 0.587 * gNext + 0.114 * bNext;

                    int bDown = nextRow[offset];
                    int gDown = nextRow[offset + 1];
                    int rDown = nextRow[offset + 2];
                    double lumDown = 0.299 * rDown + 0.587 * gDown + 0.114 * bDown;

                    double edgeGrad = Math.Abs(lum - lumNext) + Math.Abs(lum - lumDown);

                    if (edgeGrad > 32.0)
                    {
                        highContrastEdgeCount++;
                    }
                }
            }

            if (totalSamples == 0) return 0;

            double mean = luminanceSum / totalSamples;
            double variance = Math.Max(0, (luminanceSqSum / totalSamples) - (mean * mean));

            double edgePenalty = highContrastEdgeCount * 80.0;
            double variancePenalty = Math.Sqrt(variance) * 4.0;

            return edgePenalty + variancePenalty;
        }

        private Point GetFallbackParkingPosition(double widgetWidth, double widgetHeight)
        {
            var workArea = SystemParameters.WorkArea;
            double left = workArea.Left + workArea.Width - widgetWidth - 24;
            double top = workArea.Top + 24;
            return new Point(left, top);
        }
    }
}
