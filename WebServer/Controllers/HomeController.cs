using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebServer.Models;

namespace WebServer.Controllers {
    public class HomeController : Controller {
        private async Task Authenticate(string key) {
            var claims = new List<Claim> {
                new Claim(ClaimsIdentity.DefaultNameClaimType, key)
            };
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        //[Route("core")]
        //public IActionResult Core() {
        //    if (User.Identity.IsAuthenticated) {
        //        return File("~/js/app.js", "application/javascript");
        //    }
        //    return File("~/js/welcome.js", "application/javascript");
        //}

        [Authorize]
        [HttpGet("download")]
        public IActionResult DownloadFile(string file) {
            var gDir = Server.Config.Srv.DownloadFolder;
            file = Path.Combine(gDir, file);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(file));
            if (check.StartsWith("..")) {
                return StatusCode(404); // Backdoor detection
            }

            if (!System.IO.File.Exists(file)) {
                return StatusCode(404); // Not found
            }

            return PhysicalFile(Path.GetFullPath(file), "application/octet-stream", Path.GetFileName(file));
        }

        public class Chunk {
            //[FromForm(Name = "dztotalfilesize")]
            //public long TotalFileSize { get; set; }
            //[FromForm(Name = "dztotalchunkcount")]
            //public long TotalChunkCount { get; set; }
            //[FromForm(Name = "dzchunkindex")]
            //public long ChunkIndex { get; set; }
            //[FromForm(Name = "dzchunksize")]
            //public long ChunkSize { get; set; }
            //[FromForm(Name = "dzchunkbyteoffset")]
            //public long ChunkByteOffset { get; set; }

            [FromHeader(Name = "Upload-Length")]
            public long TotalFileSize { get; set; }
            [FromHeader(Name = "Upload-Name")]
            public string FileName { get; set; }
            [FromHeader(Name = "Upload-Offset")]
            public long Offset { get; set; }
            [FromHeader(Name = "Content-Type")]
            public string ContentType { get; set; }
        }

        public static string Base64EncodeObject(object obj) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.None));
            return Convert.ToBase64String(plainTextBytes);
        }

        public static T Base64DecodeObject<T>(string base64String) {
            var base64EncodedBytes = Convert.FromBase64String(base64String);
            return JsonConvert.DeserializeObject<T>(System.Text.Encoding.UTF8.GetString(base64EncodedBytes));
        }

        public class PatchQuery {
            public string Dir { get; set; }
            public string SocketID { get; set; }
            public string FileName { get; set; }
        }
        public class PatchTime {
            public PatchQuery Query;
            public DateTime Last;
        }
        public static Dictionary<string, PatchTime> Patches = new Dictionary<string, PatchTime>();

        [Authorize]
        [HttpPost("process")]
        public async Task<IActionResult> ProcessFile(string dir, string socketId, IFormFile file) {
            if (file == null) {
                var q = new PatchQuery { Dir = dir, SocketID = socketId, FileName = null };
                return Ok(Base64EncodeObject(q));
            }

            if (dir == null)
                dir = "";
            var oDir = dir;

            var gDir = Server.Config.Srv.DownloadFolder;
            dir = Path.Combine(gDir, dir);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(dir));
            if (check.StartsWith("..")) {
                return StatusCode(500); // Backdoor detection
            }

            if (!Directory.Exists(dir)) {
                return StatusCode(500); // Not found
            }

            var path = Path.Combine(dir, file.FileName);
            try {
                using (var fs = new FileStream(path, FileMode.Create)) {
                    await file.CopyToAsync(fs);
                }
            } catch (Exception) {
                System.IO.File.Delete(path);
                return StatusCode(500);
            }

            FolderBrowser.IsWaitApply = true;

            var client = Startup.Hub.Clients.Client(socketId);
            if (client != null) {
                FolderBrowser.SendFolder(Startup.Hub.Clients.Client(socketId), oDir);
            }

            return Ok(Base64EncodeObject(new PatchQuery { Dir = oDir, SocketID = socketId, FileName = file.FileName }));
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile() {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8)) {
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(body)) {
                    return StatusCode(500);
                }

                var q = Base64DecodeObject<PatchQuery>(body);
                var fname = q.FileName;
                lock (Patches) {
                    if (fname == null && Patches.ContainsKey(body)) {
                        fname = Patches[body].Query.FileName;
                        Patches.Remove(body);
                    }
                }

                var dir = q.Dir == null ? "" : q.Dir;
                var path = Path.Combine(dir, fname);
                FolderBrowser.RemoveFile(Startup.Hub.Clients.Client(q.SocketID), path, dir);
                return Ok();
            }
        }

        [Authorize]
        [HttpPatch("upload")]
        public async Task<IActionResult> UploadFile(Chunk chunk, string patch) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return StatusCode(403);

            var q = Base64DecodeObject<PatchQuery>(patch);
            q.FileName = chunk.FileName;
            var dir = q.Dir == null ? "" : q.Dir;
            var socketId = q.SocketID;

            if (dir == null)
                dir = "";
            var oDir = dir;

            lock (Patches) {
                if (!Patches.ContainsKey(patch)) {
                    Patches[patch] = new PatchTime { Query = q, Last = DateTime.UtcNow };
                } else {
                    Patches[patch].Last = DateTime.UtcNow;
                }
            }

            var gDir = Server.Config.Srv.DownloadFolder;
            dir = Path.Combine(gDir, dir);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(dir));
            if (check.StartsWith("..")) {
                return StatusCode(500); // Backdoor detection
            }

            if (!Directory.Exists(dir)) {
                return StatusCode(500); // Not found
            }

            var path = Path.Combine(dir, chunk.FileName);
            if (chunk.Offset == 0 && System.IO.File.Exists(path)) {
                System.IO.File.Delete(path);
            }

            try {
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8)) {
                    using (var fs = new FileStream(path, FileMode.Append)) {
                        await reader.BaseStream.CopyToAsync(fs);
                    }
                }
            } catch (Exception) {
                System.IO.File.Delete(path);
                return StatusCode(500);
            }

            FolderBrowser.IsWaitApply = true;

            var client = Startup.Hub.Clients.Client(socketId);
            if (client != null) {
                FolderBrowser.SendFolder(Startup.Hub.Clients.Client(socketId), oDir);
            }

            return StatusCode(200);
        }

        [Route("")]
        public IActionResult Index() {
            
            if (User.Identity.IsAuthenticated) {
                return View("Access");
            }
            return View();
        }

        [HttpPost("key")]
        public async Task<IActionResult> ApplyKey(string key) {
            if (key == Server.Config.Srv.WebKey) {
                await Authenticate(key);
                //Response.Cookies.Append("Access-Key", key);
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
