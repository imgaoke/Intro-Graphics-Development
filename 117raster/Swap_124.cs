// Text params -> script context.
// Any global pre-processing is allowed here.
public Bitmap globalInput;
formula.contextCreate = (in Bitmap input, in string param) =>
{
  //globalInput = input;
  lock(input)
    globalInput = (Bitmap)input.Clone();
  if (string.IsNullOrEmpty(param))
    return null;

  Dictionary<string, string> p = Util.ParseKeyValueList(param);

  float coeff = 1.0f;

  // coeff=<float>
  if (Util.TryParse(p, "coeff", ref coeff))
    coeff = Util.Saturate(coeff);

  float freq = 12.0f;

  // freq=<float>
  if (Util.TryParse(p, "freq", ref freq))
    freq = Util.Clamp(freq, 0.01f, 1000.0f);

  Dictionary<string, object> sc = new Dictionary<string, object>();
  sc["coeff"] = coeff;
  sc["freq"] = freq;
  sc["tooltip"] = "coeff=<float> .. swap coefficient (0.0 - no swap, 1.0 - complete swap)\r" +
                  "freq=<float> .. density frequency for image generation (default=12)";

  return sc;
};

// R <-> B channel swap with weights.
formula.pixelTransform0 = (
  in ImageContext ic,
  ref float R,
  ref float G,
  ref float B) =>
{

  // double the hue value
  float coeff = 0.0f;
  float r = R;
  float g = G;
  float b = B;
  Arith.RGBtoHSV(r * 255.0, g * 255.0, b * 255.0, out double H, out double S, out double V);
  double h = H * 2.0 % 360;
  
  Arith.HSVtoRGB(h, S, V, out double rr, out double gg, out double bb);
  R = (float)(rr / 255.0);
  G = (float)(gg / 255.0);
  B = (float)(bb / 255.0);


  
  List<(int, int, int)> listOfXEdges = new List<(int, int, int)>() { (-1, -1, 1), (0, -1, 2), (1, -1, 1), (-1, 1, -1), (0, 1, -2), (1, 1, -1)};
  List<(int, int, int)> listOfYEdges = new List<(int, int, int)>() { (-1, -1, 1), (-1, 0, 2), (-1, 1, 1), (1, -1, -1), (0, 1, -2), (1, 1, -1)};
  float[,] listOfXYAccumulators = new float[2,3] { { 0, 0, 0 }, { 0, 0, 0 } };
  int accessibleXNeighbours = 0;
  int accessibleYNeighbours = 0;


  // highlighten edges by a 3x3 sobel kernal
  for (int i = 0; i < 6; i++)
  {
    if (ic.x + listOfXEdges[i].Item1 >= 0 &&
        ic.x + listOfXEdges[i].Item1 < ic.width &&
        ic.y + listOfXEdges[i].Item2 >= 0 &&
        ic.y + listOfXEdges[i].Item2 < ic.height)
    {
      accessibleXNeighbours += 1;
      Color color1;
      lock (globalInput)
        color1 = globalInput.GetPixel(ic.x + listOfXEdges[i].Item1, ic.y + listOfXEdges[i].Item2);
      float XR = color1.R * (1.0f / 255.0f);
      float XG = color1.G * (1.0f / 255.0f);
      float XB = color1.B * (1.0f / 255.0f);
      listOfXYAccumulators[0, 0] += XR * listOfXEdges[i].Item3;
      listOfXYAccumulators[0, 1] += XG * listOfXEdges[i].Item3;
      listOfXYAccumulators[0, 2] += XB * listOfXEdges[i].Item3;
    }
    
    if (ic.x + listOfYEdges[i].Item1 >= 0 &&
        ic.x + listOfYEdges[i].Item1 < ic.width &&
        ic.y + listOfYEdges[i].Item2 >= 0 &&
        ic.y + listOfYEdges[i].Item2 < ic.height)
    {
      accessibleYNeighbours += 1;
      Color color2;
      lock (globalInput)
        color2 = globalInput.GetPixel(ic.x + listOfYEdges[i].Item1, ic.y + listOfYEdges[i].Item2);
      float YR = color2.R * (1.0f / 255.0f);
      float YG = color2.G * (1.0f / 255.0f);
      float YB = color2.B * (1.0f / 255.0f);
      listOfXYAccumulators[1, 0] += YR * listOfYEdges[i].Item3;
      listOfXYAccumulators[1, 1] += YG * listOfYEdges[i].Item3;
      listOfXYAccumulators[1, 2] += YB * listOfYEdges[i].Item3;
    }
  }



  for (int i = 0; i < 3; i++)
  { 
    listOfXYAccumulators[0, i] = listOfXYAccumulators[0, i] / (float)accessibleXNeighbours;
    listOfXYAccumulators[1, i] = listOfXYAccumulators[1, i] / (float)accessibleYNeighbours;

  }
  R = (float)Math.Sqrt(listOfXYAccumulators[0, 0] * listOfXYAccumulators[0, 0] + listOfXYAccumulators[1, 0] * listOfXYAccumulators[1, 0]);
  G = (float)Math.Sqrt(listOfXYAccumulators[0, 1] * listOfXYAccumulators[0, 1] + listOfXYAccumulators[1, 1] * listOfXYAccumulators[1, 1]);
  B = (float)Math.Sqrt(listOfXYAccumulators[0, 2] * listOfXYAccumulators[0, 2] + listOfXYAccumulators[1, 2] * listOfXYAccumulators[1, 2]);
  
  return true;
};

// Test create function: sinc(r^2)
formula.pixelCreate = (
  in ImageContext ic,
  out float R,
  out float G,
  out float B) =>
{
  // [x, y] in {0, 1]
  double x = ic.x / (double)Math.Max(1, ic.width - 1);
  double y = ic.y / (double)Math.Max(1, ic.height - 1);

  // I need uniform scale (x-scale == y-scale) with origin at the image center.
  if (ic.width > ic.height)
  {
    // Landscape.
    x -= 0.5;
    y = ic.height * (y - 0.5) / ic.width;
  }
  else
  {
    // Portrait.
    x = ic.width * (x - 0.5) / ic.height;
    y -= 0.5;
  }

  // Custom scales.
  float freq = 12.0f;
  Util.TryParse(ic.context, "freq", ref freq);

  x *= freq;
  y *= freq;

  // Periodic function of r^2.
  double rr = x * x + y * y;
  bool odd = ((int)Math.Round(rr) & 1) > 0;

  // Simple color palette (yellow, blue).
  R = odd ? 0.0f : 1.0f;
  G = R;
  B = 1.0f - R;
};
