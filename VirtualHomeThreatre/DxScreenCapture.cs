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
using System.Threading;

namespace VirtualHomeThreatre
{
	public partial class DxScreenCapture : Form
	{
		Device d;
		Surface CapturedSurface;
		GraphicsStream Graphics;
		public int Texture = -1;
		int FrameBuffer;
		int RenderBuffer;

		public DxScreenCapture()
		{
			//InitializeComponent(); //Don't need, we're only a form to get a handle for directX - I think this is cheating/wrong, there's got to be a better way.

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
			//First time Genesis
			if (Texture == -1) {
				Texture = GL.GenTexture();

				GL.BindTexture(TextureTarget.Texture2D, Texture);

				bool LinearFilter = false;
				if (LinearFilter) {
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear); //Smooth
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				} else {
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest); //Pixely
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				}

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height,
					0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

				GL.GenFramebuffers(1, out FrameBuffer);
				GL.BindFramebuffer(FramebufferTarget.FramebufferExt, FrameBuffer);
				GL.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, Texture, 0);
				//GL.GenRenderbuffers(1, out RenderBuffer);
				//GL.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, RenderBuffer);
				//GL.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.

				var x = GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);
				if (x == FramebufferErrorCode.FramebufferCompleteExt) {
					Console.WriteLine("good");
				} else {
					throw new Exception("Looks like your graphics card doesn't support frame buffers...");
				}

			}

			GL.BindFramebuffer(FramebufferTarget.FramebufferExt, FrameBuffer);
			//GL.ClearColor(Color.FromArgb(0, 0, 0, 0));
			//GL.ClearDepth(1.0f);
			//GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			//GL.Viewport(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
			//GL.MatrixMode(MatrixMode.Projection);
			//GL.LoadIdentity();
			//GL.Ortho(0, Screen.PrimaryScreen.Bounds.Width, 0, Screen.PrimaryScreen.Bounds.Height, -1, 1);
			//GL.MatrixMode(MatrixMode.Modelview);
			//GL.LoadIdentity();
			//GL.Disable(EnableCap.Texture2D);
			//GL.Disable(EnableCap.Blend);
			//GL.Enable(EnableCap.DepthTest);
			unsafe {
				GL.DrawPixels(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, 
					OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)Graphics.InternalDataPointer);
			}
			GL.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);

			return Texture;
		}
	}
}
