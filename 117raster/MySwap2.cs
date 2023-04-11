// Text params -> script context.
// Any global pre-processing is allowed here.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

formula.contextCreate = (in Bitmap input, in string param) =>
{
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
  float coeff = 0.0f;
  Util.TryParse(ic.context, "coeff", ref coeff);

  float r = Util.Saturate(R * (1.0f - coeff) + B * coeff);
  float b = Util.Saturate(R * coeff + B * (1.0f - coeff));
  R = r;
  B = b;

  // Output color was modified.
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

  // four corners
  double x1 = (ic.x + 1) / (double)Math.Max(1, ic.width - 1);
  double y1 = (ic.y - 1) / (double)Math.Max(1, ic.height - 1);
  double x2 = (ic.x + 1) / (double)Math.Max(1, ic.width - 1);
  double y2 = (ic.y + 1) / (double)Math.Max(1, ic.height - 1);
  double x3 = (ic.x - 1) / (double)Math.Max(1, ic.width - 1);
  double y3 = (ic.y - 1) / (double)Math.Max(1, ic.height - 1);
  double x4 = (ic.x - 1) / (double)Math.Max(1, ic.width - 1);
  double y4 = (ic.y + 1) / (double)Math.Max(1, ic.height - 1);
  List<(double, double)> listOfCorners = new List<(double, double)>() { (x1, y1), (x2, y2), (x3, y3), (x4, y4) };


  // I need uniform scale (x-scale == y-scale) with origin at the image center.
  if (ic.width > ic.height)
  {
    // Landscape.
    x -= 0.5;
    y = ic.height * (y - 0.5) / ic.width;
    for (int i = 0; i < listOfCorners.Count; i++)
    {
      double newX = listOfCorners[i].Item1 - 0.5;
      double newY = ic.height * (listOfCorners[i].Item2 - 0.5) / ic.width;
      listOfCorners[i] = (newX, newY);
    }
  }
  else
  {
    // Portrait.
    x = ic.width * (x - 0.5) / ic.height;
    y -= 0.5;
    for (int i = 0; i < listOfCorners.Count; i++)
    {
      double newX = ic.width * (listOfCorners[i].Item1 - 0.5) / ic.height;
      double newY = listOfCorners[i].Item2 - 0.5;
      listOfCorners[i] = (newX, newY);
    }
  }

  // greyscales
  double greyScale = (float)(Math.Sqrt(x * x + y * y));
  List<double> grayScales = new List<double>();
  foreach ((double, double) corner in listOfCorners)
  {
    grayScales.Add((double)(Math.Sqrt(corner.Item1 * corner.Item1 + corner.Item2 * corner.Item2)));
  }

  // Custom scales.
  float freq = 4f;
  Util.TryParse(ic.context, "freq", ref freq);

  x *= freq;
  y *= freq;

  // test if the current pixel is in the mandelbrot set
  for (int i = 0; i < listOfCorners.Count; i++)
  {
    double newX = 4 * listOfCorners[i].Item1;
    double newY = 4 * listOfCorners[i].Item2;
    listOfCorners[i] = (newX, newY);
  }
  int iter = 0;
  double zX = 0;
  double zY = 0;
  do
  {
    iter++;
    double tempX = zX * zX - zY * zY;
    double tempY = 2 * zX * zY;
    zX = tempX + x;
    zY = tempY + y;
    if (Math.Sqrt(zX * zX + zY * zY) > 2.0) break;
  }
  while (iter < 100);
  bool inMan = iter < 100 ? true : false;


  // count the number of corners of the current pixel that is in the same set as the current pixel
  int samePixelCount = 0;
  
  List<bool> inManCorners = new List<bool>();
  foreach ((double, double) corner in listOfCorners)
  {
    iter = 0;
    zX = 0;
    zY = 0;
    do
    {
      iter++;
      double tempX = zX * zX - zY * zY;
      double tempY = 2 * zX * zY;
      zX = tempX + corner.Item1;
      zY = tempY + corner.Item2;
      if (Math.Sqrt(zX * zX + zY * zY) > 2.0) break;
    }
    while (iter < 100);

    bool inManCorner = iter < 100 ? true : false;
    if (inManCorner == inMan)
    {
      samePixelCount += 1;
    }
    inManCorners.Add(inManCorner);
  }

  // greyscale color scheme
  // if any of the corners of the current pixel is not in the same as the current pixel, we replace current pixel's greyscale with the average of the grayscale value of the neighbours
  if (samePixelCount != 4)
  {
    double totalCornerGrayScales = 0;
    for (int i = 0; i < 4; i++)
    {
      if (inManCorners[i] == true) 
      {
        totalCornerGrayScales += grayScales[i];
      }
      else
      {
        totalCornerGrayScales += 1 - grayScales[i];
      }
    }
    R = (float)totalCornerGrayScales / 4;
  }
  else
  {
    R = (float)(inMan ? greyScale : 1 - greyScale);
  }

  G = R;
  B = R;
};
