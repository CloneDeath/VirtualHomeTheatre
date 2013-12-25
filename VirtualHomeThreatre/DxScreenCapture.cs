using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace VirtualHomeThreatre
{
	public partial class DxScreenCapture : Form
	{
		Device d;
		Surface CapturedSurface;
		GraphicsStream Graphics;
		int Texture = -1;

		public DxScreenCapture()
		{
			//InitializeComponent();

			PresentParameters present_params = new PresentParameters();
			present_params.Windowed = true;
			present_params.SwapEffect = SwapEffect.Discard;
			d = new Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, present_params);
		}

		public void CaptureScreen()
		{
			if (CapturedSurface != null) {
				CapturedSurface.Dispose();
			}
			if (Graphics != null) {
				Graphics.Dispose();
			}

			CapturedSurface = d.CreateOffscreenPlainSurface(Screen.PrimaryScreen.Bounds.Width,
						Screen.PrimaryScreen.Bounds.Height, Format.A8R8G8B8, Pool.Scratch);
			d.GetFrontBufferData(0, CapturedSurface);
			Graphics = CapturedSurface.LockRectangle(LockFlags.None);
		}

		internal int GetGLTex()
		{
			if (Texture == -1) {
				Texture = GL.GenTexture();
			}

			GL.BindTexture(TextureTarget.Texture2D, Texture);

			bool LinearFilter = false;
			if (LinearFilter) {
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear); //Smooth
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			} else {
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest); //Pixely
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			}

			unsafe {
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height,
						0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)Graphics.InternalDataPointer);
			}

			//GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			/*OpenTK.Graphics.Glu.Build2DMipmap(OpenTK.Graphics.TextureTarget.Texture2D,
				(int)PixelInternalFormat.Rgba, data.Width, data.Height, OpenTK.Graphics.PixelFormat.Bgra,
				 OpenTK.Graphics.PixelType.UnsignedByte, data.Scan0);*/
			return Texture;



			//Bitmap b = new Bitmap(rectangle.Width, rectangle.Height);

			//BitmapData data = b.LockBits(new Rectangle(new Point(0, 0), b.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			//unsafe {
			//    byte* ptr = (byte*)data.Scan0;
			//    for (int y = rectangle.Top; y < rectangle.Bottom; y++) {
			//        Graphics.Position = ((Screen.PrimaryScreen.Bounds.Width * y) + rectangle.Left) * 4;
			//        for (int x = rectangle.Left; x < rectangle.Right; x++) {
			//            Graphics.InternalBufferPointer
			//            int subx = x - rectangle.Left;
			//            int suby = y - rectangle.Top;
			//            ptr[((subx + (suby * rectangle.Width)) * 4) + 0] = (byte)Graphics.ReadByte(); //B
			//            ptr[((subx + (suby * rectangle.Width)) * 4) + 1] = (byte)Graphics.ReadByte(); //G
			//            ptr[((subx + (suby * rectangle.Width)) * 4) + 2] = (byte)Graphics.ReadByte(); //R
			//            ptr[((subx + (suby * rectangle.Width)) * 4) + 3] = (byte)Graphics.ReadByte(); //A
			//            //b.SetPixel(x - rectangle.Left, y - rectangle.Top, Color.FromArgb(Alpha, Red, Green, Blue));
			//        }
			//    }
			//}
			//b.UnlockBits(data);

			//return b;
		}

		//static int MakeTexture(Bitmap bitmap, bool LinearFilter)
		//{
		//    BitmapData data = bitmap.LockBits(
		//      new Rectangle(0, 0, bitmap.Width, bitmap.Height),
		//      ImageLockMode.ReadOnly,
		//      bitmap.PixelFormat);
		//    //Img.PixelFormat.Format32bppArgb);
		//    int tex = GL.GenTexture();

		//    GL.BindTexture(TextureTarget.Texture2D, tex);

		//    if (LinearFilter) {
		//        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear); //Smooth
		//        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
		//    } else {
		//        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest); //Pixely
		//        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
		//    }

		//    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Screen.PrimaryScreen.
		//    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
		//    OpenTK.Graphics.Glu.Build2DMipmap(OpenTK.Graphics.TextureTarget.Texture2D,
		//        (int)PixelInternalFormat.Rgba, data.Width, data.Height, OpenTK.Graphics.PixelFormat.Bgra,
		//         OpenTK.Graphics.PixelType.UnsignedByte, data.Scan0);
		//    bitmap.UnlockBits(data);
		//    return tex;
		//}
	}
}
