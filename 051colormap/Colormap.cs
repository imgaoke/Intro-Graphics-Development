// Ke Gao
using System.Drawing;
using MathSupport;
using System.Collections.Generic;
using System;

namespace _051colormap
{
  class Colormap
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    public static void InitForm (out string author)
    {
      author = "Ke Gao";
    }

    class ColorCandidate
    {
      public int count = 1;
      public double averageR;
      public double averageG;
      public double averageB;

      public ColorCandidate (int R, int G, int B)
      {
        this.averageR = R;
        this.averageG = G;
        this.averageB = B;
      }
    }

    /// <summary>
    /// Generate a colormap based on input image.
    /// </summary>
    /// <param name="input">Input raster image.</param>
    /// <param name="numCol">Required colormap size (ignore it if you must).</param>
    /// <param name="colors">Output palette (array of colors).</param>
    public static void Generate (Bitmap input, int numCol, out Color[] colors)
    {

      // The general idea to scan the image line by line and fill the list of color candidates for pallete
      int width  = input.Width;
      int height = input.Height;

      // The color at location (0, 0) is chosen as the first candidate
      // this block of code is for first line of the image because (0, 0) is taken out as the first candidate
      // therefore pixels from (1, 0) to (width, 0) are considered
      List<ColorCandidate> candidates = new List<ColorCandidate>();
      Color firstColor = input.GetPixel(0, 0);
      candidates.Add(new ColorCandidate(firstColor.R, firstColor.G, firstColor.B));
      for (int i = 1; i < width; i++)
      {
        Color tempColor = input.GetPixel(i, 0);
        bool newCandidate = true;

        // iterate through all the candidates
        // stops at the first candidate such that if the current pixel "tempColor" is close to the "candidate" in RGB sense, "tempColor" and the "candidate" will
        // be combined to be the new "candidate"
        foreach (ColorCandidate cc in candidates)
        {
          if (Math.Sqrt(Arith.Pow(tempColor.R - cc.averageR, 2) +
            Arith.Pow(tempColor.G - cc.averageG, 2) +
            Arith.Pow(tempColor.B - cc.averageB, 2)) <= 50)
          {
            cc.averageR = (tempColor.R + cc.averageR * cc.count) / (cc.count + 1);
            cc.averageG = (tempColor.G + cc.averageG * cc.count) / (cc.count + 1);
            cc.averageB = (tempColor.B + cc.averageB * cc.count) / (cc.count + 1);
            cc.count += 1;
            newCandidate = false;
            break;
          }
        }
        if (newCandidate == true)
        {
          candidates.Add(new ColorCandidate(tempColor.R, tempColor.G, tempColor.B));
        }
      }


      // this block of code is for rest lines of the image
      for (int i = 0; i < width; i++)
      {
        for (int j = 1; j < height; j++)
        {
          Color tempColor = input.GetPixel(i, j);
          bool newCandidate = true;
          foreach (ColorCandidate cc in candidates)
          {
            if (Math.Sqrt(Arith.Pow(tempColor.R - cc.averageR, 2) +
              Arith.Pow(tempColor.G - cc.averageG, 2) +
              Arith.Pow(tempColor.B - cc.averageB, 2)) <= 50)
            {
              cc.averageR = (tempColor.R + cc.averageR * cc.count) / (cc.count + 1);
              cc.averageG = (tempColor.G + cc.averageG * cc.count) / (cc.count + 1);
              cc.averageB = (tempColor.B + cc.averageB * cc.count) / (cc.count + 1);
              cc.count += 1;
              newCandidate = false;
              break;
            }
          }
          if (newCandidate == true)
          {
            candidates.Add(new ColorCandidate(tempColor.R, tempColor.G, tempColor.B));
          }
        }
      }


      // fill the output array "colors", if there's enough candidates in list "candidates", first "numCol" number of candidates will given to the output
      // otherwise, other than all the candidates that are given to the output, some random colors from the image is taken to be given to the output
      colors = new Color[numCol];
      if (candidates.Count >= colors.Length)
      {
        for(int i = 0; i < colors.Length; i++)
        {
          colors[i] = Color.FromArgb((int)candidates[i].averageR, (int)candidates[i].averageG, (int)candidates[i].averageB); 
        }
      }
      else
      {
        for (int i = 0; i < candidates.Count; i++)
        {
          colors[i] = Color.FromArgb((int)candidates[i].averageR, (int)candidates[i].averageG, (int)candidates[i].averageB);
        }
        for (int i = candidates.Count; i < colors.Length; i++)
        {
          Random r = new Random();
          var x = r.Next(0,width);
          var y = r.Next(0,width);
          colors[i] = input.GetPixel(x, y);
        }
      }
    }
  }
}
