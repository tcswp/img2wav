using System;
using System.IO;
using System.Drawing;

namespace Image2Wave
{	
	class img2wav
	{
		// the output will be 44.1k 16-bit mono audio
		const int sampleRate = 44100;
		const short bitDepth = 16;
		const short numChannels = 1;
		
		static void CreateWaveFile(string relPath, short[] data)
		{
			int dataSize = data.Length * sizeof (short);
			using (BinaryWriter waveFile = new BinaryWriter(File.Open(relPath, FileMode.Create)))
			{
				// RIFF-WAVE header
				waveFile.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
				waveFile.Write(dataSize+32);						// wave chunk size
				waveFile.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
				
				// fmt sub-chunk
				waveFile.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));	
				waveFile.Write(16);									// size of fmt chunk
				waveFile.Write((short) 1);							// linear PCM audio format
				waveFile.Write(numChannels);						// number of channels
				waveFile.Write(sampleRate);
				waveFile.Write(sampleRate*bitDepth*numChannels/8);	// byte rate
				waveFile.Write((short)(bitDepth*numChannels/8));	// block align
				waveFile.Write(bitDepth);
				
				// data sub-chunk
				waveFile.Write(System.Text.Encoding.ASCII.GetBytes("data"));
				waveFile.Write(dataSize);
				for (int i = 0; i < data.Length; i++)
					waveFile.Write(data[i]);
			}
		}
		
		static short[] EncodeImage(Bitmap bitmap, int spp)
		{
			int width = bitmap.Size.Width;
			int height = bitmap.Size.Height;
			double[] data = new double[spp*width];
			short[] pcm16 = new short[spp*width];
			double maxAmp = 0;
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					Color pixel = bitmap.GetPixel(x,y);

					double amp = (double)(pixel.R+pixel.G+pixel.B)/765;

					if (amp == 0) continue;
					
					// scale height
					double freq = ((double)(height-y+1))/height*sampleRate/2;

					for (int t = 0; t < spp; t++)
					{
						double sample = amp*Math.Sin(2*Math.PI*freq/sampleRate*t); 
						data[x*spp+t] += sample;
						if (Math.Abs(data[x*spp+t]) > maxAmp)
							maxAmp = Math.Abs(data[x*spp+t]);
					}
				}
			}
			
			// make 16-bit
			for (int i = 0; i < width*spp; i++)
				pcm16[i] = (short)(data[i]/maxAmp*32767);
			
			return pcm16;
		}
		
		static void Main(string[] args)
		{
			string bitmapFile = args[0];
			//string filename = args[0].Split('.')[0]+".wav";
			string filename = args[1];
			
			Bitmap bitmap = new Bitmap(bitmapFile);
			
			int pps = Int32.Parse(args[2]);
			int spp = sampleRate/pps;
			
			Console.WriteLine("sampling frequency: {0}\n"
							+ "pixel rate: {1}\n"
							+ "samples per pixel: {2}", sampleRate,pps,spp);
						
			Console.WriteLine("\nencoding {0} to 16-bit WAV...", bitmapFile);
			
			short[] pcm16 = EncodeImage(bitmap, spp);
			CreateWaveFile(filename, pcm16);
		}
	}
}