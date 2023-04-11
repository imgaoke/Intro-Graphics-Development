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
  
  List<float> listOfR = new List<float>();
  List<float> listOfG = new List<float>();
  List<float> listOfB = new List<float>();

  int accessibleNeighbours = 0;


  // 9x9 median filter kernal
  for (int i = -4; i <= 4; i++)
    for (int j = -4; j <= 4; j++)
    {
      if (ic.x + i >= 0 &&
          ic.x + i < ic.width &&
          ic.y + j >= 0 &&
          ic.y + j < ic.height)
      {
        accessibleNeighbours += 1;
        Color color1;
        lock (globalInput)
          color1 = globalInput.GetPixel(ic.x + i, ic.y + j);
        float XR = color1.R * (1.0f / 255.0f);
        float XG = color1.G * (1.0f / 255.0f);
        float XB = color1.B * (1.0f / 255.0f);
        listOfR.Add(XR);
        listOfG.Add(XG);
        listOfB.Add(XB);
      }
    }
  listOfR.Sort();
  listOfG.Sort();
  listOfB.Sort();

  R = listOfR[listOfR.Count / 2];
  G = listOfG[listOfG.Count / 2];
  B = listOfB[listOfB.Count / 2];

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
