using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace FlibTest {
	class Program {
		static void Main(string[] args) {
			Console.Title = "Flib Test";

			Flib.Font F = new Flib.Font("FreeSans.ttf");

			string Str = "Hello font world! \"Quote\", IJK, ijk g";
			int W, H;
			F.MeasureString(Str, out W, out H);

			Bitmap Bmp = new Bitmap(W, H);
			using (Graphics G = Graphics.FromImage(Bmp)) {
				G.Clear(Color.Black);
				F.Iterate(Str, (M, X, Y) => {
					int x, y, w, h;
					F.GetPack(M.Glyph, out x, out y, out w, out h);
					G.DrawImage(F.FontAtlas, X, Y, new RectangleF(x, y, w, h), GraphicsUnit.Pixel);
				});
			}

			Bmp.Save("test.png", ImageFormat.Png);
			Console.WriteLine("Done {0}", F);
		}
	}
}