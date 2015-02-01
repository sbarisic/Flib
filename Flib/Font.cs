using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SharpFont;

namespace Flib {
	public struct GlyphMetrics {
		public char Glyph;
		public int Kerning, Descent, AdvanceX, AdvanceY, Height, Width, StringIdx;

		public GlyphMetrics(char C, int Kerning, int Descent, int AdvanceX, int AdvanceY, int Height, int Width, int StringIdx) {
			Glyph = C;
			this.Kerning = Kerning;
			this.Descent = Descent;
			this.AdvanceX = AdvanceX;
			this.AdvanceY = AdvanceY;
			this.Height = Height;
			this.Width = Width;
			this.StringIdx = StringIdx;
		}
	}

	public class Font {
		static Library Library = new SharpFont.Library();

		Face F;
		GlyphSlot Glyph {
			get {
				return F.Glyph;
			}
		}

		FTVector GetKerning(char Left, char Right, KerningMode M = KerningMode.Default) {
			return F.GetKerning(F.GetCharIndex(Left), F.GetCharIndex(Right), M);
		}

		void LoadGlyph(char C) {
			F.LoadGlyph(F.GetCharIndex(C), LoadFlags.Default, LoadTarget.Normal);
		}

		void BuildFontAtlas(Color Fore, Color Back) {
			RectanglePack RP = new RectanglePack();
			HashSet<char> AddedChars = new HashSet<char>();

			Action<char> AddGlyph = (Chr) => {
				if (AddedChars.Contains(Chr))
					return;
				AddedChars.Add(Chr);

				GlyphMetrics M = GlyphMetrics(Chr);
				RP.Add(Chr, new Vec(M.Width + DFOffset * 2, M.Height + DFOffset * 2));
			};

			for (char c = (char)33; c < 127; c++)
				AddGlyph(c);

			Pack = RP.Pack();
			FontAtlas = new Bitmap((int)RP.Size.X, (int)RP.Size.Y);

			using (Graphics G = Graphics.FromImage(FontAtlas)) {
				G.Clear(Back);

				foreach (char C in Pack.Keys) {
					Rect R = Pack[C];
					LoadGlyph(C);
					Glyph.RenderGlyph(RenderMode.Normal);

					if (Glyph.Bitmap.Width != 0)
						G.DrawImageUnscaled(Glyph.Bitmap.ToGdipBitmap(Fore), (int)R.X, (int)R.Y);
				}
			}

			if (DistanceField)
				FontAtlas = Flib.DistanceField.Generate(FontAtlas, DFOffset);
		}

		int SpaceWidth;

		public Bitmap FontAtlas;
		Dictionary<char, Rect> Pack;

		bool DistanceField;
		int DFOffset;

		public int TabSize;
		public string Family;
		public float LineSpacing;
		public int Size {
			get;
			private set;
		}

		public object Userdata;

		public Font(byte[] Face, int Size = 16, bool DistanceField = false, int DFOffset = 5) {
			F = Library.NewMemoryFace(Face, 0);
			F.SelectCharmap(Encoding.Unicode);
			F.SetCharSize(Size << 6, Size << 6, 96, 96);

			this.DistanceField = DistanceField;
			this.DFOffset = DFOffset;

			LineSpacing = 1.35f;
			TabSize = 4;
			this.Size = Size;

			Family = F.FamilyName != null ? F.FamilyName : "";

			SpaceWidth = GlyphMetrics(' ', null).AdvanceX;
			BuildFontAtlas(Color.White, Color.Transparent);

			Userdata = null;
		}

		public Font(string Path, int Size = 16, bool DistanceField = false, int DFOffset = 5)
			: this(File.ReadAllBytes(Path), Size, DistanceField, DFOffset) {
		}

		public GlyphMetrics GlyphMetrics(char Current, char? Previous = null) {
			LoadGlyph(Current);
			int Descent = (Glyph.Metrics.Height >> 6) - (Glyph.Metrics.HorizontalBearingY >> 6);
			int Kerning = Glyph.Metrics.HorizontalBearingX >> 6;

			if (F.HasKerning && Previous.HasValue)
				Kerning += GetKerning(Previous.Value, Current).X >> 6;
			return new GlyphMetrics(Current, Kerning, Descent, Glyph.Advance.X >> 6, Glyph.Advance.Y >> 6,
				(Glyph.Metrics.Height >> 6), (Glyph.Metrics.Width >> 6), 0);
		}

		public int GetRelativeX(GlyphMetrics M) {
			return M.Kerning;
		}

		public int GetRelativeY(GlyphMetrics M) {
			return Size + M.Descent - M.Height;
		}

		public delegate void IteratorFunc(GlyphMetrics M, int X, int Y);
		public void Iterate(string Str, IteratorFunc A) {
			int X = 0;
			int Y = 0;
			GlyphMetrics M;

			for (int i = 0; i < Str.Length; i++) {
				switch (Str[i]) {
					case '\r':
					case '\b':
						continue;

					case '\t':
						X += SpaceWidth * TabSize;
						continue;

					case '\n':
						X = 0;
						Y += (int)(Size * LineSpacing);
						continue;

					default:
						M = GlyphMetrics(Str[i], (i > 0 ? (char?)Str[i - 1] : null));
						M.StringIdx = i;
						A(M, X + GetRelativeX(M), Y + GetRelativeY(M));
						X += M.AdvanceX;
						Y += M.AdvanceY;
						continue;
				}
			}
		}

		public bool GetPack(char C, out int X, out int Y, out int W, out int H) {
			X = Y = W = H = 0;
			if (Pack.ContainsKey(C)) {
				Rect R = Pack[C];
				X = R.X;
				Y = R.Y;
				W = R.W;
				H = R.H;

				if (DistanceField) {
					X -= DFOffset;
					Y -= DFOffset;
				} else {
					W -= DFOffset * 2;
					H -= DFOffset * 2;
				}
				return true;
			}
			return false;
		}

		public bool GetPackUV(char C, out float U, out float V, out float W, out float H) {
			int _U, _V, _W, _H;
			if (!GetPack(C, out _U, out _V, out _W, out _H)) {
				U = V = W = H = 0;
				return false;
			}

			U = (float)_U / FontAtlas.Width;
			V = (float)_V / FontAtlas.Height;
			W = (float)_W / FontAtlas.Width;
			H = (float)_H / FontAtlas.Height;
			return true;
		}

		public void MeasureString(string S, out int W, out int H) {
			int _W = 0;
			int _H = 0;
			Iterate(S, (M, X, Y) => {
				_W = Math.Max(_W, X + M.AdvanceX);
				_H = Math.Max(_H, Y + M.AdvanceY + M.Height);
			});
			W = _W;
			H = _H;
		}

		public override string ToString() {
			return string.Format("Font({0})", Family);
		}
	}
}