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
  public class PortraitPreservingRecoloring : ModuleFormula
  {
    /// <summary>
    /// Mandatory plain constructor.
    /// </summary>
    public PortraitPreservingRecoloring ()
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
    public override string Name => "PortraitPreservingRecoloring";

    /// <summary>
    /// Tooltip for Param (text parameters).
    /// </summary>
    public override string Tooltip =>
      "fast/slow .. fast/slow bitmap access\r" +
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


      f.pixelTransform0 = (
        in ImageContext ic,
        ref float R,
        ref float G,
        ref float B) =>
      {
        // color of profile00.jpg
        double referenceR = 131.0d / 255.0d;
        double referenceG = 88.0d / 255.0d;
        double referenceB = 82.0d / 255.0d;
        double referenceHue, referenceSaturation, referenceValue = 0.0d;
        MathSupport.Arith.RGBtoHSV((double)referenceR, (double)referenceG, (double)referenceB, out referenceHue, out referenceSaturation, out referenceValue);

        double hue, saturation, value = 0.0d;
        MathSupport.Arith.RGBtoHSV((double)R, (double)G, (double)B, out hue, out saturation, out value);

        double hueRange = 40.0d;
        double saturationRange = 0.6d;
        double valueRange = 0.6d;
        double hueGeneralDifference = Math.Abs(hue - referenceHue);
        double hueSmallerDifference = hueGeneralDifference > 180 ? 360 - hueGeneralDifference : hueGeneralDifference;
        double saturationDifference = Math.Abs(saturation - referenceSaturation);
        double valueDifference = Math.Abs(value - referenceValue);
         
        if (hueSmallerDifference <= hueRange && saturationDifference <= saturationRange && valueDifference <= valueRange)
        {
          // completely protected
          //value = Util.Saturate(value - 1); // for debugging
        }
        else if ((hueSmallerDifference > hueRange && hueSmallerDifference <= hueRange * 2) &&
        (saturationDifference > saturationRange && saturationDifference <= saturationRange * 2) &&
        (valueDifference > valueRange && valueDifference <= valueRange * 2))
        {
          // partly protected
          hue = (hue + 120.0d * Math.Sqrt((hueSmallerDifference / 360.0d) * (hueSmallerDifference / 360.0d) + saturationDifference * saturationDifference + valueDifference * valueDifference));
        }
        else
        {
          // completely re-coloured
          hue = (hue + 120.0d) % 360;
        }

        MathSupport.Arith.HSVtoRGB((float)hue, (float)saturation, (float)value, out R, out G, out B);

        // Output color was modified.
        return true;
      };
     

      return f;
    }
  }
}
