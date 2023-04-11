using System;
using System.Collections.Generic;
using System.Drawing;
using LineCanvas;
using Utilities;

namespace _092lines
{
  public class Lines
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    /// <param name="name">Your first-name and last-name.</param>
    /// <param name="wid">Initial image width in pixels.</param>
    /// <param name="hei">Initial image height in pixels.</param>
    /// <param name="param">Optional text to initialize the form's text-field.</param>
    /// <param name="tooltip">Optional tooltip = param help.</param>
    public static void InitParams (out string name, out int wid, out int hei, out string param, out string tooltip)
    {
      // {{

      // Put your name here.
      name = "Josef Pelikán";

      // Image size in pixels.
      wid = 1600;
      hei = 900;

      // Specific animation params.
      param = "width=10.0,anti=true,numberOfLinesOnLargerDimension=30,prob=0.95,seed=12";

      // Tooltip = help.
      tooltip = "width=<int>, anti[=<bool>], numberOfLinesOnLargerDimension=<int>, hatches=<int>, prob=<float>, seed=<int>";

      // }}
    }

    /// <summary>
    /// Draw the image into the initialized Canvas object.
    /// </summary>
    /// <param name="c">Canvas ready for your drawing.</param>
    /// <param name="param">Optional string parameter from the form.</param>
    public static void Draw (Canvas c, string param)
    {
      // {{ TODO: put your drawing code here

      // Input params.
      float penWidth = 1.0f;   // pen width
      bool antialias = false;  // use anti-aliasing?
      int numberOfLinesOnLargerDimension    = 10;    // number of randomly generated objects (squares, stars, Brownian particles)
      int hatches    = 12;     // number of hatch-lines for the squares
      double prob    = 0.95;   // continue-probability for the Brownian motion simulator
      int seed       = 12;     // random generator seed

      Dictionary<string, string> p = Util.ParseKeyValueList(param);
      if (p.Count > 0)
      {
        // with=<line-width>
        if (Util.TryParse(p, "width", ref penWidth))
        {
          if (penWidth < 0.0f)
            penWidth = 0.0f;
        }

        // anti[=<bool>]
        Util.TryParse(p, "anti", ref antialias);

        // squares=<number>
        if (Util.TryParse(p, "numberOfLinesOnLargerDimension", ref numberOfLinesOnLargerDimension) &&
            numberOfLinesOnLargerDimension < 0)
          numberOfLinesOnLargerDimension = 0;

        // hatches=<number>
        if (Util.TryParse(p, "hatches", ref hatches) &&
            hatches < 1)
          hatches = 1;

        // prob=<probability>
        if (Util.TryParse(p, "prob", ref prob) &&
            prob > 0.999)
          prob = 0.999;

        // seed=<int>
        Util.TryParse(p, "seed", ref seed);
      }

      int wq = c.Width / 4;
      int hq = c.Height / 4;
      int wh = wq + wq;
      int hh = hq + hq;
      int minh = Math.Min(wh, hh);
      int maxh = Math.Max(wh, hh);
      double t;
      int i, j;
      double cx, cy, angle, x, y;

      c.Clear(Color.White);

      // 1st quadrant - star.

      c.SetPenWidth(penWidth);
      c.SetAntiAlias(antialias);
      c.SetColor(Color.Black);


      // the number of objects on the smaller size of the screen will be adjusted accordingly
      bool isWidthLarger = (c.Width > c.Height) ? true : false;
      int spacesOnLargerDimensionExceptLines = isWidthLarger ? c.Width - numberOfLinesOnLargerDimension * (int)penWidth : c.Height - numberOfLinesOnLargerDimension * (int)penWidth;
      int averageDistance = spacesOnLargerDimensionExceptLines / (numberOfLinesOnLargerDimension + 1);
      int numberOfLinesOnSmallerDimension = isWidthLarger ? (c.Height / averageDistance - 1) : (c.Width / averageDistance - 1);

      
      Random r = (seed == 0) ? new Random() : new Random(seed);

      // consider only grids with 4 borders, and (int, int) is the coordinate of the grid, (bool,bool) indicates if the grid's right and down border is erased
      var lineDictionary = new Dictionary<(int,int), (bool, bool)>();

      var lineDistanceOnLargerDimenion = new int[numberOfLinesOnLargerDimension];
      var lineDistanceOnSmallerDimenion = new int[numberOfLinesOnSmallerDimension];

      // first line
      int accumulatedDifference = 0;
      int accumulatedLineSpace = 0;
      int lineSpace = r.Next(1, averageDistance + 1);
      lineDistanceOnLargerDimenion[0] = lineSpace;
      int difference = averageDistance - lineSpace;
      accumulatedDifference += difference;
      accumulatedLineSpace += lineSpace + (int)penWidth / 2;
      c.Line(accumulatedLineSpace, 0, accumulatedLineSpace, c.Height);

      // second to second to last line
      for (int l = 1; l < numberOfLinesOnLargerDimension - 1; l++)
      {
        lineSpace = r.Next(1, averageDistance + accumulatedDifference + 1);
        lineDistanceOnLargerDimenion[l] = lineSpace;
        difference = averageDistance - lineSpace;
        accumulatedDifference += difference;
        accumulatedLineSpace += lineSpace + (int)penWidth;
        c.Line(accumulatedLineSpace, 0, accumulatedLineSpace, c.Height);
      }
      //last line
      lineSpace = accumulatedDifference + r.Next(1, averageDistance);
      lineDistanceOnLargerDimenion[numberOfLinesOnLargerDimension - 1] = lineSpace;
      accumulatedLineSpace += lineSpace;
      //Console.WriteLine("accumulatedLineSpace:" + accumulatedLineSpace);
      c.Line(accumulatedLineSpace, 0, accumulatedLineSpace, c.Height);


      // first line
      accumulatedDifference = 0;
      accumulatedLineSpace = 0;
      lineSpace = r.Next(1, averageDistance + 1);
      lineDistanceOnSmallerDimenion[0] = lineSpace;
      difference = averageDistance - lineSpace;
      accumulatedDifference += difference;
      accumulatedLineSpace += lineSpace + (int)penWidth / 2;
      //Console.WriteLine(accumulatedLineSpace);
      c.Line(0, accumulatedLineSpace, c.Width, accumulatedLineSpace);

      // second to second to last line
      for (int l = 1; l < numberOfLinesOnSmallerDimension - 1; l++)
      {
        lineSpace = r.Next(1, averageDistance + accumulatedDifference + 1);
        lineDistanceOnSmallerDimenion[l] = lineSpace;
        difference = averageDistance - lineSpace;
        accumulatedDifference += difference;
        accumulatedLineSpace += lineSpace + (int)penWidth;
        //Console.WriteLine(accumulatedLineSpace);
        c.Line(0, accumulatedLineSpace, c.Width, accumulatedLineSpace);
      }
      //last line
      lineSpace = accumulatedDifference + r.Next(1, averageDistance);
      lineDistanceOnSmallerDimenion[numberOfLinesOnSmallerDimension - 1] = lineSpace;
      accumulatedLineSpace += lineSpace;
      //Console.WriteLine("accumulatedLineSpace:" + accumulatedLineSpace);
      c.Line(0, accumulatedLineSpace, c.Width, accumulatedLineSpace);

      /*
      accumulatedDifference = 0;
      accumulatedLineSpace = 0;
      for (int l = 0; l < numberOfLinesOnSmallerDimension; l++)
      {
        int lineSpace = r.Next(averageDistance - averageDistance / 2 + accumulatedDifference, averageDistance + accumulatedDifference + 1);
        int difference = averageDistance - lineSpace;
        accumulatedDifference += difference;
        if (l == 0)
        {
          accumulatedLineSpace += lineSpace + (int)penWidth / 2;
        }
        else
        {
          accumulatedLineSpace += lineSpace + (int)penWidth + (int)penWidth / 2;
        }

        c.Line(0, accumulatedLineSpace, c.Width, accumulatedLineSpace);
      }
      *
      */

      int horizontalNumberOfLines = isWidthLarger? numberOfLinesOnLargerDimension : numberOfLinesOnSmallerDimension;
      int vertialNumberOfLines = isWidthLarger ? numberOfLinesOnSmallerDimension : numberOfLinesOnLargerDimension;
      var horizontalLineDistance = isWidthLarger? lineDistanceOnLargerDimenion : lineDistanceOnSmallerDimenion;
      var verticalLineDistance = isWidthLarger? lineDistanceOnSmallerDimenion : lineDistanceOnLargerDimenion;

      int totalNumberOfSquare = (numberOfLinesOnLargerDimension - 1) * (numberOfLinesOnSmallerDimension - 1);
      int numberOfSquaresToBeModified = (totalNumberOfSquare / 12) == 0 ? 1 : (totalNumberOfSquare / 12);


      for (int l = 0; l < numberOfSquaresToBeModified; l++)
      {
        int gi = r.Next(2, horizontalNumberOfLines);
        int gj = r.Next(2, vertialNumberOfLines - 1);
        while (true)
        {
          /*
          if(
             (gi == 1 &&
              gj + 1 <= vertialNumberOfLines && !lineDictionary.ContainsKey((gi, gj + 1)) &&
              gj - 1 >= 1 && !lineDictionary.ContainsKey((gi, gj - 1)) &&
              !lineDictionary.ContainsKey((gi, gj)))
             ||
             (gj == 1 &&
              gi + 1 <= horizontalNumberOfLines && !lineDictionary.ContainsKey((gi + 1, gj)) &&
              gj - 1 >= 1 && !lineDictionary.ContainsKey((gi, gj - 1)) &&
              !lineDictionary.ContainsKey((gi, gj)))
             ||
             (gi == horizontalNumberOfLines &&
              gj + 1 <= vertialNumberOfLines && !lineDictionary.ContainsKey((gi, gj + 1)) &&
              gj - 1 >= 1 && !lineDictionary.ContainsKey((gi, gj - 1)) &&
              !lineDictionary.ContainsKey((gi, gj)))
             ||
             (gj == vertialNumberOfLines &&
              gi + 1 <= horizontalNumberOfLines && !lineDictionary.ContainsKey((gi + 1, gj)) &&
              gi - 1 >= 1 && !lineDictionary.ContainsKey((gi + 1, gj)) &&
              !lineDictionary.ContainsKey((gi, gj)))
            )
          */
          if(gi >= 1 && gi <= horizontalNumberOfLines && gj >= 1 && gj <= vertialNumberOfLines)
          {
            bool rightOrDown = r.Next(1, 101) > 50 ? true : false;
            lineDictionary[(gi, gj)] = (rightOrDown, !rightOrDown);
            break;
          }
          gi = r.Next(2, horizontalNumberOfLines);
          gj = r.Next(2, vertialNumberOfLines - 1);
        }
      }

      int lineNumber = 4;
      int columnNumber = 4;
      lineDictionary[(lineNumber, columnNumber)] = (true, false);

      

      int getCoordiante (int num, int[] anylineDistance)
      {
        int coord = 0;
        for (int l = 0; l < num; l++)
        {
          coord += anylineDistance[l] + (int)penWidth;
        }
        return coord;
      }

      var colors = new Color[]{ Color.Yellow, Color.Red, Color.Blue};
      for (int l = 1; l < horizontalNumberOfLines; l++)
      {
        for (int m = 1; m < vertialNumberOfLines; m++)
        {
          if (lineDictionary.ContainsKey((l, m)))
          {
            (bool right, bool down) = lineDictionary[(l, m)];
            //Console.WriteLine(right);
            //Console.WriteLine(down);
            //Console.WriteLine(l);
            //Console.WriteLine(m);
            int gx, gy;
            
            if (right)
            {
              
              gx = getCoordiante(l, horizontalLineDistance);
              gy = getCoordiante(m,verticalLineDistance);
              gx -= (int)penWidth / 2;
              gy -= (int)penWidth;
              //gx =(int)penWidth / 2;
              //gy -= (int)penWidth / 2;
              //Console.WriteLine(gx);
              //Console.WriteLine(gy);
              //gy += verticalLineDistance[l - 1] / 2;
              int colorIndex = r.Next(0, colors.Length);
              c.SetColor(colors[colorIndex]);
              c.SetPenWidth(penWidth + 1);
              //c.SetPenWidth(verticalLineDistance[l - 1] / 2);
              //Console.WriteLine(verticalLineDistance[m - 1]);
              c.Line(gx, gy, gx, gy - verticalLineDistance[m - 1]);
              c.SetPenWidth(penWidth);

              gx -= (int)penWidth / 2 + horizontalLineDistance[l - 1];
              gy -= verticalLineDistance[m - 1] / 2;
              c.SetPenWidth(verticalLineDistance[m - 1] + 1);
              c.Line(gx, gy, gx + horizontalLineDistance[l - 1] + horizontalLineDistance[l] + penWidth, gy);
              c.SetPenWidth(penWidth);

            }
            
            
            else if (down)
            {
              gx = getCoordiante(l, horizontalLineDistance);
              gy = getCoordiante(m, verticalLineDistance);
              gx -= (int)penWidth;
              gy -= (int)penWidth / 2;
              int colorIndex = r.Next(0, colors.Length);
              c.SetColor(colors[colorIndex]);
              //c.SetColor(Color.Green);
              c.SetPenWidth(penWidth + 1);
              c.Line(gx, gy, gx - horizontalLineDistance[l - 1], gy);
              c.SetPenWidth(penWidth);

              gx -= horizontalLineDistance[l - 1] / 2;
              gy -= (int)penWidth / 2 + verticalLineDistance[m - 1];
              c.SetPenWidth(horizontalLineDistance[l - 1] + 1);
              c.Line(gx, gy, gx, gy + verticalLineDistance[m - 1] + verticalLineDistance[m] + penWidth);
              c.SetPenWidth(penWidth);
            }
            
          }
        }
      }

      

      

      //Point A = new Point(averageDistance * (l + 1) + (int)penWidth * l + (int)penWidth / 2, 0);
      //Point B = new Point(averageDistance * (l + 1) + (int)penWidth * l + (int)penWidth / 2, c.Height);

      //c.Line(0, c.Height/2, c.Width, c.Height/2);

      Point canvasOrigin = new Point(c.Width / 2, c.Height / 2);

      Point rotatePoint (float degree, Point point, Point center)
      {
        Point pointRelativeToCenter = new Point(point.X - center.X, point.Y - center.Y);
        var radian = degree * System.Math.PI / 180.0;
        float cos = (float)Math.Cos(radian);
        float sin = (float)Math.Sin(radian);
        Point newPointRelativeToCenter = new Point((int)(pointRelativeToCenter.X * cos - pointRelativeToCenter.Y * sin), (int)(pointRelativeToCenter.X * sin + pointRelativeToCenter.Y * cos));
        Point newPointBack = new Point(newPointRelativeToCenter.X + center.X, newPointRelativeToCenter.Y + center.Y);
        return newPointBack;
      }

      //Point A = rotatePoint(30, new Point(0, c.Height/2), canvasOrigin);
      //Point B = rotatePoint(30, new Point(c.Width, c.Height/2), canvasOrigin);

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
        //gradient = -gradient;
        while (pointTwoReachX >= 0 && pointTwoReachX <= c.Width && pointTwoReachY >= 0 && pointTwoReachY <= c.Height)
        {
          pointTwoReachX += increment;
          pointTwoReachY += increment * gradient;
        }

        return (new Point((int)pointOneReachX, (int)pointOneReachY), new Point((int)pointTwoReachX, (int)pointTwoReachY));
      }

      //(A, B) = clampLine(A, B);
      //c.Line(A.X, A.Y, B.X, B.Y);

      
      /*
      for (int l = 0; l < numberOfLinesOnLargerDimension; l ++)
      {
        Point A = new Point(averageDistance * (l + 1) + (int)penWidth * l + (int)penWidth / 2, 0);
        Point B = new Point(averageDistance * (l + 1) + (int)penWidth * l + (int)penWidth / 2, c.Height);

        
        A = rotatePoint(30, A, canvasOrigin);
        B = rotatePoint(30, B, canvasOrigin);
        (A, B) = clampLine(A, B);
        
        c.Line(A.X, A.Y, B.X, B.Y);
        
      }

      
      for (int l = 0; l < numberOfLinesOnSmallerDimension; l++)
      {
        Point A = new Point(0, averageDistance * (l + 1) + (int)penWidth * l + (int)penWidth / 2);
        Point B = new Point(c.Width, averageDistance * (l + 1) + (int)penWidth * l + (int)penWidth / 2);
        
        A = rotatePoint(30, A, canvasOrigin);
        B = rotatePoint(30, B, canvasOrigin);
        (A, B) = clampLine(A, B);
        
        c.Line(A.X, A.Y, B.X, B.Y);
        
      }

      Point firstSqure = new Point (numberOfLinesOnLargerDimension / 2, numberOfLinesOnSmallerDimension / 5 * 2);
      Point secondSqure = new Point (numberOfLinesOnLargerDimension / 3 * 2, numberOfLinesOnSmallerDimension / 5 * 4);

      c.SetColor(Color.Blue);
      c.SetPenWidth(averageDistance);
      int firstX = (firstSqure.X - 1) * (averageDistance + (int)penWidth);
      int firstY = (firstSqure.Y - 1) * (averageDistance + (int)penWidth) + averageDistance / 2;
      Point firstA = new Point(firstX, firstY);
      Point firstB = new Point(firstX + averageDistance, firstY);

      
      firstA = rotatePoint(30, firstA, canvasOrigin);
      firstB = rotatePoint(30, firstB, canvasOrigin);
      //(firstA, firstB) = clampLine(firstA, firstB);
      
      c.Line(firstA.X, firstA.Y, firstB.X, firstB.Y);

      
      c.SetColor(Color.Red);
      c.SetPenWidth(averageDistance);
      int secondX = (secondSqure.X - 1) * (averageDistance + (int)penWidth);
      int secondY = (secondSqure.Y - 1) * (averageDistance + (int)penWidth) + averageDistance / 2;

      Point secondA = new Point(secondX, secondY);
      Point secondB = new Point(secondX + averageDistance, secondY);

      secondA = rotatePoint(30, secondA, canvasOrigin);
      secondB = rotatePoint(30, secondB, canvasOrigin);
      //(secondA, secondB) = clampLine(secondA, secondB);

      c.Line(secondA.X, secondA.Y, secondB.X, secondB.Y);
      
      */
      

      /*
      c.SetPenWidth(penWidth);
      c.SetAntiAlias(antialias);

      const int MAX_LINES = 30;
      for (i = 0, t = 0.0; i < MAX_LINES; i++, t += 1.0 / MAX_LINES)
      {
        c.SetColor(Color.FromArgb(i * 255 / MAX_LINES, 255, 255 - i * 255 / MAX_LINES)); // [0,255,255] -> [255,255,0]
        c.Line(t * wh, 0, wh - t * wh, hh);
      }
      for (i = 0, t = 0.0; i < MAX_LINES; i++, t += 1.0 / MAX_LINES)
      {
        c.SetColor(Color.FromArgb(255, 255 - i * 255 / MAX_LINES, i * 255 / MAX_LINES)); // [255,255,0] -> [255,0,255]
        c.Line(0, hh - t * hh, wh, t * hh);
      }
      

      // 2nd quadrant - random hatched squares.
      double size = minh / 10.0;
      double padding = size * Math.Sqrt(0.5);
      c.SetColor(Color.LemonChiffon);
      c.SetPenWidth(1.0f);
      Random r = (seed == 0) ? new Random() : new Random(seed);

      for (i = 0; i < objects; i++)
      {
        do
          cx = r.NextDouble() * wh;
        while (cx < padding ||
               cx > wh - padding);

        c.SetAntiAlias(cx > wq);
        cx += wh;

        do
          cy = r.NextDouble() * hh;
        while (cy < padding ||
               cy > hh - padding);

        angle = r.NextDouble() * Math.PI;

        double dirx = Math.Sin(0) * size * 0.5;
        double diry = Math.Cos(0) * size * 0.5;
        cx -= dirx - diry;
        cy -= diry + dirx;
        double dx = -diry * 2.0 / hatches;
        double dy = dirx * 2.0 / hatches;
        double linx = dirx + dirx;
        double liny = diry + diry;

        for (j = 0; j++ < hatches; cx += dx, cy += dy)
          c.Line(cx, cy, cx + linx, cy + liny);
      }

      // 3rd quadrant - random stars.
      c.SetColor(Color.LightCoral);
      c.SetPenWidth(penWidth);
      size = minh / 16.0;
      padding = size;
      const int MAX_SIDES = 30;
      List<PointF> v = new List<PointF>(MAX_SIDES + 1);

      for (i = 0; i < objects; i++)
      {
        do
          cx = r.NextDouble() * wh;
        while (cx < padding ||
               cx > wh - padding);

        c.SetAntiAlias(cx > wq);

        do
          cy = r.NextDouble() * hh;
        while (cy < padding ||
               cy > hh - padding);
        cy += hh;

        int sides = r.Next(3, MAX_SIDES);
        double dAngle = Math.PI * 2.0 / sides;

        v.Clear();
        angle = 0.0;

        for (j = 0; j++ < sides; angle += dAngle)
        {
          double rad = size * (0.1 + 0.9 * r.NextDouble());
          x = cx + rad * Math.Sin(angle);
          y = cy + rad * Math.Cos(angle);
          v.Add(new PointF((float)x, (float)y));
        }
        v.Add(v[0]);
        c.PolyLine(v);
      }

      // 4th quadrant - Brownian motion.
      c.SetPenWidth(penWidth);
      c.SetAntiAlias(true);
      size = minh / 10.0;
      padding = size;

      for (i = 0; i < objects; i++)
      {
        do
          x = r.NextDouble() * wh;
        while (x < padding ||
               x > wh - padding);

        do
          y = r.NextDouble() * hh;
        while (y < padding ||
               y > hh - padding);

        c.SetColor(Color.FromArgb(127 + r.Next(0, 128),
                                  127 + r.Next(0, 128),
                                  127 + r.Next(0, 128)));

        for (j = 0; j++ < 1000;)
        {
          angle = r.NextDouble() * Math.PI * 2.0;
          double rad = size * r.NextDouble();
          cx = x + rad * Math.Sin(angle);
          cy = y + rad * Math.Cos(angle);
          if (cx < 0.0 || cx > wh ||
              cy < 0.0 || cy > hh)
            break;

          c.Line(x + wh, y + hh, cx + wh, cy + hh);
          x = cx;
          y = cy;
          if (r.NextDouble() > prob)
            break;
        }
      }
      */

      // }}
    }
  }
}
