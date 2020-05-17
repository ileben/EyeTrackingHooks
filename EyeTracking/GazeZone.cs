using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace EyeTrackingHooks
{
	public struct GazeZone
	{
		Point count;
		Point position;

		public GazeZone(int zoneCountX, int zoneCountY, Point screenPosition)
		{
			count = new Point(zoneCountX, zoneCountY);

			Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
			int zoneSizeX = screenBounds.Width / zoneCountX;
			int x = (screenPosition.X - screenBounds.Left) / zoneSizeX;

			int zoneSizeY = screenBounds.Height / zoneCountY;
			int y = (screenPosition.Y - screenBounds.Top) / zoneSizeY;

			position = new Point(x, y);
		}

		public bool IsOnLeftEdge()
		{
			return position.X == 0;
		}

		public bool IsOnRightEdge()
		{
			return position.X == count.X - 1;
		}

		public bool IsOnTopEdge()
		{
			return position.Y == 0;
		}

		public bool IsOnBottomEdge()
		{
			return position.Y == count.Y - 1;
		}

		public bool IsOnEdge()
		{
			return IsOnLeftEdge() || IsOnRightEdge() || IsOnTopEdge() || IsOnBottomEdge();
		}

		public int GetHorizontalEdgeSign()
		{
			if (IsOnLeftEdge())
				return -1;
			if (IsOnRightEdge())
				return +1;
			return 0;
		}

		public int GetVerticalEdgeSign()
		{
			if (IsOnTopEdge())
				return -1;
			if (IsOnBottomEdge())
				return +1;
			return 0;
		}

		public Point GetEdgeSign()
		{
			return new Point(GetHorizontalEdgeSign(), GetVerticalEdgeSign());
		}
	}
}
