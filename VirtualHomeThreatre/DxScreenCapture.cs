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
using System.Runtime.InteropServices;

namespace VirtualHomeThreatre
{
	public partial class DxScreenCapture : Form
	{
		Device d;
		Surface CapturedSurface;
		GraphicsStream Graphics;
		public int Texture = -1;

		int[] pboBuffers = new int[10]; //10 buffers

		int pboIndex = 0;
		int pboNextIndex
		{
			get
			{
				return (pboIndex + 1) % pboBuffers.Length;
			}
		}

		int ScreenLength
		{
			get
			{
				return Screen.PrimaryScreen.Bounds.Height * Screen.PrimaryScreen.Bounds.Width * 4;
			}
		}
		int ScreenWidth
		{
			get
			{
				return Screen.PrimaryScreen.Bounds.Width;
			}
		}
		int ScreenHeight
		{
			get
			{
				return Screen.PrimaryScreen.Bounds.Height;
			}
		}

		int SubScreenWidth = 640;
		int SubScreenHeight = 480;
		int SubScreenLength = 640 * 480 * 4;
		
		public DxScreenCapture()
		{
			//InitializeComponent(); //Don't need, we're only a form to get a handle for directX - I think this is cheating/wrong, there's got to be a better way.

			//INIT DIRECT X
			PresentParameters present_params = new PresentParameters();
			present_params.Windowed = true;
			present_params.SwapEffect = SwapEffect.Discard;
			d = new Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, present_params);

			//INIT OPENGL
			Texture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			CaptureScreen();

			unsafe {
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height,
					0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)Graphics.InternalDataPointer);
			}

			// create 2 pixel buffer objects, you need to delete them when program exits.
			// glBufferDataARB with NULL pointer reserves only memory space.
			GL.GenBuffers(pboBuffers.Length, pboBuffers);
			for (int i = 0; i < pboBuffers.Length; i++){
				GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboBuffers[i]);
				GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)ScreenLength, IntPtr.Zero, BufferUsageHint.StreamDraw);
			}
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
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

		//[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
		//public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

		internal int GetGLTex()
		{
			// bind the texture and PBO
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboBuffers[pboIndex]);

			// copy pixels from PBO to texture object
			// Use offset instead of ponter.
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

			// bind PBO to update pixel values
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboBuffers[pboNextIndex]);

			// map the buffer object into client's memory
			// Note that glMapBufferARB() causes sync issue.
			// If GPU is working with this buffer, glMapBufferARB() will wait(stall)
			// for GPU to finish its job. To avoid waiting (stall), you can call
			// first glBufferDataARB() with NULL pointer before glMapBufferARB().
			// If you do that, the previous data in PBO will be discarded and
			// glMapBufferARB() returns a new allocated pointer immediately
			// even if GPU is still working with the previous data.
			//GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)ScreenLength, IntPtr.Zero, BufferUsageHint.StreamDraw);
			
			//IntPtr ptr = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
			//GLubyte* ptr = (GLubyte*)glMapBufferARB(GL_PIXEL_UNPACK_BUFFER_ARB, GL_WRITE_ONLY_ARB);
			//if (ptr != IntPtr.Zero) {
				// update data directly on the mapped buffer
				unsafe {
					GL.BufferSubData(BufferTarget.PixelUnpackBuffer, IntPtr.Zero, (IntPtr)ScreenLength, (IntPtr)Graphics.InternalDataPointer);
					//CopyMemory(ptr, (IntPtr)Graphics.InternalDataPointer, (uint)ScreenLength);

				}
				//GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer); // release pointer to mapping buffer

				pboIndex = (pboIndex + 1) % pboBuffers.Length;
			//}

			// it is good idea to release PBOs with ID 0 after use.
			// Once bound with 0, all pixel operations behave normal ways.
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
			return Texture;
		}
	}
}
