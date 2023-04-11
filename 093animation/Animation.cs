using System;
using System.Collections.Generic;
using System.Drawing;
using LineCanvas;
using MathSupport;
using Utilities;

namespace _093animation
{
  public class Animation
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    /// <param name="name">Your first-name and last-name.</param>
    /// <param name="wid">Image width in pixels.</param>
    /// <param name="hei">Image height in pixels.</param>
    /// <param name="from">Animation start in seconds.</param>
    /// <param name="to">Animation end in seconds.</param>
    /// <param name="fps">Frames-per-seconds.</param>
    /// <param name="param">Optional text to initialize the form's text-field.</param>
    /// <param name="tooltip">Optional tooltip = param help.</param>
    public static void InitParams (out string name, out int wid, out int hei, out double from, out double to, out double fps, out string param, out string tooltip)
    {
      // {{

      // Put your name here.
      name = "Ke Gao";

      // Image size in pixels.
      wid = 1600;
      hei = 900;

      // Animation.
      from = 0.0;
      to   = 10.0;
      fps  = 25.0;

      // Specific animation params.
      param = "width=8.0,anti=true,numberOfLinesOnLargerDimension=30,seed=12,degree=30";

      // Tooltip = help.
      tooltip = "width=<int>, anti[=<bool>], numberOfLinesOnLargerDimension=<int>, seed=<int>, degree=<int>";

      // }}
    }

    /// <summary>
    /// Global initialization. Called before each animation batch
    /// or single-frame computation.
    /// </summary>
    /// <param name="width">Width of the future canvas in pixels.</param>
    /// <param name="height">Height of the future canvas in pixels.</param>
    /// <param name="start">Start time (t0)</param>
    /// <param name="end">End time (for animation length normalization).</param>
    /// <param name="fps">Required fps.</param>
    /// <param name="param">Optional string parameter from the form.</param>
    public static void InitAnimation (int width, int height, double start, double end, double fps, string param)
    {
      // {{ TODO: put your init code here

      // }}
    }

    /// <summary>
    /// Draw single animation frame.
    /// Has to be re-entrant!
    /// </summary>
    /// <param name="c">Canvas to draw to.</param>
    /// <param name="time">Current time in seconds.</param>
    /// <param name="start">Start time (t0)</param>
    /// <param name="end">End time (for animation length normalization).</param>
    /// <param name="param">Optional string parameter from the form.</param>
    public static void DrawFrame (Canvas c, double time, double start, double end, string param)
    {
      // {{ TODO: put your drawing code here

      double timeNorm = Arith.Clamp((time - start) / (end - start), 0.0, 1.0);
      // Input params.
      float penWidth                     = 1.0f;   // pen width
      bool antialias                     = false;  // use anti-aliasing?
      int numberOfLinesOnLargerDimension = 10;     // number of lines on the larger dimension of the canvas
      int seed                           = 12;     // random generator seed
      int degree                         = 30;     // rotation of the whole graph

      

      Dictionary<string, string> p = Util.ParseKeyValueList(param);
      if (p.Count > 0)
      {
        // with=<line-width>
        if (Util.TryParse(p, "width", ref penWidth))
        {
          if (penWidth < 0.0f)
            penWidth = 0.0f;
        }

        // degree=<int>
        if (Util.TryParse(p, "degree", ref degree))
        {
          if (degree < 0)
            degree = 0;
        }

        // anti[=<bool>]
        Util.TryParse(p, "anti", ref antialias);

        // numberOfLinesOnLargerDimension=<int>
        if (Util.TryParse(p, "numberOfLinesOnLargerDimension", ref numberOfLinesOnLargerDimension) &&
            numberOfLinesOnLargerDimension < 0)
          numberOfLinesOnLargerDimension = 0;

        // seed=<int>
        Util.TryParse(p, "seed", ref seed);
      }

      // rotate the whole picture with time
      degree = 30 + (int)(timeNorm * 90);

      c.Clear(Color.White);
      c.SetPenWidth(penWidth);
      c.SetAntiAlias(antialias);
      c.SetColor(Color.Black);

      Point canvasOrigin = new Point(c.Width / 2, c.Height / 2);

      // return the rotated point around a center
      Point rotatePoint (float rotationDegree, Point point, Point center)
      {
        Point pointRelativeToCenter = new Point(point.X - center.X, point.Y - center.Y);
        var radian = rotationDegree * System.Math.PI / 180.0;
        float cos = (float)Math.Cos(radian);
        float sin = (float)Math.Sin(radian);
        Point newPointRelativeToCenter = new Point((int)(pointRelativeToCenter.X * cos - pointRelativeToCenter.Y * sin), (int)(pointRelativeToCenter.X * sin + pointRelativeToCenter.Y * cos));
        Point newPointBack = new Point(newPointRelativeToCenter.X + center.X, newPointRelativeToCenter.Y + center.Y);
        return newPointBack;
      }

      // a line after rotation may become shorter so this function clamp the end points of lines to the border of the canvas
      (Point, Point) clampLine (Point pointOne, Point pointTwo)
      {
        int offsetTowardsOneX = pointOne.X - pointTwo.X;
        int offsetTowardsOneY = pointOne.Y - pointTwo.Y;
        float gradient = (float)offsetTowardsOneY / (float)offsetTowardsOneX;

        float pointOneReachX = pointOne.X;
        float pointOneReachY = pointOne.Y;
        int increment = Math.Sign(offsetTowardsOneX);
        while (pointOneReachX >= 0 && pointOneReachX <= c.Width && pointOneReachY >= 0 && pointOneReachY <= c.Height)
        {
          pointOneReachX += increment;
          pointOneReachY += increment * gradient;
        }

        float pointTwoReachX = pointTwo.X;
        float pointTwoReachY = pointTwo.Y;
        increment = -increment;
        while (pointTwoReachX >= 0 && pointTwoReachX <= c.Width && pointTwoReachY >= 0 && pointTwoReachY <= c.Height)
        {
          pointTwoReachX += increment;
          pointTwoReachY += increment * gradient;
        }
        return (new Point((int)pointOneReachX, (int)pointOneReachY), new Point((int)pointTwoReachX, (int)pointTwoReachY));
      }

      // the number of lines on the smaller size of the canvas will be adjusted accordingly
      bool isWidthLarger = (c.Width > c.Height) ? true : false;
      int spacesOnLargerDimensionExceptLines = isWidthLarger ? c.Width - numberOfLinesOnLargerDimension * (int)penWidth : c.Height - numberOfLinesOnLargerDimension * (int)penWidth;
      int averageDistance = spacesOnLargerDimensionExceptLines / (numberOfLinesOnLargerDimension + 1);
      int numberOfLinesOnSmallerDimension = isWidthLarger ? c.Height / (averageDistance + (int)penWidth) : c.Width / (averageDistance + (int)penWidth);


      Random r = (seed == 0) ? new Random() : new Random(seed);

      // consider only grids which is surrounded by 4 outmost lines, and (int, int) is the coordinate of the grid, (bool,bool) indicates if the grid's right and down border is erased
      var lineDictionary = new Dictionary<(int,int), (bool, bool)>();

      var lineDistanceOnLargerDimenion = new int[numberOfLinesOnLargerDimension];
      var lineDistanceOnSmallerDimenion = new int[numberOfLinesOnSmallerDimension];

      int[][] lineDistance = { lineDistanceOnLargerDimenion, lineDistanceOnSmallerDimenion };
      var dimensions = new int[]{numberOfLinesOnLargerDimension, numberOfLinesOnSmallerDimension};

      // combine the two above functions for convinience
      void rotateAndClampALine (Point A, Point B, int rotationDegree)
      {
        A = rotatePoint(rotationDegree, A, canvasOrigin);
        B = rotatePoint(rotationDegree, B, canvasOrigin);
        (A, B) = clampLine(A, B);

        c.Line(A.X, A.Y, B.X, B.Y);
      }

      // rotate only the points
      void rotateOnlyPoints (Point A, Point B, int rotationDegree)
      {
        A = rotatePoint(rotationDegree, A, canvasOrigin);
        B = rotatePoint(rotationDegree, B, canvasOrigin);

        c.Line(A.X, A.Y, B.X, B.Y);
      }

      // draw the vertial and horizontal lines
      void drawGrids ()
      {
        Point A = new Point();
        Point B = new Point();
        for (int i = 0; i < dimensions.Length; i++)
        {
          // first line
          int accumulatedDifference = 0;
          int accumulatedLineSpace = 0;
          int lineSpace = r.Next(1, averageDistance + 1);
          lineDistance[i][0] = lineSpace;
          int difference = averageDistance - lineSpace;
          accumulatedDifference += difference;
          accumulatedLineSpace += lineSpace + (int)penWidth / 2;
          A.X = (1 - i) * accumulatedLineSpace + i * 0;
          A.Y = (1 - i) * 0 + i * accumulatedLineSpace;
          B.X = (1 - i) * accumulatedLineSpace + i * c.Width;
          B.Y = (1 - i) * c.Height + i * accumulatedLineSpace;
          rotateAndClampALine(A, B, degree);

          // second to second to last line
          for (int l = 1; l < dimensions[i] - 1; l++)
          {
            lineSpace = r.Next(1, averageDistance + accumulatedDifference + 1);
            lineDistance[i][l] = lineSpace;
            difference = averageDistance - lineSpace;
            accumulatedDifference += difference;
            accumulatedLineSpace += lineSpace + (int)penWidth;
            A.X = (1 - i) * accumulatedLineSpace + i * 0;
            A.Y = (1 - i) * 0 + i * accumulatedLineSpace;
            B.X = (1 - i) * accumulatedLineSpace + i * c.Width;
            B.Y = (1 - i) * c.Height + i * accumulatedLineSpace;
            rotateAndClampALine(A, B, degree);
          }
          //last line
          lineSpace = accumulatedDifference + r.Next(1, averageDistance);
          lineDistance[i][dimensions[i] - 1] = lineSpace;
          accumulatedLineSpace += lineSpace;
          A.X = (1 - i) * accumulatedLineSpace + i * 0;
          A.Y = (1 - i) * 0 + i * accumulatedLineSpace;
          B.X = (1 - i) * accumulatedLineSpace + i * c.Width;
          B.Y = (1 - i) * c.Height + i * accumulatedLineSpace;
          rotateAndClampALine(A, B, degree);

        }
      }
      drawGrids();


      int horizontalNumberOfLines = isWidthLarger? numberOfLinesOnLargerDimension : numberOfLinesOnSmallerDimension;
      int vertialNumberOfLines = isWidthLarger ? numberOfLinesOnSmallerDimension : numberOfLinesOnLargerDimension;
      var horizontalLineDistance = isWidthLarger? lineDistanceOnLargerDimenion : lineDistanceOnSmallerDimenion;
      var verticalLineDistance = isWidthLarger? lineDistanceOnSmallerDimenion : lineDistanceOnLargerDimenion;

      int totalNumberOfSquare = (numberOfLinesOnLargerDimension - 1) * (numberOfLinesOnSmallerDimension - 1);
      int numberOfSquaresToBeModified = (totalNumberOfSquare / 9) == 0 ? 1 : (totalNumberOfSquare / 9);

      // number of squres that are colored increase with time
      numberOfSquaresToBeModified = (int)(numberOfSquaresToBeModified * timeNorm);

      // randomly select the coordinates of the squares to be modified
      for (int l = 0; l < numberOfSquaresToBeModified; l++)
      {
        int gi = r.Next(2, horizontalNumberOfLines);
        int gj = r.Next(2, vertialNumberOfLines);
        while (true)
        {
          if (gi >= 1 && gi <= horizontalNumberOfLines && gj >= 1 && gj <= vertialNumberOfLines)
          {
            bool rightOrDown = r.Next(1, 101) > 50 ? true : false;
            lineDictionary[(gi, gj)] = (rightOrDown, !rightOrDown);
            break;
          }
          gi = r.Next(2, horizontalNumberOfLines);
          gj = r.Next(2, vertialNumberOfLines);
        }
      }

      // get one coordinate of the square
      int getCoordiante (int num, int[] anylineDistance)
      {
        int coord = 0;
        for (int l = 0; l < num; l++)
        {
          coord += anylineDistance[l] + (int)penWidth;
        }
        return coord;
      }

      Random r2 = (seed == 0) ? new Random() : new Random(seed + 1);
      // erase one border and fill in the color
      var colors = new Color[]{ Color.Yellow, Color.Red, Color.Blue};
      for (int l = 1; l < horizontalNumberOfLines; l++)
      {
        for (int m = 1; m < vertialNumberOfLines; m++)
        {
          Point A = new Point();
          Point B = new Point();
          if (lineDictionary.ContainsKey((l, m)))
          {
            (bool right, bool down) = lineDictionary[(l, m)];
            int gx, gy;
            if (right)
            {
              // erase right border
              gx = getCoordiante(l, horizontalLineDistance);
              gy = getCoordiante(m, verticalLineDistance);
              gx -= (int)penWidth / 2;
              gy -= (int)penWidth;
              //int colorIndex = r2.Next(0, colors.Length);
              //c.SetColor(colors[colorIndex]);
              c.SetColor(colors[(l + m) % 3]);
              c.SetPenWidth(penWidth + 1);
              A.X = gx;
              A.Y = gy;
              B.X = gx;
              B.Y = gy - verticalLineDistance[m - 1];
              rotateOnlyPoints(A, B, degree);
              c.SetPenWidth(penWidth);
              // fill the space which was divided by the border
              gx -= (int)penWidth / 2 + horizontalLineDistance[l - 1];
              gy -= verticalLineDistance[m - 1] / 2;
              c.SetPenWidth(verticalLineDistance[m - 1] + 1);
              A.X = gx;
              A.Y = gy;
              B.X = gx + horizontalLineDistance[l - 1] + horizontalLineDistance[l] + (int)penWidth;
              B.Y = gy;
              rotateOnlyPoints(A, B, degree);
              c.SetPenWidth(penWidth);

            }
            else if (down)
            {
              // erase down border
              gx = getCoordiante(l, horizontalLineDistance);
              gy = getCoordiante(m, verticalLineDistance);
              gx -= (int)penWidth;
              gy -= (int)penWidth / 2;
              int colorIndex = r2.Next(0, colors.Length);
              //c.SetColor(colors[colorIndex]);
              //c.SetPenWidth(penWidth + 1);
              c.SetColor(colors[(l + m) % 3]);
              
              A.X = gx;
              A.Y = gy;
              B.X = gx - horizontalLineDistance[l - 1];
              B.Y = gy;
              rotateOnlyPoints(A, B, degree);
              c.SetPenWidth(penWidth);
              // fill the space which was divided by the border
              gx -= horizontalLineDistance[l - 1] / 2;
              gy -= (int)penWidth / 2 + verticalLineDistance[m - 1];
              c.SetPenWidth(horizontalLineDistance[l - 1] + 1);
              A.X = gx;
              A.Y = gy;
              B.X = gx;
              B.Y = gy + verticalLineDistance[m - 1] + verticalLineDistance[m] + (int)penWidth;
              rotateOnlyPoints(A, B, degree);
              c.SetPenWidth(penWidth);
            }
          }
        }
      }
      

      // }}
    }
  }
}
