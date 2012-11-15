using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoStereoCalibration
{
    public partial class Calibration : Form
    {
        float ViewerDistance = 150.0f;
        float MicroLensFocalPoint = 2.792992f;
        float MicroLensMagnification;

        int Width = 1692;
        int Height = 1062;
        const int NumLenses = 7;
        
        float Angle = 2.845976f;  //2.385246f;
        float PitchLens = 0.015f;  //0.15f;
        float PitchPixel = 0.0530421509f;

        const float AngleDelta = 0.001f;
        const float PitchDelta = 0.01f;

        const float MicroLensFocalPointRange = 8.0f;
        const float PitchLensRange = 10.0f;
        const float AngleRange = (float)Math.PI * 2.1f;
        const float AngleStart = 0.0f; //2.38f;

        public Calibration()
        {
            InitializeComponent();

            Width = this.pictureBox.Width;
            Height = this.pictureBox.Height;

            trackBar1.SmallChange = 1;
            trackBar1.Minimum = 0;
            trackBar1.Maximum = int.MaxValue;
            trackBar2.Minimum = 0;
            trackBar2.Maximum = int.MaxValue;
            trackBar3.Minimum = 0;
            trackBar3.Maximum = int.MaxValue;

            trackBar1.Value = (int)(MicroLensFocalPoint / MicroLensFocalPointRange * int.MaxValue);
            trackBar2.Value = (int)(PitchLens / PitchLensRange * int.MaxValue);
            trackBar3.Value = (int)((Angle - AngleStart) / AngleRange * int.MaxValue);
        }

        private void Calibration_Load(object sender, EventArgs e)
        {
            for (var i = 0; i < _images.Length; ++i)
            {
                _images[i] = new Bitmap("Images\\sample" + (i + 1) + ".bmp");
                _unmanagedImages[i] = new UnmanagedBitmap(_images[i]);
            }

            UpdateVariables();
            UpdateLabel();

            this.Focus();
        }

        private readonly Bitmap[] _images = new Bitmap[8];
        private readonly UnmanagedBitmap[] _unmanagedImages = new UnmanagedBitmap[8];

        private void Redraw()
        {
            var unsafeBitmap = new UnmanagedBitmap(Width, Height);

            for (var px = 0; px < Width; ++px)
            {
                Parallel.For(0, Height, py =>
                {
                    float projection = (MicroLensMagnification + 1.0f) / MicroLensMagnification * PitchLens / (float)Math.Cos(Angle);

                    float viewsPerLens = projection / PitchPixel;

                    PixelData p = new PixelData();

                    for (int component = 0; component < 3; ++component)
                    {
                        int k = (int)px;
                        int l = (int)py + component;

                        float xoffset = (k - (l * (float)Math.Tan(Angle))) % projection;

                        int pixelOffset = (int)Math.Round(xoffset / PitchPixel);

                        if (!_offsets.ContainsKey(pixelOffset))
                            _offsets.Add(pixelOffset, 0);

                        _offsets[pixelOffset] = _offsets[pixelOffset] + 1;

                        int viewIndex = (int)Math.Round(((k + xoffset - (3 * l * (float)Math.Tan(Angle))) % viewsPerLens) / viewsPerLens * NumLenses);

                        viewIndex = viewIndex < 0 ? -viewIndex : viewIndex;

                        int xread = px + pixelOffset;

                        xread = Math.Max(xread, 0);
                        xread = Math.Min(xread, Width - 1);

                        var pixel = _unmanagedImages[viewIndex].GetPixel(xread, py);
                        
                        _counts[viewIndex]++;

                        switch (component)
                        {
                            case 0: p.R   = pixel.R; break;
                            case 1: p.G = pixel.G; break;
                            case 2: p.B  = pixel.B; break;
                        }
                    }

                    unsafeBitmap.SetPixel(px, py, p);
                });
            }

            this.pictureBox.Image = unsafeBitmap.SnapShot();

            unsafeBitmap.Dispose();
        }

        private int[] _counts = new int[8];
        private Dictionary<int, int> _offsets = new Dictionary<int, int>();

        private void pictureBox_Click(object sender, EventArgs e)
        {
        }

        public void UpdateVariables()
        {
            MicroLensMagnification = (MicroLensFocalPoint * ViewerDistance) - 1;

            Redraw();

            this.pictureBox.Refresh();
        }

        public void UpdateLabel()
        {
            label1.Text = "MicroLensFocalPoint: " + MicroLensFocalPoint.ToString();
            label2.Text = "PitchLens: " + PitchLens.ToString();
            label3.Text = "Angle: " + Angle.ToString();
        }

        private void trackbar_update(object sender, MouseEventArgs e)
        {
            UpdateVariables();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (!isKeyboard)
            {
                MicroLensFocalPoint = ((float)trackBar1.Value / (int.MaxValue - 1)) * MicroLensFocalPointRange;
                UpdateImage();
            }

            isKeyboard = false;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (!isKeyboard)
            {
                PitchLens = ((float)trackBar2.Value / (int.MaxValue - 1)) * PitchLensRange;
                UpdateImage();
            }

            isKeyboard = false;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            if (!isKeyboard)
            {
                Angle = AngleStart + (((float)trackBar3.Value / (int.MaxValue - 1)) * AngleRange);
                UpdateImage();
            }

            isKeyboard = false;
        }

        private void Calibration_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    loop = !loop;
                    Process();
                    System.Media.SystemSounds.Beep.Play();
                    break;
                case Keys.Tab:
                case Keys.Up:
                    Screenshot();
                    isKeyboard = true;
                    System.Media.SystemSounds.Exclamation.Play();
                    break;
                case Keys.Left:
                    Backward();
                    isKeyboard = true;
                    UpdateImage();
                    break;
                case Keys.Right:
                    Forward();
                    isKeyboard = true;
                    UpdateImage();
                    break;
            }
        }

        private void Forward()
        {
            Angle += AngleDelta;

            if (Angle > AngleRange)
            {
                Angle = 0.0f;

                PitchLens += PitchDelta;
            }
        }

        private void Backward()
        {
            Angle -= AngleDelta;

            if (Angle < AngleStart)
            {
                Angle = AngleRange;

                PitchLens -= PitchDelta;
            }
        }

        private void Process()
        {
            while (loop)
            {
                Forward();

                UpdateImage();

                Application.DoEvents();
            }
        }

        private void Screenshot()
        {
            lock (this)
            {
                this.pictureBox.Image.Save("Image" + fileNumber.ToString("000") + ".bmp", ImageFormat.Bmp);

                var writer = new StreamWriter("Params" + fileNumber.ToString("000") + ".txt");

                writer.WriteLine("Angle: " + Angle);
                writer.WriteLine("LensPitch: " + PitchLens);
                writer.WriteLine("LensFocalPoint: " + MicroLensFocalPoint);
                writer.WriteLine("ViewerDistance: " + ViewerDistance);
                writer.WriteLine("NumLenses: " + NumLenses);

                writer.Close();

                fileNumber++;
            }
        }

        private void UpdateImage()
        {
            UpdateLabel();

            UpdateVariables();

            Refresh();
        }

        private bool loop;
        private int fileNumber;

        private bool isKeyboard;
    }
}
