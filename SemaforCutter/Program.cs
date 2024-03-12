using OpenCvSharp;

var video_path = "video/video2.mp4";
var output_path = "video1_output_part1.mp4";
var frame_rate = 30;

var cap = new VideoCapture(video_path);

List<Task> frames = new();
for (int i = 0; i < cap.FrameCount; i++)
{
    Mat frame = new();
    var dupa = cap.Read(frame);
    if (!dupa) throw new Exception("Coś się zjebało przy ładowaniu klatek");
    var task = DoMagic(frame, i);
    frames.Add(task);
}

foreach (var frame in frames) frame.Wait();

async Task DoMagic(Mat frame, int index)
{
    int treshold = 69;
    var blur = frame.MedianBlur(3);
    int[] array_blur = null;

    blur.GetArray(out array_blur);
    var darken_array = array_blur.Select(x => Math.Min(treshold, x)).ToArray();
    Mat darken = Mat.FromArray(darken_array);

    float contrast = 3.5f;
    float alpha = (contrast + 100) / 100;
    float beta = 3.5f;

    Mat lighter = darken.ConvertScaleAbs(alpha, beta);
    lighter = lighter.CvtColor(ColorConversionCodes.RGB2GRAY);

    Mat threshold = lighter.Threshold(180, 255, ThresholdTypes.BinaryInv);

    int[,] kernel = new int[3, 3];
    for (int j = 0; j < kernel.GetLength(0); j++)
    {
        for (int k = 0; k < kernel.GetLength(1); k++)
        {
            kernel[j, k] = 1;
        }
    }

    Mat closing = threshold.MorphologyEx(MorphTypes.Close, null, null);
    Mat contursParams = new();
    closing.FindContours(out Mat[] contours, OutputArray.Create(contursParams), RetrievalModes.External,
        ContourApproximationModes.ApproxSimple);
    Mat[] traffic_ligths;
    foreach (Mat contur in contours)
    {
        var peri = contur.ArcLength(true);
        var approx = contur.ApproxPolyDP(0.04 * peri, true);

        if (approx.Cols * approx.Rows > 3)
        {
            
        }
    }
        

}

