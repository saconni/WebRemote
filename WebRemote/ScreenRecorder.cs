using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace WebRemote
{
    public class Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public int X;
        public int Y;
    }

    public class Region
    {
        public int X1 = 0;
        public int Y1 = 0;
        public int X2 = 0;
        public int Y2 = 0;

        public int Width { get => X2 - X1; }
        public int Height { get => Y2 - Y1; }

        public int Tolerance = 100;

        public Region(Point p)
        {
            X1 = p.X - Tolerance;
            Y1 = p.Y - Tolerance;
            X2 = p.X + Tolerance;
            Y2 = p.Y + Tolerance;
        }

        public Region(int x, int y, int width, int height)
        {
            X1 = x;
            Y1 = y;
            X2 = x + width - 1;
            Y2 = y + height - 1;
        }

        public bool Contains(Point p)
        {
            return p.X >= (X1 - Tolerance) && p.X <= (X2 + Tolerance)
                && p.Y >= (Y1 - Tolerance) && p.Y <= (Y2 + Tolerance);
        }

        public bool Contains(Region c)
        {
            return Contains(new Point(c.X1, c.Y1))
                && Contains(new Point(c.X2, c.Y2));
        }

        public void AddPoint(Point p)
        {
            X1 = Math.Min(X1, p.X);
            Y1 = Math.Min(Y1, p.Y);
            X2 = Math.Max(X2, p.X);
            Y2 = Math.Max(Y2, p.Y);
        }
    }

    public class Frame
    {
        static int CurrentNumber = 0;

        public int Number { get; private set; } = ++CurrentNumber;
        public Bitmap Image { get; set; } = null;
        public List<Region> Regions { get; set; } = new List<Region>();

        ~Frame()
        {
            if (Image != null)
                Image.Dispose();
        }
    }

    public class ScreenRecorder
    {
        private System.Timers.Timer _refreshTimer;
        private Bitmap _lastCapture = null;
        private List<Frame> _frames = new List<Frame>();

        public List<Frame> Frames { get => _frames; }

        public ScreenRecorder()
        {
            _refreshTimer = new System.Timers.Timer(100);
            _refreshTimer.AutoReset = false;
            _refreshTimer.Elapsed += (who, args) => Refresh();

            _lastCapture = new Bitmap(OutputWidth, OutputHeight);

            CreateKeyFrame();
        }

        public void Start()
        {
            _refreshTimer.Start();
        }

        public void Refresh()
        {
            CreateFrame();
            _refreshTimer.Start();
        }

        public int ScreenWidth { get => Screen.PrimaryScreen.Bounds.Width; }
        public int ScreenHeight { get => Screen.PrimaryScreen.Bounds.Height; }
        public int OutputWidth { get => 1280; }
        public int OutputHeight { get => 720; }

        public void CreateKeyFrame()
        {
            Frame frame = new Frame();
            frame.Image = CaptureScreen();
            frame.Regions.Add(new Region(0, 0, OutputWidth, OutputHeight));
            AppendFrame(frame);
        }

        public void CreateFrame()
        {
            var newCapture = CaptureScreen();

            Frame frame = new Frame();
            frame.Regions = CreateUpdatedRegions(newCapture, _lastCapture);
            frame.Image = newCapture;

            _lastCapture = new Bitmap(newCapture);

            AppendFrame(frame);

            /*
            int j = 0;
            foreach (var c in regions)
            {
                using (var bmp = new Bitmap(c.Width, c.Height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    j++;
                    g.DrawImage(newCapture, new Rectangle(0, 0, bmp.Width, bmp.Height),
                        new Rectangle(c.X1, c.Y1, c.Width, c.Height), GraphicsUnit.Pixel);
                    bmp.Save($"d:\\image{j}.png");
                }
            }
            */
        }

        private void AppendFrame(Frame frame)
        {
            var newFrames = _frames.Skip(Math.Max(0, _frames.Count() - 9)).ToList();
            newFrames.Add(frame);
            _frames = newFrames;
        }

        public Bitmap CaptureScreen()
        {
            using (Bitmap capture = new Bitmap(ScreenWidth, ScreenHeight))
            {
                using (Graphics g = Graphics.FromImage(capture))
                using (MemoryStream ms = new MemoryStream())
                {
                    g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
                }

                Bitmap output = ScaleImage(capture, OutputWidth, OutputHeight);
                return output;
            }
        }

        public static Bitmap ScaleImage(Bitmap image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        public void NormalizeRegions(List<Region> clusters)
        {
            List<Region> redundant = new List<Region>();

            foreach (var c in clusters)
            {
                if (c.X1 < 0) c.X1 = 0;
                if (c.Y1 < 0) c.Y1 = 0;
                if (c.X2 > OutputWidth) c.X2 = OutputWidth - 1;
                if (c.Y2 > OutputHeight) c.Y2 = OutputHeight - 1;
                foreach (var c1 in clusters)
                {
                    if (c1 != c && c.Contains(c1))
                    {
                        redundant.Add(c1);
                    }
                }
            }

            foreach (var c in redundant)
            {
                clusters.Remove(c);
            }
        }

        public unsafe List<Region> CreateUpdatedRegions(Bitmap image1, Bitmap image2)
        {
            if (image1.Height != image2.Height || image1.Width != image2.Width)
                throw new Exception("images are not the same size");

            List<Region> regions = new List<Region>();

            int width = image1.Width;
            int height = image1.Height;

            // create an image to store the diff pixels
            var diffImage = new Bitmap(width, height);

            BitmapData data1 = image1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData data2 = image2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData diffData = diffImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* data1Ptr = (byte*)data1.Scan0;
            byte* data2Ptr = (byte*)data2.Scan0;
            byte* diffPtr = (byte*)diffData.Scan0;

            // iterate over height (rows)
            for (int i = 0; i < height; i++)
            {
                // iterate over width (columns)
                for (int j = 0; j < width; j++)
                {
                    bool changed = false;

                    // for each channel
                    for (int x = 0; x < 3; x++)
                    {
                        diffPtr[0] = (byte)Math.Abs(data1Ptr[0] - data2Ptr[0]);
                        if (diffPtr[0] > 0) changed = true;
                        data1Ptr++; // advance image1 ptr
                        data2Ptr++; // advance image2 ptr
                        diffPtr++; // advance diff image ptr
                    }

                    // if the pixels are different
                    if (changed)
                    {
                        // look for an existing region...
                        Point p = new Point(j, i);
                        bool alreadyInCluster = false;
                        foreach (var c in regions)
                        {
                            if (c.Contains(p))
                            {
                                alreadyInCluster = true;
                                c.AddPoint(p);
                                break;
                            }
                        }
                        //... or create a new one
                        if (!alreadyInCluster)
                        {
                            regions.Add(new Region(p));
                        }
                    }
                }
            }

            image1.UnlockBits(data1);
            image2.UnlockBits(data2);
            diffImage.UnlockBits(diffData);

            // diffImage.Save(@"d:\diff.png");
            diffImage.Dispose();

            NormalizeRegions(regions);

            /*
            using (Bitmap clustersImage = CreateEmptyBitmap())
            using (Graphics g = Graphics.FromImage(clustersImage))
            {
                // normalize and draw the clusters
                foreach (var c in clusters)
                {
                    g.DrawRectangle(new Pen(Color.FromArgb(255, 0, 0, 0)), new Rectangle(c.X1, c.Y1, c.Width, c.Height));
                }

                clustersImage.Save(@"d:\clusters.png");
            }
            */

            return regions;
        }
    }
}
