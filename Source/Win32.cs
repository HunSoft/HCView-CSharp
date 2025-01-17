using System;
using System.Runtime.InteropServices;

namespace HC.Win32
{	 
	public struct RECT 
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

        public RECT(int aLeft, int aTop, int aRight, int aBottom)
        {
            Left = aLeft;
            Top = aTop;
            Right = aRight;
            Bottom = aBottom;
        }

        public POINT TopLeft()
        {
            return new POINT(Left, Top);
        }

        public void SetWidth(int value)
        {
            Right = Left + value;
        }

        public int Width
        {
            get { return Right - Left; }
            set { SetWidth(value); }
        }

        public void SetHeight(int value)
        {
            Bottom = Top + value;
        }

        public int Height
        {
            get { return Bottom - Top; }
            set { SetHeight(value); }
        }

        public void Offset(int x, int y)
        {
            Left += x;
            Top += y;
            Right += x;
            Bottom += y;
        }

        public void Inflate(int x, int y)
        {
            Left -= x;
            Right += x;
            Top -= y;
            Bottom += y;
        }
	}

    [StructLayout(LayoutKind.Sequential)]//ȷ��MoveToEx�ֶ�˳��
	public struct POINT 
	{
		public int X;
		public int Y;

        public POINT(int ax, int ay)
        {
            X = ax;
            Y = ay;
        }

        public void Offset(int x, int y)
        {
            X += x;
            Y += y;
        }
	}

	public struct SIZE 
	{
		public int cx;
		public int cy;

        public SIZE(int x, int y)
        {
            cx = x;
            cy = y;
        }
	}
	public struct FILETIME 
	{
		public int dwLowDateTime;
		public int dwHighDateTime;
	}
	public struct SYSTEMTIME 
	{
		public short wYear;
		public short wMonth;
		public short wDayOfWeek;
		public short wDay;
		public short wHour;
		public short wMinute;
		public short wSecond;
		public short wMilliseconds;
	}
}

