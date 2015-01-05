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

			Console.WriteLine("Font debugging");
			{
				string Strr = "Hello\tTabs!\n\tHello More tabs!";

				Flib.Font F = new Flib.Font("FreeSans.ttf");
				Bitmap Map = F.RenderString(Strr, Color.Black, Color.White);
				Map.Save("test.png", ImageFormat.Png);
				Map.Dispose();

				Console.WriteLine("Done");
				return;
			}
		}
	}
}