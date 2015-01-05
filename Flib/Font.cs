﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using SharpFont;

namespace Flib {
	public struct GlyphMetrics {
		public int Kerning, Descent, AdvanceX, AdvanceY, Height, Width;

		public GlyphMetrics(int Kerning, int Descent, int AdvanceX, int AdvanceY, int Height, int Width) {
			this.Kerning = Kerning;
			this.Descent = Descent;
			this.AdvanceX = AdvanceX;
			this.AdvanceY = AdvanceY;
			this.Height = Height;
			this.Width = Width;
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

		public string Family;
		public float LineSpacing;
		public int Size {
			get;
			private set;
		}

		public Font(string Path, int Size = 16) {
			F = Library.NewFace(Path, 0);
			F.SelectCharmap(Encoding.Unicode);
			F.SetCharSize(Size << 6, Size << 6, 96, 96);
			LineSpacing = 1.35f;
			this.Size = Size;

			Family = F.FamilyName != null ? F.FamilyName : "";
		}

		public uint GetGlyphIndex(char C) {
			return F.GetCharIndex(C);
		}

		public void MeasureChar(char C, out int W, out int H) {
			uint Index = GetGlyphIndex(C);
			F.LoadGlyph(Index, LoadFlags.Default, LoadTarget.Normal);
			W = Glyph.Advance.X >> 6;
			H = Glyph.Metrics.Height >> 6;
		}

		FTVector GetKerning(char Left, char Right, KerningMode M = KerningMode.Default) {
			return F.GetKerning(GetGlyphIndex(Left), GetGlyphIndex(Right), M);
		}

		public void LoadGlyph(char C) {
			F.LoadGlyph(GetGlyphIndex(C), LoadFlags.Default, LoadTarget.Normal);
		}

		public GlyphMetrics GlyphMetrics(char Current, char? Previous) {
			LoadGlyph(Current);
			int Descent = (Glyph.Metrics.Height >> 6) - (Glyph.Metrics.HorizontalBearingY >> 6);
			int Kerning = Glyph.Metrics.HorizontalBearingX >> 6;
			if (F.HasKerning && Previous.HasValue)
				Kerning += GetKerning(Previous.Value, Current).X >> 6;
			return new GlyphMetrics(Kerning, Descent, Glyph.Advance.X >> 6, Glyph.Advance.Y >> 6,
				Glyph.Metrics.Height >> 6, Glyph.Metrics.Width >> 6);
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
					case '\n':
						X = 0;
						Y += (int)(Size * LineSpacing);
						continue;
				}

				M = GlyphMetrics(Str[i], (i > 0 ? (char?)Str[i - 1] : null));
				A(M, X + GetRelativeX(M), Y + GetRelativeY(M));
				X += M.AdvanceX;
				Y += M.AdvanceY;
			}
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

		public Bitmap RenderString(string Txt, Color Fore, Color Back) {
			int W = 0;
			int H = 0;
			MeasureString(Txt, out W, out H);
			Bitmap Bmp = new Bitmap(W, H);
			Graphics G = Graphics.FromImage(Bmp);
			G.Clear(Back);
			Iterate(Txt, (M, X, Y) => {
				Glyph.RenderGlyph(RenderMode.Normal);
				if (Glyph.Bitmap.Width != 0)
					G.DrawImageUnscaled(Glyph.Bitmap.ToGdipBitmap(Fore), X, Y);
			});
			G.Dispose();
			return Bmp;
		}
	}
}