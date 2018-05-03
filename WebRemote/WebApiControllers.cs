using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Windows.Forms;

namespace WebRemote
{
    public class ScreenCaptureController : ApiController
    {
        public static ScreenRecorder Recorder = null; 

        public HttpResponseMessage Get(int lastKnownFrame)
        {
            var frames = Recorder.Frames;

            List<Region> regions = new List<Region>();

            if(frames.FirstOrDefault(f=> f.Number == lastKnownFrame) != null)
            {
                frames.FindAll(f => f.Number > lastKnownFrame).ForEach(f =>
                {
                    f.Regions.ForEach(r =>
                    {
                        regions.Add(r);
                    });
                    
                });
            }
            else
            {
                regions.Add(new Region(0, 0, Recorder.OutputWidth, Recorder.OutputHeight));
            }

            Recorder.NormalizeRegions(regions);

            Frame lastFrame = frames.Last();
            using (Bitmap lastImage = new Bitmap(lastFrame.Image))
            using (var response = new MemoryStream())
            {
                foreach (var region in regions)
                {
                    using (var bmp = new Bitmap(region.Width, region.Height))
                    using (var graph = Graphics.FromImage(bmp))
                    using (var ms = new MemoryStream())
                    {
                        graph.DrawImage(lastImage, new Rectangle(0, 0, bmp.Width, bmp.Height),
                            new Rectangle(region.X1, region.Y1, region.Width, region.Height), GraphicsUnit.Pixel);

                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                        var array = ms.ToArray();
                        response.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(lastFrame.Number)), 0, 4);
                        response.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(array.Length)), 0, 4);
                        response.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(region.X1)), 0, 4);
                        response.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(region.Y1)), 0, 4);
                        response.Write(array, 0, array.Length);
                    }

                }

                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(response.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");// "application/octet-stream"
                return result;
            }



            /*
            using (MemoryStream ms = new MemoryStream())
            {
                lastFrame.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(ms.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");// "application/octet-stream"
                return result;
            }
            */
        }
    }
}
