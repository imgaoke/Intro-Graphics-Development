using Raster;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Utilities;



namespace Modules
{
  public class ModuleFullColor : DefaultRasterModule
  {
    /// <summary>
    /// Mandatory plain constructor.
    /// </summary>
    public ModuleFullColor ()
    {
      // Default cell size (wid x hei).
      param = "wid=4096,hei=4096";
    }

    /// <summary>
    /// Author's full name.
    /// </summary>
    public override string Author => "001pilot";

    /// <summary>
    /// Name of the module (short enough to fit inside a list-boxes, etc.).
    /// </summary>
    public override string Name => "FullColor";

    /// <summary>
    /// Tooltip for Param (text parameters).
    /// </summary>
    public override string Tooltip => "[wid=<width>][,hei=<height>][,slow][,ignore-input][,no-check]";

    /// <summary>
    /// Usually read-only, optionally writable (client is defining number of inputs).
    /// </summary>
    public override int InputSlots => 1;

    /// <summary>
    /// Usually read-only, optionally writable (client is defining number of outputs).
    /// </summary>
    public override int OutputSlots => 1;

    /// <summary>
    /// Input raster image.
    /// </summary>
    protected Bitmap inImage = null;

    /// <summary>
    /// Output raster image.
    /// </summary>
    protected Bitmap outImage = null;

    /// <summary>
    /// Output message (color check).
    /// </summary>
    protected string message;

    /// <summary>
    /// Assigns an input raster image to the given slot.
    /// Doesn't start computation (see #Update for this).
    /// </summary>
    /// <param name="inputImage">Input raster image (can be null).</param>
    /// <param name="slot">Slot number from 0 to InputSlots-1.</param>
    public override void SetInput (
      Bitmap inputImage,
      int slot = 0)
    {
      inImage = inputImage;
    }

    /// <summary>
    /// Recompute the output image[s] according to input image[s].
    /// Blocking (synchronous) function.
    /// #GetOutput() functions can be called after that.
    /// </summary>
    public override void Update ()
    {
      // Input image is optional.
      // Starts a new computation.
      UserBreak = false;

      // Default values.
      int wid = 4096;
      int hei = 4096;
      bool fast = true;
      bool ignoreInput = false;
      bool check = true;

      // We are not using 'paramDirty', so the 'Param' string has to be parsed every time.
      Dictionary<string, string> p = Util.ParseKeyValueList(param);
      if (p.Count > 0)
      {
        // wid=<int> [image width in pixels]
        if (Util.TryParse(p, "wid", ref wid))
          wid = Math.Max(1, wid);

        // hei=<int> [image height in pixels]
        if (Util.TryParse(p, "hei", ref hei))
          hei = Math.Max(1, wid);

        // slow ... use Bitmap.SetPixel()
        fast = !p.ContainsKey("slow");

        // ignore-input ... ignore input image even if it is present
        ignoreInput = p.ContainsKey("ignore-input");

        // no-check ... disable color check at the end
        check = !p.ContainsKey("no-check");
      }

      outImage = new Bitmap(wid, hei, PixelFormat.Format24bppRgb);

      // Generate full-color image.
      int xo, yo;
      byte ro, go, bo;

      // record if a color has been used
      var colorUsed = new Boolean[256, 256, 256];

      // color search function
      (int rr, int bb, int gg) colorSearch(int r, int g, int b){
        //search red channel
        (int, int, int) rSearch (int inR, int inG, int inB)
        {
          int firstRange = inR <= 127 ? inR : 255 - inR;
          int restStart = inR <= 127 ? inR + firstRange + 1 : 0;
          int restRange = inR <= 127 ? 255 - 2 * inR : 255 - 2 * (255 - inR);

          if (colorUsed[inR, inG, inB] == false)
          {
            colorUsed[inR, inG, inB] = true;
            return (inR, inG, inB);
          }

          for (int i = 1; i <= firstRange; ++i)
          {
            if (colorUsed[inR + i, inG, inB] == false)
            {
              colorUsed[inR + i, inG, inB] = true;
              return (inR + i, inG, inB);
            }
            else if (colorUsed[inR - i, inG, inB] == false)
            {
              colorUsed[inR - i, inG, inB] = true;
              return (inR - i, inG, inB);
            }
          }

          for (int i = 0; i < restRange; ++i)
          {
            if (colorUsed[restStart + i, inG, inB] == false)
            {
              colorUsed[restStart + i, inG, inB] = true;
              return (restStart + i, inG, inB);
            }
          }
          return (256, 256, 256); // red channel is full
        }

        //search green channel
        (int, int, int) gSearch (int inR, int inG, int inB)
        {
          int firstRange = inG <= 127 ? inG : 255 - inG;
          int restStart = inG <= 127 ? inG + firstRange + 1 : 0;
          int restRange = inG <= 127 ? 255 - 2 * inG : 255 - 2 * (255 - inG);

          if (colorUsed[inR, inG, inB] == false)
          {
            colorUsed[inR, inG, inB] = true;
            return (inR, inG, inB);
          }

          for (int i = 1; i <= firstRange; ++i)
          {
            if (colorUsed[inR, inG + i, inB] == false)
            {
              colorUsed[inR, inG + i, inB] = true;
              return (inR, inG + i, inB);
            }
            else if (colorUsed[inR, inG - i, inB] == false)
            {
              colorUsed[inR, inG - i, inB] = true;
              return (inR, inG - i, inB);
            }
          }

          for (int i = 0; i < restRange; ++i)
          {
            if (colorUsed[inR, restStart + i, inB] == false)
            {
              colorUsed[inR, restStart + i, inB] = true;
              return (inR, restStart + i, inB);
            }
          }
          return (256, 256, 256); // green channel is full
        }

        //search blue channel
        (int, int, int) bSearch (int inR, int inG, int inB)
        {
          int firstRange = inB <= 127 ? inB : 255 - inB;
          int restStart = inB <= 127 ? inB + firstRange + 1 : 0;
          int restRange = inB <= 127 ? 255 - 2 * inB : 255 - 2 * (255 - inB);

          if (colorUsed[inR, inG, inB] == false)
          {
            colorUsed[inR, inG, inB] = true;
            return (inR, inG, inB);
          }

          for (int i = 1; i <= firstRange; ++i)
          {
            if (colorUsed[inR, inG, inB + i] == false)
            {
              colorUsed[inR, inG, inB + i] = true;
              return (inR, inG, inB + i);
            }
            else if (colorUsed[inR, inG, inB - i] == false)
            {
              colorUsed[inR, inG, inB - i] = true;
              return (inR, inG, inB - i);
            }
          }

          for (int i = 0; i < restRange; ++i)
          {
            if (colorUsed[inR, inG, restStart + i] == false)
            {
              colorUsed[inR, inG, restStart + i] = true;
              return (inR, inG, restStart + i);
            }
          }
          return (256, 256, 256); // blue channel is full
        }

        //manually triple loop for which from outside to inside the colors to loop are red, green, blue
        (int, int, int) rgIter ()
        {
          int firstRRange = r <= 127 ? r : 255 - r;
          int restRStart = r <= 127 ? r + firstRRange + 1 : 0;
          int restRRange = r <= 127 ? 255 - 2 * r : 255 - 2 * (255 - r);

          int firstGRange = g <= 127 ? g : 255 - g;
          int restGStart = g <= 127 ? g + firstGRange + 1 : 0;
          int restGRange = g <= 127 ? 255 - 2 * g : 255 - 2 * (255 - g);

          int tempR;
          int tempG;
          int tempB;



          for (int i = 0; i <= firstRRange; ++i)
          {
            for (int j = 0; j <= firstGRange; ++j)
            {
              if (i == 0 && j != 0)
              {
                (tempR, tempG, tempB) = bSearch(r, g + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(r, g - j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else if (i != 0 && j == 0)
              {
                (tempR, tempG, tempB) = bSearch(r + i, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(r - i, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

              else if (i == 0 && j == 0)
              {
                (tempR, tempG, tempB) = bSearch(r, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = bSearch(r + i, g + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(r + i, g - j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(r - i, g + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(r - i, g - j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

            }

            for (int j = 0; j < restGRange; ++j)
            {
              if (i == 0)
              {
                (tempR, tempG, tempB) = bSearch(r, restGStart + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = bSearch(r + i, restGStart + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(r - i, restGStart + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

            }
          }

          for (int i = 0; i <= restRRange; ++i)
          {
            for (int j = 0; j <= firstGRange; ++j)
            {
              if (j == 0)
              {
                (tempR, tempG, tempB) = bSearch(restRStart + i, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = bSearch(restRStart + i, g + j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = bSearch(restRStart + i, g - j, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
            }

            for (int j = 0; j < restGRange; ++j)
            {
              (tempR, tempG, tempB) = bSearch(restRStart + i, restGStart + j, b);
              if ((tempR, tempG, tempB) != (256, 256, 256))
              {
                return (tempR, tempG, tempB);
              }
            }
          }

          Console.WriteLine("rgIter bugged");
          return (256, 256, 256); // bugged
        }

        //manually triple loop for which from outside to inside the colors to loop are green, blue, red
        (int, int, int) gbIter ()
        {
          int firstGRange = g <= 127 ? g : 255 - g;
          int restGStart = g <= 127 ? g + firstGRange + 1 : 0;
          int restGRange = g <= 127 ? 255 - 2 * g : 255 - 2 * (255 - g);

          int firstBRange = b <= 127 ? b : 255 - b;
          int restBStart = b <= 127 ? b + firstBRange + 1 : 0;
          int restBRange = b <= 127 ? 255 - 2 * b : 255 - 2 * (255 - b);

          int tempR;
          int tempG;
          int tempB;



          for (int i = 0; i <= firstGRange; ++i)
          {
            for (int j = 0; j <= firstBRange; ++j)
            {
              if (i == 0 && j != 0)
              {
                (tempR, tempG, tempB) = rSearch(r, g, b + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, g, b - j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else if (i != 0 && j == 0)
              {
                (tempR, tempG, tempB) = rSearch(r, g + i, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, g - i, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

              else if (i == 0 && j == 0)
              {
                (tempR, tempG, tempB) = rSearch(r, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = rSearch(r, g + i, b + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, g + i, b - j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, g - i, b + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, g - i, b - j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

            }

            for (int j = 0; j < restBRange; ++j)
            {
              if (i == 0)
              {
                (tempR, tempG, tempB) = rSearch(r, g, restBStart + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = rSearch(r, g + i, restBStart + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, g - i, restBStart + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

            }
          }

          for (int i = 0; i <= restGRange; ++i)
          {
            for (int j = 0; j <= firstBRange; ++j)
            {
              if (j == 0)
              {
                (tempR, tempG, tempB) = rSearch(r, restGStart + i, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = rSearch(r, restGStart + i, b + j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = rSearch(r, restGStart + i, b - j);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
            }

            for (int j = 0; j < restBRange; ++j)
            {
              (tempR, tempG, tempB) = rSearch(r, restGStart + i, restBStart + j);
              if ((tempR, tempG, tempB) != (256, 256, 256))
              {
                return (tempR, tempG, tempB);
              }
            }
          }
          Console.WriteLine("gbIter bugged");
          return (256, 256, 256); // bugged
        }


        //manually triple loop for which from outside to inside the colors to loop are blue, red, green
        (int, int, int) brIter ()
        {
          int firstBRange = b <= 127 ? b : 255 - b;
          int restBStart = b <= 127 ? b + firstBRange + 1 : 0;
          int restBRange = b <= 127 ? 255 - 2 * b : 255 - 2 * (255 - b);

          int firstRRange = r <= 127 ? r : 255 - r;
          int restRStart = r <= 127 ? r + firstRRange + 1 : 0;
          int restRRange = r <= 127 ? 255 - 2 * r : 255 - 2 * (255 - r);

          int tempR;
          int tempG;
          int tempB;



          for (int i = 0; i <= firstBRange; ++i)
          {
            for (int j = 0; j <= firstRRange; ++j)
            {
              if (i == 0 && j != 0)
              {
                (tempR, tempG, tempB) = gSearch(r + j, g, b );
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(r - j, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else if (i != 0 && j == 0)
              {
                (tempR, tempG, tempB) = gSearch(r, g, b + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(r, g, b - i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

              else if (i == 0 && j == 0)
              {
                (tempR, tempG, tempB) = gSearch(r, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = gSearch(r + j, g, b + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(r - j, g, b + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(r + j, g, b - i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(r - j, g, b - i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

            }

            for (int j = 0; j < restRRange; ++j)
            {
              if (i == 0)
              {
                (tempR, tempG, tempB) = gSearch(restRStart + j, g, b);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = gSearch(restRStart + j, g, b + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(restRStart + j, g, b - i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }

            }
          }

          for (int i = 0; i <= restBRange; ++i)
          {
            for (int j = 0; j <= firstRRange; ++j)
            {
              if (j == 0)
              {
                (tempR, tempG, tempB) = gSearch(r, g, restBStart + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
              else
              {
                (tempR, tempG, tempB) = gSearch(r + j, g, restBStart + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
                (tempR, tempG, tempB) = gSearch(r - j, g, restBStart + i);
                if ((tempR, tempG, tempB) != (256, 256, 256))
                {
                  return (tempR, tempG, tempB);
                }
              }
            }

            for (int j = 0; j < restRRange; ++j)
            {
              (tempR, tempG, tempB) = gSearch(restRStart + j, g, restBStart + i);
              if ((tempR, tempG, tempB) != (256, 256, 256))
              {
                return (tempR, tempG, tempB);
              }
            }
          }
          Console.WriteLine("brIter bugged");
          return (256, 256, 256); // bugged
        }


        // for each pixel in the picture, randomly choose the search sequence
        Random rand = new Random();
        int nextRGB = rand.Next(3);
        switch (nextRGB)
        {
          case 0:
            return rgIter();
          case 1:
            return gbIter();
          case 2:
            return brIter();
        }
        return (256, 256, 256);
      }

      if (!ignoreInput &&
          inImage != null)
      {
        // Input image is present => use it.      

        // Convert pixel data (fast memory-mapped code).
        PixelFormat iFormat = inImage.PixelFormat;
        if (!PixelFormat.Format24bppRgb.Equals(iFormat) &&
            !PixelFormat.Format32bppArgb.Equals(iFormat) &&
            !PixelFormat.Format32bppPArgb.Equals(iFormat) &&
            !PixelFormat.Format32bppRgb.Equals(iFormat))
          iFormat = PixelFormat.Format24bppRgb;

        int width  = inImage.Width;
        int height = inImage.Height;
        int xi, yi;
        BitmapData dataIn  = inImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, iFormat);
        BitmapData dataOut = outImage.LockBits(new Rectangle( 0, 0, wid, hei ), ImageLockMode.WriteOnly, outImage.PixelFormat);
        unsafe
        {
          byte* iptr, optr;
          byte ri, gi, bi;
          int dI = Image.GetPixelFormatSize(iFormat) / 8;               // pixel size in bytes
          int dO = Image.GetPixelFormatSize(outImage.PixelFormat) / 8;  // pixel size in bytes

          yi = 0;
          for (yo = 0; yo < hei; yo++)
          {
            // User break handling.
            if (UserBreak)
              break;

            iptr = (byte*)dataIn.Scan0  + yi * dataIn.Stride;
            optr = (byte*)dataOut.Scan0 + yo * dataOut.Stride;

            xi = 0;
            for (xo = 0; xo < wid; xo++)
            {
              // read input colors
              bi = iptr[0];
              gi = iptr[1];
              ri = iptr[2];
              // Console.WriteLine(bi); //bi, gi, ri is in range 0-255

              // !!! TODO: do anything with the colors


              (var tempR, var tempG, var tempB) = colorSearch(bi, gi, ri);

              (bo, go, ro) = ((byte)tempR, (byte)tempG, (byte)tempB);

              // write output colors
              optr[0] = bo;
              optr[1] = go;
              optr[2] = ro;

              iptr += dI;
              optr += dO;
              if (++xi >= width)
              {
                xi = 0;
                iptr = (byte*)dataIn.Scan0 + yi * dataIn.Stride;
              }
            }

            if (++yi >= height)
            {
              yi = 0;
            }
              
          }
        }
        outImage.UnlockBits(dataOut);
        inImage.UnlockBits(dataIn);
      }
      else
      {
        // No input => generate constant full-color image.

        int col;
        if (fast)
        {
          // Generate pixel data (fast memory-mapped code).

          BitmapData dataOut = outImage.LockBits(new Rectangle(0, 0, wid, hei), ImageLockMode.WriteOnly, outImage.PixelFormat);
          unsafe
          {
            byte* optr;
            int dO = Image.GetPixelFormatSize(outImage.PixelFormat) / 8;  // pixel size in bytes

            col = 0;
            for (yo = 0; yo < hei; yo++)
            {
              // User break handling.
              if (UserBreak)
                break;

              optr = (byte*)dataOut.Scan0 + yo * dataOut.Stride;

              for (xo = 0; xo < wid; xo++, col++)
              {
                // !!! TODO: do anything with the input color
                bo = (byte)((col >> 16) & 0xFF);
                go = (byte)((col >> 8) & 0xFF);
                ro = (byte)(col & 0xFF);

                // write output colors
                optr[0] = bo;
                optr[1] = go;
                optr[2] = ro;

                optr += dO;
              }
            }
          }
          outImage.UnlockBits(dataOut);
        }
        else
        {
          // Generate pixel data (slow mode).

          col = 0;
          for (yo = 0; yo < hei; yo++)
          {
            // User break handling.
            if (UserBreak)
              break;

            for (xo = 0; xo < wid; xo++, col++)
            {
              // !!! TODO: do anything with the input color
              bo = (byte)((col >> 16) & 0xFF);
              go = (byte)((col >> 8) & 0xFF);
              ro = (byte)(col & 0xFF);

              // write output colors
              outImage.SetPixel(xo, yo, Color.FromArgb(ro, go, bo));
            }
          }
        }
      }

      // Output message.
      if (check &&
          !UserBreak)
      {
        long colors = Draw.ColorNumber(outImage);
        message = colors == (1 << 24) ? "Colors: 16M, Ok" : $"Colors: {colors}, Fail";
      }
      else
        message = null;
    }

    /// <summary>
    /// Returns an output raster image.
    /// Can return null.
    /// </summary>
    /// <param name="slot">Slot number from 0 to OutputSlots-1.</param>
    public override Bitmap GetOutput (
      int slot = 0) => outImage;

    /// <summary>
    /// Returns an optional output message.
    /// Can return null.
    /// </summary>
    /// <param name="slot">Slot number from 0 to OutputSlots-1.</param>
    public override string GetOutputMessage (
      int slot = 0) => message;
  }
}
