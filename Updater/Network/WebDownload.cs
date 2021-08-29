using System.Net;
using System.Threading.Tasks;

namespace Updater.Network {
    public class WebDownload {
        public string Url { get; }
        public long Offset { get; set; }

        HttpWebRequest Request;
        HttpWebResponse Response;

        const int PartSize = 8096;

        public WebDownload(string url) {
            Url = url;
        }

        public WebDownload(string url, long offset) {
            Url = url;
            Offset = offset;
            Request = WebRequest.Create(Url) as HttpWebRequest;
            if (offset > 0) {
                Request.AddRange(offset);
            }
            Request.Timeout = 5000;
            Response = Request.GetResponse() as HttpWebResponse;
        }

        public void Dispose() {
            if (Response != null) {
                Response.Dispose();
            }
        }

        public void StartDownload(long offset = 0) {
            Offset = offset;
            Request = WebRequest.Create(Url) as HttpWebRequest;
            if (offset > 0) {
                Request.AddRange(offset);
            }
            Request.Timeout = 5000;
            Response = Request.GetResponse() as HttpWebResponse;
        }

        public int GetPart(byte[] buffer) {
            if (Response == null) return 0;
            return Response.GetResponseStream().Read(buffer, 0, PartSize);
        }

        public async Task<int> GetPartAsync(byte[] buffer) {
            if (Response == null) return 0;
            return await Response.GetResponseStream().ReadAsync(buffer, 0, PartSize);
        }

        public (byte[] Buffer, int Readed) GetPart() {
            if (Response == null) return (null, 0);
            var buff = new byte[8096];
            var readed = Response.GetResponseStream().Read(buff, 0, PartSize);
            if (readed == 0) {
                return (null, 0);
            }
            return (buff, readed);
        }

        public async Task<(byte[] Buffer, int Readed)> GetPartAsync() {
            if (Response == null) return (null, 0);
            var buff = new byte[8096];
            var readed = await Response.GetResponseStream().ReadAsync(buff, 0, PartSize);
            if (readed == 0) {
                return (null, 0);
            }
            return (buff, readed);
        }
    }
}
