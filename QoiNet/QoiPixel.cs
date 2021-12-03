using System.Runtime.InteropServices;

namespace QoiNet
{
	[StructLayout(LayoutKind.Explicit)]
	public struct QoiPixel
	{
		public byte R
		{
			get => _r;
			set => _r = value;
		}

		public byte G
		{
			get => _g;
			set => _g = value;
		}

		public byte B
		{
			get => _b;
			set => _b = value;
		}

		public byte A
		{
			get => _a;
			set => _a = value;
		}

		public int Value
		{
			get => _value;
			set => _value = value;
		}
		
		[FieldOffset(0)]
		private byte _r;
		[FieldOffset(1)]
		private byte _g;
		[FieldOffset(2)]
		private byte _b;
		[FieldOffset(3)]
		private byte _a;
		
		[FieldOffset(0)]
		private int _value;

		public QoiPixel(byte r, byte g, byte b, byte a)
		{
			_value = 0;
			
			_r = r;
			_g = g;
			_b = b;
			_a = a;
		}
	}
}