using System;
using System.Buffers.Binary;
using System.IO;

namespace QoiNet
{
	public class QoiImage
	{
		public uint Width => _width;
		public uint Height => _height;
		public uint Channels => _channels;
		public uint Colorspace => _colorspace;

		private const uint Magic = ((uint) 'q' << 24) | ((uint) 'o' << 16) | ((uint) 'i' << 8) | 'f';

		private const byte TwoLeadingBitsMask   = 0b11000000;
		private const byte ThreeLeadingBitsMask = 0b11100000;
		private const byte FourLeadingBitsMask  = 0b11110000;
			
		private const byte QoiIndex  = 0b00000000;
		private const byte QoiRun8   = 0b01000000;
		private const byte QoiRun16  = 0b01100000;
		private const byte QoiDiff8  = 0b10000000;
		private const byte QoiDiff16 = 0b11000000;
		private const byte QoiDiff24 = 0b11100000;
		private const byte QoiColor  = 0b11110000;
		
		private readonly uint _width;
		private readonly uint _height;
		private readonly byte _channels;
		private readonly byte _colorspace;
		private readonly byte[] _pixels;

		private QoiImage(uint width, uint height, byte channels, byte colorspace, byte[] pixels)
		{
			_width = width;
			_height = height;
			_channels = channels;
			_colorspace = colorspace;
			_pixels = pixels;
		}
		
		public static void DecodeFromFile(string file) 
			=> Decode(File.ReadAllBytes(file));
		
		public static QoiImage Decode(ReadOnlySpan<byte> content)
		{
			if (BinaryPrimitives.ReadUInt32BigEndian(content) != Magic)
			{
				throw new InvalidMagicException();
			}

			uint width = BinaryPrimitives.ReadUInt32BigEndian(content);
			uint height = BinaryPrimitives.ReadUInt32BigEndian(content);
			byte channels = content[0];
			byte colorspace = content[1];
			
			content = content[2..];

			byte[] pixels = new byte[width * height * channels];
			
			QoiPixel pixel = new QoiPixel(0, 0, 0, 255);
			Span<QoiPixel> index = stackalloc QoiPixel[64];

			int run = 0;
			
			for (int pixelIndex = 0; pixelIndex < width * height * channels; pixelIndex++)
			{
				if (run > 0)
				{
					run--;
				}
				else
				{
					byte currentByte = content[0];
				
					content = content[1..];

					if ((currentByte & TwoLeadingBitsMask) == QoiIndex)
					{
						pixel = index[currentByte ^ QoiIndex];
					}
					else if ((currentByte & ThreeLeadingBitsMask) == QoiRun8)
					{
						run = (currentByte & 0x1F);
					}
					else if ((currentByte & ThreeLeadingBitsMask) == QoiRun16)
					{
						byte currentByte2 = content[0];

						content = content.Slice(1);
						
						run = (((currentByte & 0x1F) << 8) | (currentByte2)) + 32;
					}
					else if ((currentByte & TwoLeadingBitsMask) == QoiDiff8)
					{
						pixel.R += (byte) (((currentByte >> 4) & 0x03) - 2);
						pixel.G += (byte) (((currentByte >> 2) & 0x03) - 2);
						pixel.B += (byte) (( currentByte       & 0x03) - 2);
					}
					else if ((currentByte & ThreeLeadingBitsMask) == QoiDiff16)
					{
						byte currentByte2 = content[0];

						content = content[1..];
						
						pixel.R += (byte) ((currentByte  & 0x1f) - 16);
						pixel.G += (byte) ((currentByte2 >> 4)   - 8);
						pixel.B += (byte) ((currentByte2 & 0x0f) - 8);
					}
					else if ((currentByte & FourLeadingBitsMask) == QoiDiff24)
					{
						byte currentByte2 = content[0];
						byte currentByte3 = content[1];

						content = content[2..];

						pixel.R = (byte) ((((currentByte & 0x0F) << 1) | (currentByte2 >> 7)) - 16);
						pixel.G = (byte) (((currentByte2 & 0x7C) >> 2) - 16);
						pixel.B = (byte) ((((currentByte2 & 0x03) << 3) | ((currentByte3 & 0xE0) >> 5)) - 16);
						pixel.A = (byte) ((currentByte3 & 0x1F) - 16);
					}
					else if ((currentByte & FourLeadingBitsMask) == QoiColor)
					{
						if ((currentByte & 8) != 0)
						{
							pixel.R = content[0];
							
							content = content[1..];
						}
						
						if ((currentByte & 4) != 0)
						{
							pixel.G = content[0];
							
							content = content[1..];
						}
						
						if ((currentByte & 2) != 0)
						{
							pixel.B = content[0];
							
							content = content[1..];
						}
						
						if ((currentByte & 1) != 0)
						{
							pixel.A = content[0];
							
							content = content[1..];
						}
					}

					index[ColorHash(pixel) % 64] = pixel;
				}

				if (channels == 4)
				{
					pixels[pixelIndex] = pixel.R;
					pixels[pixelIndex + 1] = pixel.G;
					pixels[pixelIndex + 2] = pixel.B;
					pixels[pixelIndex + 3] = pixel.A;
				}
				else
				{
					pixels[pixelIndex] = pixel.R;
					pixels[pixelIndex + 1] = pixel.G;
					pixels[pixelIndex + 2] = pixel.B;
				}
			}
			
			return new QoiImage(width, height, channels, colorspace, pixels);
		}

		public QoiPixel GetPixel(int x, int y)
		{
			if (x >= Width)
			{
				throw new ArgumentOutOfRangeException();
			}
		
			if (y >= Height)
			{
				throw new ArgumentOutOfRangeException();
			}

			long pixelIndex = y * Width + x;

			return new QoiPixel(_pixels[pixelIndex], _pixels[pixelIndex + 1], _pixels[pixelIndex + 2], _pixels[pixelIndex + 3]);
		}

		public void SetPixel(int x, int y, QoiPixel pixel)
		{
			if (x >= Width)
			{
				throw new ArgumentOutOfRangeException();
			}
		
			if (y >= Height)
			{
				throw new ArgumentOutOfRangeException();
			}
			
			long pixelIndex = y * Width + x;

			_pixels[pixelIndex] = pixel.R;
			_pixels[pixelIndex] = pixel.G;
			_pixels[pixelIndex] = pixel.B;
			_pixels[pixelIndex] = pixel.A;
		}

		private static int ColorHash(QoiPixel pixel)
		{
			return pixel.R ^ pixel.G ^ pixel.B ^ pixel.A;
		}
	}
}