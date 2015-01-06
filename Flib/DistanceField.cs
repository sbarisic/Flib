using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Flib {
	public unsafe class DistanceField {
		static float Clamp(float Val, float Min, float Max) {
			if (Val > Max)
				return Max;
			if (Val < Min)
				return Min;
			return Val;
		}

		static BitmapData Lock(Bitmap B, ImageLockMode M) {
			return B.LockBits(new Rectangle(0, 0, B.Width, B.Height), M, PixelFormat.Format32bppArgb);
		}

		public static Bitmap Generate(Bitmap Src, int SearchDistance = 5) {
			Bitmap Dest = new Bitmap(Src.Width, Src.Height);

			BitmapData SrcData = Lock(Src, ImageLockMode.ReadOnly);
			BitmapData DstData = Lock(Dest, ImageLockMode.WriteOnly);
			byte* _Src = (byte*)SrcData.Scan0;
			byte* _Dst = (byte*)DstData.Scan0;
			int BPP = 4;

			Func<int, int, byte[]> GetPixel = (X, Y) => {
				byte[] Col = new byte[4];
				if (X < 0 || Y < 0 || X >= SrcData.Width || Y >= SrcData.Height)
					return Col;

				int I = (Y * SrcData.Width + X) * BPP;
				Col[0] = _Src[I];
				Col[1] = _Src[I + 1];
				Col[2] = _Src[I + 2];
				Col[3] = _Src[I + 3];
				return Col;
			};

			Action<int, int, byte[]> SetPixel = (X, Y, Col) => {
				int I = (Y * DstData.Width + X) * BPP;
				_Dst[I] = Col[0];
				_Dst[I + 1] = Col[1];
				_Dst[I + 2] = Col[2];
				_Dst[I + 3] = Col[3];
			};

			int R = 0;

			for (int x = 0; x < Dest.Width; ++x) {
				for (int y = 0; y < Dest.Height; ++y) {
					float A = (float)GetPixel(x, y)[R] / 255f;
					float Distance = float.MaxValue;

					int FXMin = Math.Max(x - SearchDistance, 0);
					int FXMax = Math.Min(x + SearchDistance, Src.Width);
					int FYMin = Math.Max(y - SearchDistance, 0);
					int FYMax = Math.Min(y + SearchDistance, Src.Height);

					for (int fx = FXMin; fx < FXMax; ++fx) {
						for (int fy = FYMin; fy < FYMax; ++fy) {
							float P = (float)GetPixel(fx, fy)[R] / 255f;

							// A != P
							if (A != P || A == 1.0f) {
								float xd = x - fx;
								float yd = y - fy;
								float d = (float)Math.Sqrt((xd * xd) + (yd * yd));

								if (Math.Abs(d) < Math.Abs(Distance))
									Distance = d;
							}
						}
					}

					if (Distance != float.MaxValue) {
						Distance = Clamp(Distance, -SearchDistance, +SearchDistance);
						A = 1f - Clamp((Distance + SearchDistance) / (SearchDistance + SearchDistance), 0, 1);
					}

					byte C = (byte)(A * 255);
					SetPixel(x, y, new byte[] { C, C, C, 255 });
				}
			}

			Src.UnlockBits(SrcData);
			Dest.UnlockBits(DstData);
			return Dest;
		}
	}
}