using OpenCvSharp;
using OpenCvSharp.Internal.Util;

void FromVideo(string s, int batch1, int skip1, string outputFrames1, string outputPath1, string imageOutput1)
{
    {
        var cap = new VideoCapture(s);

        List<Task<Mat>> frames = [];
        Dictionary<int, Mat> rawFrames = [];


        for (var i = 0; i < cap.FrameCount / batch1; i++)
        {
            Console.WriteLine($"Batch {i} {i * batch1}/{cap.FrameCount}");
            LoadFrames(batch1, i * batch1 + skip1, ref rawFrames, cap);
            Console.WriteLine(" > Executing");
            frames.Clear();
            foreach (var (index, frame) in rawFrames)
            {
                int i0 = index;
                var task = new Task<Mat>(() => DoMagic(frame, i0, outputFrames1));
                task.Start();
                frames.Add(task);
            }

            Console.WriteLine(" | Writing");
            VideoWriter writer = new VideoWriter(outputPath1 + $"/batch{i:000}.mp4", FourCC.Default, cap.Fps,
                new Size(cap.FrameWidth, cap.FrameHeight));
            foreach (var frame in frames) writer.Write(frame.Result);
            writer.Dispose();
        }
    }

    bool LoadFrames(int count, int skip, ref Dictionary<int, Mat> buffer, VideoCapture source)
    {
        source.Set(VideoCaptureProperties.PosFrames, skip);
        buffer.Clear();
        for (int i = skip; i < skip + count; i++)
        {
            Mat frame = new();
            if (!source.Read(frame)) return false;
            buffer.Add(i, frame);
        }

        return true;
    }

    Mat BrightnessAndContrast(Mat src, float alpha, float beta)
    {
        src.GetArray(out Vec3b[] array);
        var modified = array.Select(x =>
            new Vec3b(SaturateCast.ToByte(alpha * x[0] + beta),
                SaturateCast.ToByte(alpha * x[1] + beta),
                SaturateCast.ToByte(alpha * x[2] + beta)));
        return Mat.FromArray(modified).Reshape(src.Rows, src.Cols);
    }

    Mat DoMagic(Mat frame, int index, string output)
    {
        byte threshold = 69;
        var blur = frame.MedianBlur(3);

        blur.GetArray(out Vec3b[] arrayBlur);
        var darkenArray = arrayBlur.Select(x =>
            new Vec3b(Math.Min(x[0], threshold), Math.Min(x[1], threshold), Math.Min(x[2], threshold))).ToArray();
        Mat darken = Mat.FromArray(darkenArray).Reshape(blur.Rows, blur.Cols);

        const float contrast = 80;
        const float alpha = (contrast + 100) / 100;
        const float beta = 80;

        var lighter = BrightnessAndContrast(darken, alpha, beta);
        lighter = lighter.CvtColor(ColorConversionCodes.RGB2GRAY);
        Cv2.ImWrite(Path.Join(imageOutput1 + "light.png"), lighter);
        var thresholdMat = lighter.Threshold(180, 255, ThresholdTypes.BinaryInv);

        var kernel = new int[3, 3];
        for (var j = 0; j < kernel.GetLength(0); j++)
        for (var k = 0; k < kernel.GetLength(1); k++)
            kernel[j, k] = 1;

        Mat closing = thresholdMat.MorphologyEx(MorphTypes.Close, null);
        Mat contoursParams = new();
        closing.FindContours(out var contours, OutputArray.Create(contoursParams), RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);
        List<Rect> trafficLights = [];
        foreach (Mat contour in contours)
        {
            var peri = contour.ArcLength(true);
            var approx = contour.ApproxPolyDP(0.04 * peri, true);

            if (approx.Rows < 4) continue;
            if (contour.ContourArea() < 200) continue;
            trafficLights.Add(contour.BoundingRect());
            frame.Rectangle(contour.BoundingRect(), new Scalar(0, 255, 0), 2);
        }

        return frame;
    }
}

const string videoPath =
    @"C:\Users\zresp\Desktop\ActualProjects\Python\SemaforCutter\RailwaySignsCutter\video\video2.mp4";
const string outputPath = @"C:\Users\zresp\Desktop\ActualProjects\Python\SemaforCutter\RailwaySignsCutter\klatki";
const string outputFrames = @"C:\Users\zresp\Desktop\ActualProjects\Python\SemaforCutter\RailwaySignsCutter\klatki";

const string imageOutput = @"C:\Users\zresp\Desktop\ActualProjects\Python\SemaforCutter\RailwaySignsCutter\images";
const string imageSource = @"C:\Users\zresp\Desktop\ActualProjects\Python\SemaforCutter\RailwaySignsCutter\images";
const int skip = 1300;

const int taskLimit = 500;
const int batch = 1000;

FromVideo(videoPath, batch, skip, outputFrames, outputPath, imageOutput);