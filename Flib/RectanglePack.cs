using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flib {
	struct Rect {
		public int X, Y, W, H;
		public Rect(int X, int Y, int W, int H) {
			this.X = X;
			this.Y = Y;
			this.W = W;
			this.H = H;
		}

		public void SetPos(int X, int Y) {
			this.X = X;
			this.Y = Y;
		}

		public void SetSize(int W, int H) {
			this.W = W;
			this.H = H;
		}
	}

	struct Vec {
		public int X, Y;
		public Vec(int X, int Y) {
			this.X = X;
			this.Y = Y;
		}
	}

	class Spot {
		public Spot Insert(char ID, Vec Rect) {
			if (ChildA != null && ChildB != null) {
				Spot NewNode;
				if ((NewNode = ChildA.Insert(ID, Rect)) != null)
					return NewNode;
				return ChildB.Insert(ID, Rect);
			}

			if (Rect.X > Area.W || Rect.Y > Area.H)
				return null;

			ChildA = new Spot();
			ChildB = new Spot();

			int WidthDelta = Area.W - Rect.X;
			int HeightDelta = Area.H - Rect.Y;

			if (WidthDelta <= HeightDelta) {
				ChildA.Area.SetPos(Area.X + Rect.X, Area.Y);
				ChildA.Area.SetSize(WidthDelta, Rect.Y);
				ChildB.Area.SetPos(Area.X, Area.Y + Rect.Y);
				ChildB.Area.SetSize(Area.W, HeightDelta);
			} else {
				ChildA.Area.SetPos(Area.X, Area.Y + Rect.Y);
				ChildA.Area.SetSize(Rect.X, HeightDelta);
				ChildB.Area.SetPos(Area.X + Rect.X, Area.Y);
				ChildB.Area.SetSize(WidthDelta, Area.H);
			}

			Area.SetSize(Rect.X, Rect.Y);
			this.ID = ID;

			return this;
		}

		public Spot ChildA;
		public Spot ChildB;
		public Rect Area;
		public char ID;
	}

	unsafe class RectanglePack {
		Vec _Size;

		public Spot RootSpot;
		public bool AutoResize;
		public Vec Size {
			get {
				return _Size;
			}
			set {
				if (value.X <= 0 || value.Y <= 0)
					throw new Exception("Rectangle pack size must be higher than 0");

				if (RootSpot.ChildA != null && RootSpot.ChildB != null) {
					RectanglePack NewPack = new RectanglePack(value.X, value.Y);

					Action<Spot> AddSpots = null;
					AddSpots = (S) => {
						if (S.ChildA == null && S.ChildB == null)
							return;
						NewPack.Add(S.ID, new Vec(S.Area.W, S.Area.H));
						AddSpots(S.ChildA);
						AddSpots(S.ChildB);
					};
					AddSpots(RootSpot);
					RootSpot = NewPack.RootSpot;
				} else
					RootSpot.Area.SetSize(value.X, value.Y);

				_Size = value;
			}
		}

		public RectanglePack(int W, int H) {
			AutoResize = true;

			_Size = new Vec(W, H);
			RootSpot = new Spot();
			RootSpot.Area = new Rect(0, 0, W, H);
		}

		public RectanglePack()
			: this(150, 150) {
			AutoResize = true;
		}

		public bool Add(char ID, Vec V) {
			if (V.X == 0 || V.Y == 0)
				return true;
			if (RootSpot.Insert(ID, V) != null)
				return true;

			if (!AutoResize)
				return false;

			int WidthDelta = 0;
			int HeightDelta = 0;
			if (Size.X > Size.Y) {
				HeightDelta += V.Y;
				if (V.X > Size.X)
					WidthDelta += V.X;
			} else {
				WidthDelta += V.X;
				if (V.Y > Size.Y)
					HeightDelta += V.Y;
			}

			bool OldAutoResize = AutoResize;
			AutoResize = false;
			int MaxTries = 10;

			for (int Tries = 1; Tries < MaxTries; Tries++) {
				Size = new Vec(Size.X + WidthDelta * Tries, Size.Y + HeightDelta * Tries);
				if (Add(ID, V))
					break;
				else if (Tries >= MaxTries)
					throw new Exception("Can not fit!");
			}

			AutoResize = OldAutoResize;
			return true;
		}

		delegate void AddSpotsFunc(Spot S);

		public Dictionary<char, Rect> Pack() {
			Dictionary<char, Rect> Packed = new Dictionary<char, Rect>();

			AddSpotsFunc AddSpots = null;
			AddSpots = (S) => {
				if (S.ChildA == null && S.ChildB == null)
					return;
				Packed.Add(S.ID, S.Area);
				AddSpots(S.ChildA);
				AddSpots(S.ChildB);
			};
			AddSpots(RootSpot);

			return Packed;
		}
	}
}