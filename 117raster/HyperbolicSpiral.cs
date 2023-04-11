using System;
using System.Collections.Generic;
using System.Drawing;
using Utilities;
using MathSupport;


namespace Modules
{
  /// <summary>
  /// ModuleFormulaInternal class - template for internal image filter
  /// defined pixel-by-pixel using lambda functions.
  /// </summary>
  public class HyperbolicSpirals : ModuleFormula
  {
    /// <summary>
    /// Mandatory plain constructor.
    /// </summary>
    public HyperbolicSpirals ()
    {
      param = "fast";
    }

    /// <summary>
    /// Author's full name (SurnameFirstname).
    /// </summary>
    public override string Author => "KeGao";

    /// <summary>
    /// Name of the module (short enough to fit inside a list-boxes, etc.).
    /// </summary>
    public override string Name => "HyperbolicSpiral";

    /// <summary>
    /// Tooltip for Param (text parameters).
    /// </summary>
    public override string Tooltip =>
      "fast/slow .. fast/slow bitmap access\r" +
      "create .. new image\r" +
      "wid=<width>, hei=<height>";

    //====================================================
    //--- Formula defined directly in this source file ---

    /// <summary>
    /// Defines implicit formulas if available.
    /// </summary>
    /// <returns>null if formulas sould be read from a script.</returns>
    protected override Formula GetFormula ()
    {
      Formula f = new Formula();

      // Text params -> script context.
      // Any global pre-processing is allowed here.
      f.contextCreate = (in Bitmap input, in string param) =>
      {
        if (string.IsNullOrEmpty(param))
          return null;

        Dictionary<string, string> p = Util.ParseKeyValueList(param);

        double lowestLightness = 0.0d;
       
        // lowestLightness=<double>
        if (Util.TryParse(p, "lowestLightness", ref lowestLightness))
          lowestLightness = Util.Clamp(lowestLightness, 0.01d, 1.0d);

        Dictionary<string, object> sc = new Dictionary<string, object>();
        sc["lowestLightness"] = lowestLightness;
        sc["tooltip"] = "lowestLightness=<double> .. lower bound of the image lightness (in the middle of the picture)";

        return sc;
      };

      double hueOffsetFinder (double a, double r, double phi)
      {
        // R is what's on the sprial line of the function
        double R = a / phi;
        int hueOffset = 0;
        // count the number of cycles the current point is inside the spiral line and this number is treated as hueOffset
        while (R > r)
        {
          phi += 2 * Math.PI;
          R = a / phi;
          hueOffset += 1;
        }
        return hueOffset;
      }

      // Test create function: sinc(r^2)
      f.pixelCreate = (
        in ImageContext ic,
        out float R,
        out float G,
        out float B) =>
      {
        // [x, y] in {0, 1]
        double x = ic.x / (double)Math.Max(1, ic.width  - 1);
        double y = ic.y / (double)Math.Max(1, ic.height - 1);

        // I need uniform scale (x-scale == y-scale) with origin at the image center.
        double diagonal; //longest distance from center of the image to its boundary
        if (ic.width > ic.height)
        {
          // Landscape.
          x -= 0.5;
          y = ic.height * (y - 0.5) / ic.width;
          diagonal = Math.Sqrt(0.5 * 0.5 + (0.5 * ic.height / ic.width) * (0.5 * ic.height / ic.width));
        }
        else
        {
          // Portrait.
          x = ic.width * (x - 0.5) / ic.height;
          y -= 0.5;
          diagonal = Math.Sqrt((0.5 * ic.width / ic.height) * (0.5 * ic.width / ic.height) + 0.5 * 0.5);
        }

        //https://handwiki.org/wiki/Hyperbolic_spiral
        double a = ic.height;
        //here r is not what's in the hyperbolic spiral function r = a / phi
        //but will be used as a measure of diference between the current point and the real r computed by the function
        double r = Math.Sqrt(x * x + y * y);
        double phi = Math.Atan2(y , x);
        // make angle in the range of 0-2*PI
        if (phi < 0)
          phi += 2 * Math.PI;

        double hue = (180 / Math.PI) * phi;
        double hueOffset = hueOffsetFinder(a, r, phi);
        double saturation = 1.0;
        double lowestLightness = 0.0d;
        Util.TryParse(ic.context, "lowestLightness", ref lowestLightness);
        double value = Util.Saturate(r/diagonal + lowestLightness);
        MathSupport.Arith.HSVtoRGB((float)(hue + hueOffset) % 360, (float)saturation, (float)value, out R, out G, out B);
      };

      return f;
    }
  }
}
