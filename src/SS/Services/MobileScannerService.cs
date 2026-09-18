using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QRCoder;
using SS.Data;

namespace SS.Services
{
    public class MobileScannerService : IDisposable
    {
        private TcpListener _listener;
        private readonly ConcurrentQueue<string> _barcodeQueue = new();
        private readonly ConcurrentDictionary<string, DateTime> _recentBarcodes = new();
        private CancellationTokenSource? _cts;
        private bool _running;
        private int _port;
        private string _localIp;
        private readonly SqliteProductRepository? _productRepo;

        public event Action<string>? BarcodeReceived;

        public string Url => $"http://{_localIp}:{_port}";
        public string LocalIp => _localIp;
        public int Port => _port;
        public bool IsRunning => _running;
        public byte[]? QrImageBytes { get; private set; }
        public bool HasBarcodes() => !_barcodeQueue.IsEmpty;
        public string Mode { get; set; } = "inventory";

        public MobileScannerService(string? dbPath = null)
        {
            _localIp = GetLocalIp();
            _listener = new TcpListener(IPAddress.Any, 0);
            _listener.Start();
            _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            if (dbPath != null)
                _productRepo = new SqliteProductRepository(dbPath);
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _cts = new CancellationTokenSource();

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(Url, QRCodeGenerator.ECCLevel.M);
            using var pngQr = new PngByteQRCode(qrData);
            QrImageBytes = pngQr.GetGraphic(20);

            Task.Run(() => AcceptConnections(_cts.Token));
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _listener?.Stop();
        }

        public string? DequeueBarcode() =>
            _barcodeQueue.TryDequeue(out var barcode) ? barcode : null;

        private async Task AcceptConnections(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client), ct);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[8192];
                var sb = new StringBuilder();
                int bytesRead;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                } while (stream.DataAvailable);

                var request = sb.ToString();
                var lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) return;

                var requestLine = lines[0].Split(' ');
                var method = requestLine[0];
                var path = requestLine.Length > 1 ? requestLine[1] : "/";

                string body = "";
                var bodyIndex = request.IndexOf("\r\n\r\n");
                if (bodyIndex >= 0)
                    body = request.Substring(bodyIndex + 4);

                if (method == "GET" && path == "/")
                    await SendHtml(stream);
                else if (method == "GET" && path == "/qr")
                    await SendQr(stream);
                else if (method == "GET" && path == "/zxing.min.js")
                    await SendZxingJs(stream);
                else if (method == "POST" && path == "/scan")
                    await HandleScan(stream, body);
                else if (method == "POST" && path == "/check")
                    await HandleCheck(stream, body);
                else if (method == "POST" && path == "/products")
                    await HandleProducts(stream, body);
                else if (method == "GET" && path == "/poll")
                    await HandlePoll(stream);
                else
                    await SendResponse(stream, 404, "text/plain", "Not Found");
            }
            catch { }
            finally { client.Close(); }
        }

        private async Task SendHtml(NetworkStream stream) =>
            await SendResponse(stream, 200, "text/html; charset=utf-8", HtmlPage);

        private async Task SendQr(NetworkStream stream)
        {
            var qrUrl = $"{Url}?mode={Mode}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.M);
            using var pngQr = new PngByteQRCode(qrData);
            var qrBytes = pngQr.GetGraphic(20);
            await SendBytes(stream, 200, "image/png", qrBytes);
        }

        private static byte[]? _zxCache;
        private async Task SendZxingJs(NetworkStream stream)
        {
            if (_zxCache == null)
            {
                var path = Path.Combine(AppContext.BaseDirectory, "www", "zxing.min.js");
                if (File.Exists(path))
                    _zxCache = await File.ReadAllBytesAsync(path);
                else
                { await SendResponse(stream, 404, "text/plain", "Not Found"); return; }
            }
            await SendBytes(stream, 200, "application/javascript", _zxCache);
        }

        private async Task HandleScan(NetworkStream stream, string body)
        {
            try
            {
                if (body.StartsWith("ADD:"))
                {
                    var json = body.Substring(4);
                    var code = $"ADD:{json}";
                    if (IsDuplicate(code)) { await SendResponse(stream, 200, "application/json", "{\"ok\":true,\"dup\":true}"); return; }
                    _barcodeQueue.Enqueue(code);
                    BarcodeReceived?.Invoke(code);
                    await SendResponse(stream, 200, "application/json", "{\"ok\":true}");
                }
                else if (body.StartsWith("CART:"))
                {
                    var idStr = body.Substring(5).Trim();
                    if (int.TryParse(idStr, out var productId) && _productRepo != null)
                    {
                        var product = _productRepo.GetById(productId);
                        if (product != null)
                        {
                            var code = $"SALES:{product.Barcode}";
                            if (!IsDuplicate(code))
                            {
                                _barcodeQueue.Enqueue(code);
                                BarcodeReceived?.Invoke(code);
                            }
                            await SendResponse(stream, 200, "application/json", "{\"ok\":true}");
                            return;
                        }
                    }
                    await SendResponse(stream, 200, "application/json", "{\"ok\":false}");
                }
                else
                {
                    var barcode = body.Trim().Trim('"');
                    if (IsDuplicate(barcode)) { await SendResponse(stream, 200, "application/json", "{\"ok\":true,\"dup\":true}"); return; }

                    if (Mode == "sales" && _productRepo != null)
                    {
                        var product = _productRepo.GetByBarcode(barcode);
                        if (product != null)
                        {
                            var code = $"SALES:{barcode}";
                            _barcodeQueue.Enqueue(code);
                            BarcodeReceived?.Invoke(code);
                            await SendResponse(stream, 200, "application/json", "{\"ok\":true,\"added\":\"" + product.Name + "\"}");
                            return;
                        }
                    }

                    _barcodeQueue.Enqueue(barcode);
                    BarcodeReceived?.Invoke(barcode);
                    await SendResponse(stream, 200, "application/json", "{\"ok\":true}");
                }
            }
            catch
            {
                await SendResponse(stream, 400, "application/json", "{\"error\":\"bad request\"}");
            }
        }

        private async Task HandleCheck(NetworkStream stream, string body)
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var barcode = root.GetProperty("barcode").GetString() ?? "";
                var mode = root.TryGetProperty("mode", out var m) ? m.GetString() ?? "inventory" : "inventory";

                if (_productRepo != null)
                {
                    var product = _productRepo.GetByBarcode(barcode);
                    if (product != null)
                    {
                        var json = JsonSerializer.Serialize(new { exists = true, name = product.Name, price = product.Price, stock = product.Stock });
                        await SendResponse(stream, 200, "application/json", json);
                        return;
                    }
                }
                await SendResponse(stream, 200, "application/json", "{\"exists\":false}");
            }
            catch
            {
                await SendResponse(stream, 200, "application/json", "{\"exists\":false}");
            }
        }

        private async Task HandleProducts(NetworkStream stream, string body)
        {
            try
            {
                var query = body.Trim().Trim('"').ToLowerInvariant();
                if (_productRepo == null)
                {
                    await SendResponse(stream, 200, "application/json", "[]");
                    return;
                }

                var all = _productRepo.GetAll();
                var filtered = string.IsNullOrEmpty(query)
                    ? all.Take(20).ToList()
                    : all.Where(p => p.Name.ToLower().Contains(query) || p.Barcode.ToLower().Contains(query)).Take(20).ToList();

                var items = filtered.Select(p => new { id = p.Id, name = p.Name, barcode = p.Barcode, price = p.Price, stock = p.Stock });
                var json = JsonSerializer.Serialize(items);
                await SendResponse(stream, 200, "application/json", json);
            }
            catch
            {
                await SendResponse(stream, 200, "application/json", "[]");
            }
        }

        private async Task HandlePoll(NetworkStream stream)
        {
            if (_barcodeQueue.TryDequeue(out var code))
                await SendResponse(stream, 200, "application/json", $"\"{EscapeJson(code)}\"");
            else
                await SendResponse(stream, 204, "application/json", "null");
        }

        private bool IsDuplicate(string barcode)
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _recentBarcodes)
                if ((now - kv.Value).TotalSeconds > 5)
                    _recentBarcodes.TryRemove(kv.Key, out _);
            if (_recentBarcodes.ContainsKey(barcode)) return true;
            _recentBarcodes[barcode] = now;
            return false;
        }

        private static async Task SendResponse(NetworkStream stream, int status, string contentType, string body)
        {
            var statusText = status switch { 200 => "OK", 204 => "No Content", 400 => "Bad Request", 404 => "Not Found", _ => "OK" };
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var resp = $"HTTP/1.1 {status} {statusText}\r\nContent-Type: {contentType}\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\nAccess-Control-Allow-Origin: *\r\n\r\n";
            var headerBytes = Encoding.UTF8.GetBytes(resp);
            var combined = new byte[headerBytes.Length + bodyBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, combined, 0, headerBytes.Length);
            Buffer.BlockCopy(bodyBytes, 0, combined, headerBytes.Length, bodyBytes.Length);
            await stream.WriteAsync(combined, 0, combined.Length);
        }

        private static async Task SendBytes(NetworkStream stream, int status, string contentType, byte[] data)
        {
            var header = $"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {data.Length}\r\nConnection: close\r\nAccess-Control-Allow-Origin: *\r\n\r\n";
            var headerBytes = Encoding.UTF8.GetBytes(header);
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private static string GetLocalIp()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 80);
                return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
            }
            catch { return "127.0.0.1"; }
        }

        private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private const string HtmlPage = @"<!DOCTYPE html>
<html lang=""es""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no""><title>Safe Sale - Escaner</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,sans-serif;background:#1a1a2e;color:#e0e0e0;min-height:100vh;display:flex;flex-direction:column;align-items:center;padding:10px}
h1{color:#CE93D8;margin:10px 0 5px;font-size:1.3em}
.qr-wrap{margin:8px 0}
.qr-wrap img{width:140px;height:140px;border:2px solid #4A148C;border-radius:8px;background:#fff}
.tabs{display:flex;gap:4px;margin:10px 0;width:100%;max-width:400px}
.tab{flex:1;padding:10px;border:none;border-radius:8px;cursor:pointer;font-size:1em;font-weight:600;background:#2a2a3e;color:#999;transition:.2s}
.tab.active{background:#4A148C;color:#fff}
.tab-content{width:100%;max-width:400px;display:none}
.tab-content.active{display:block}
.camera-btn{width:100%;padding:14px;border:none;border-radius:8px;background:#4A148C;color:#fff;font-size:1.1em;cursor:pointer;margin:8px 0;font-weight:600}
.camera-btn:active{background:#6A1B9A}
.form-group{margin:8px 0}
.form-group label{display:block;color:#CE93D8;font-size:.85em;margin-bottom:3px}
.form-group input{width:100%;padding:10px;border:1px solid #3a3a5e;border-radius:8px;background:#16213e;color:#fff;font-size:1em}
.form-group input::placeholder{color:#666}
.send-btn{width:100%;padding:12px;border:none;border-radius:8px;background:#1B5E20;color:#fff;font-size:1em;cursor:pointer;margin:8px 0;font-weight:600}
.save-btn{width:100%;padding:12px;border:none;border-radius:8px;background:#E65100;color:#fff;font-size:1em;cursor:pointer;margin:8px 0;font-weight:600}
.status{padding:6px;border-radius:6px;margin:6px 0;font-size:.85em;text-align:center;min-height:28px}
.status.ok{background:#1B5E20;color:#A5D6A7}
.status.dup{background:#4E342E;color:#BCAAA4}
.status.warn{background:#E65100;color:#FFCC80}
.status.info{background:#1a237e;color:#9FA8DA}
.finput{position:absolute;left:-9999px}
canvas{display:none}
.prod-item{display:flex;align-items:center;justify-content:space-between;padding:8px;margin:3px 0;background:#16213e;border-radius:6px;border:1px solid #2a2a4e}
.prod-item .info{flex:1;min-width:0}
.prod-item .name{color:#e0e0e0;font-size:.9em;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.prod-item .meta{color:#999;font-size:.75em}
.prod-item .price{color:#CE93D8;font-size:.85em;font-weight:600;margin:0 8px}
.prod-item .add-btn{padding:6px 10px;border:none;border-radius:5px;background:#1B5E20;color:#fff;font-size:.8em;cursor:pointer;font-weight:600;white-space:nowrap}
.prod-item .add-btn:active{background:#2E7D32}
.prod-item .add-btn.added{background:#4E342E;color:#BCAAA4}
</style></head><body>
<h1>Escáner Móvil</h1>
<div class=""qr-wrap""><img id=""qrImg"" alt=""QR"" /></div>
<div class=""tabs"">
<button class=""tab active"" onclick=""switchTab(0)"">Carrito</button>
<button class=""tab"" onclick=""switchTab(1)"">Nuevo</button>
</div>
<div class=""tab-content active"" id=""tab0"">
<video id=""vid0"" autoplay playsinline muted style=""width:100%;border-radius:8px;display:none;max-height:260px;object-fit:cover""></video>
<input type=""file"" accept=""image/*"" capture=""environment"" class=""finput"" id=""fi0"" onchange=""onFile(event,0)""/>
<button class=""camera-btn"" id=""btn0"" onclick=""tapCam(0)"">Escanear en Vivo</button>
<label id=""fb0"" for=""fi0"" style=""width:100%;padding:12px;border-radius:8px;background:#4A148C;color:#fff;font-size:1em;cursor:pointer;text-align:center;margin:4px 0;font-weight:600;display:block"">Tomar Foto del Código</label>
<div class=""form-group""><label>Código</label><input type=""text"" id=""bar0"" placeholder=""Código de barras"" readonly/></div>
<div class=""status"" id=""st0""></div>
<div style=""margin-top:10px;border-top:1px solid #3a3a5e;padding-top:10px"">
<div class=""form-group""><label>Buscar producto existente</label><input type=""text"" id=""searchProd"" placeholder=""Nombre o código..."" oninput=""searchProducts()""/></div>
<div id=""prodList"" style=""max-height:200px;overflow-y:auto""></div>
</div>
</div>
<div class=""tab-content"" id=""tab1"">
<video id=""vid1"" autoplay playsinline muted style=""width:100%;border-radius:8px;display:none;max-height:260px;object-fit:cover""></video>
<input type=""file"" accept=""image/*"" capture=""environment"" class=""finput"" id=""fi1"" onchange=""onFile(event,1)""/>
<button class=""camera-btn"" id=""btn1"" onclick=""tapCam(1)"">Escanear en Vivo</button>
<label id=""fb1"" for=""fi1"" style=""width:100%;padding:12px;border-radius:8px;background:#4A148C;color:#fff;font-size:1em;cursor:pointer;text-align:center;margin:4px 0;font-weight:600;display:block"">Tomar Foto del Código</label>
<div class=""form-group""><label>Código</label><input type=""text"" id=""bar1"" placeholder=""Código de barras""/></div>
<div class=""form-group""><label>Nombre</label><input type=""text"" id=""nameIn"" placeholder=""Ej: Leche entera""/></div>
<div class=""form-group""><label>Precio</label><input type=""number"" id=""priceIn"" placeholder=""0.00"" step=""0.01"" min=""0""/></div>
<div class=""form-group""><label>Stock Inicial</label><input type=""number"" id=""stockIn"" placeholder=""0"" min=""0""/></div>
<div class=""form-group""><label>Stock Mínimo (alerta)</label><input type=""number"" id=""minStockIn"" placeholder=""5"" min=""0""/></div>
<button class=""save-btn"" onclick=""sendNew()"">Guardar Nuevo</button>
<div class=""status"" id=""st1""></div>
</div>
<canvas id=""cvs""></canvas>
<script src=""/zxing.min.js""></script>
<script>
var liveStream=null,scanning=false,lastSent=0,detector=null,curTab=0;
function switchTab(i){curTab=i;document.querySelectorAll('.tab').forEach((t,j)=>t.classList.toggle('active',j===i));document.querySelectorAll('.tab-content').forEach((t,j)=>t.classList.toggle('active',j===i))}
function stat(i,c,m){var e=document.getElementById('st'+i);e.className='status '+c;e.textContent=m;if(c!=='ok'&&c!=='dup'&&c!=='info')setTimeout(function(){e.textContent='';},5000);}
function tapCam(i){
if(liveStream){stopLive();return;}
if(navigator.mediaDevices&&navigator.mediaDevices.getUserMedia){
stat(i,'info','Abriendo cámara en vivo...');
navigator.mediaDevices.getUserMedia({video:{facingMode:'environment'}})
.then(function(s){
liveStream=s;
var v=document.getElementById('vid'+i);
v.srcObject=s;
v.style.display='block';
document.getElementById('btn'+i).textContent='Detener';
stat(i,'info','Cámara abierta. Apunta al código...');
scanning=true;
scanLoopLive(i,v);
}).catch(function(e){
console.log('Camera error:',e);
stat(i,'info','Cámara no disponible. Usa la foto.');
showFileBtn(i);
});
}else{
stat(i,'info','Cámara en vivo no soportada. Toma una foto.');
showFileBtn(i);
}
}
function scanLoopLive(i,v){
if(!scanning||!liveStream)return;
var d=null;
try{d=new BarcodeDetector({formats:['ean_13','ean_8','upc_a','upc_e','code_128','code_39','codabar','qr_code']});}catch(e){}
if(d){
d.detect(v).then(function(r){
if(r&&r.length>0){
var val=r[0].rawValue;
var now=Date.now();
if(val&&now-lastSent>2000){
lastSent=now;
document.getElementById('bar'+i).value=val;
stat(i,'ok','\u2713 Detectado: '+val);
send(i);
}
}
}).catch(function(){}).then(function(){
if(scanning)setTimeout(function(){scanLoopLive(i,v);},200);
});
}else{
var cvs=document.getElementById('cvs');
var vw=v.videoWidth||640;
var vh=v.videoHeight||480;
cvs.width=vw;cvs.height=vh;
var ctx=cvs.getContext('2d',{willReadFrequently:true});
try{
ctx.drawImage(v,0,0,vw,vh);
var id=ctx.getImageData(0,0,vw,vh);
var src=new ZXing.RGBLuminanceSource(id.data,vw,vh);
var bn=new ZXing.HybridBinarizer(src);
var bm=new ZXing.BinaryBitmap(bn);
var res=new ZXing.MultiFormatReader().decode(bm);
var val=res.getText();
if(val){
var now=Date.now();
if(now-lastSent>2000){
lastSent=now;
document.getElementById('bar'+i).value=val;
stat(i,'ok','\u2713 Detectado: '+val);
send(i);
}
}
}catch(e){}
if(scanning)setTimeout(function(){scanLoopLive(i,v);},300);
}
}
function stopLive(){
if(liveStream){liveStream.getTracks().forEach(function(t){t.stop()});liveStream=null;}
scanning=false;
document.getElementById('vid0').style.display='none';
document.getElementById('vid1').style.display='none';
document.getElementById('btn0').textContent='Escanear Código';
document.getElementById('btn1').textContent='Escanear Código';
}
function onFile(e,i){
var f=e.target.files[0];if(!f)return;
stat(i,'info','Procesando imagen...');
e.target.value='';
var reader=new FileReader();
reader.onload=function(ev){
var img=new Image();
img.onload=function(){
var c=document.getElementById('cvs');
var maxW=1280;
var w=img.naturalWidth,h=img.naturalHeight;
if(w>maxW){h=h*maxW/w;w=maxW;}
c.width=w;c.height=h;
var ctx=c.getContext('2d');
ctx.drawImage(img,0,0,w,h);
stat(i,'info','Buscando código...');
setTimeout(function(){decodeImg(c,i);},100);
};
img.src=ev.target.result;
};
reader.readAsDataURL(f);
}
function decodeImg(c,i){
var d=null;
try{d=new BarcodeDetector({formats:['ean_13','ean_8','upc_a','upc_e','code_128','code_39','codabar','qr_code']});}catch(e){}
if(d){
d.detect(c).then(function(r){
if(r&&r.length>0){
gotCode(r[0].rawValue,i);
}else{
zxDecode(c,i);
}
}).catch(function(){zxDecode(c,i);});
}else{zxDecode(c,i);}
}
function zxDecode(c,i){
try{
var luminanceSource=new ZXing.HTMLCanvasElementLuminanceSource(c);
var binarizer=new ZXing.HybridBinarizer(luminanceSource);
var bitmap=new ZXing.BinaryBitmap(binarizer);
var reader=new ZXing.MultiFormatReader();
var result=reader.decode(bitmap);
gotCode(result.getText(),i);
}catch(e){
console.log('ZXing error:',e);
try{
var ctx=c.getContext('2d');
var id=ctx.getImageData(0,0,c.width,c.height);
var src=new ZXing.RGBLuminanceSource(id.data,c.width,c.height);
var bn=new ZXing.HybridBinarizer(src);
var bm=new ZXing.BinaryBitmap(bn);
var res=new ZXing.MultiFormatReader().decode(bm);
gotCode(res.getText(),i);
}catch(e2){
console.log('ZXing fallback error:',e2);
stat(i,'warn','No se detectó. Asegúrate que el código sea visible y nitido.');
}
}
}
function gotCode(val,i){
document.getElementById('bar'+i).value=val;
stat(i,'ok','\u2713 Código: '+val);
var urlParams=new URLSearchParams(window.location.search);
var mode=urlParams.get('mode')||'inventory';
fetch('/check',{method:'POST',body:JSON.stringify({barcode:val,mode:mode}),headers:{'Content-Type':'application/json'}})
.then(function(r){return r.json();})
.then(function(j){
if(mode==='sales'){
if(j.exists){
stat(i,'ok','\u2713 '+j.name+' - Agregado al carrito');
send(i);
}else{
stat(i,'warn','Producto no encontrado: '+val+'. Agrégalo primero desde Inventario.');
}
}else{
if(j.exists){
stat(i,'ok','\u2713 '+j.name+' ya existe (Stock: '+j.stock+')');
}else{
switchTab(1);
document.getElementById('bar1').value=val;
stat(1,'info','Producto nuevo. Completa los datos.');
}
}
}).catch(function(){
send(i);
});
}
function send(i){
var val=document.getElementById('bar'+i).value.trim();
if(!val)return;
fetch('/scan',{method:'POST',body:val,headers:{'Content-Type':'text/plain'}})
.then(function(r){return r.json();})
.then(function(j){
if(j.dup){stat(i,'dup','Duplicado');}
else{stat(i,'ok','\u2713 Enviado: '+val);document.getElementById('bar'+i).value='';}
}).catch(function(){stat(i,'warn','Error de conexión');});
}
function sendNew(){
var c=document.getElementById('bar1').value.trim();
var n=document.getElementById('nameIn').value.trim();
var p=document.getElementById('priceIn').value;
var s=document.getElementById('stockIn').value||'0';
var ms=document.getElementById('minStockIn').value||'5';
if(!c||!n||!p){stat(1,'warn','Completa código, nombre y precio');return;}
fetch('/scan',{method:'POST',body:'ADD:'+JSON.stringify({barcode:c,name:n,price:parseFloat(p),stock:parseInt(s),min_stock:parseInt(ms)}),headers:{'Content-Type':'text/plain'}})
.then(function(r){return r.json();})
.then(function(j){if(j.dup)stat(1,'dup','Duplicado');else{stat(1,'ok','\u2713 Guardado: '+n);document.getElementById('bar1').value='';document.getElementById('nameIn').value='';document.getElementById('priceIn').value='';document.getElementById('stockIn').value='';document.getElementById('minStockIn').value='';}})
.catch(function(){stat(1,'warn','Error de conexión');});
}
var searchTimer=null;
function searchProducts(){
clearTimeout(searchTimer);
searchTimer=setTimeout(function(){
var q=document.getElementById('searchProd').value.trim();
fetch('/products',{method:'POST',body:JSON.stringify(q),headers:{'Content-Type':'application/json'}})
.then(function(r){return r.json();})
.then(function(items){
var el=document.getElementById('prodList');
if(!items||items.length===0){el.innerHTML='<div style=""text-align:center;color:#666;padding:8px;font-size:.85em"">No se encontraron productos</div>';return;}
var h='';
items.forEach(function(p){
h+='<div class=""prod-item""><div class=""info""><div class=""name"">'+p.name+'</div><div class=""meta"">'+p.barcode+' | Stock: '+p.stock+'</div></div><div class=""price"">$'+p.price.toFixed(2)+'</div><button class=""add-btn"" onclick=""addToCart('+p.id+',this)"">Agregar</button></div>';
});
el.innerHTML=h;
}).catch(function(){});
},300);
}
function addToCart(productId,btn){
btn.textContent='...';
btn.disabled=true;
fetch('/scan',{method:'POST',body:'CART:'+productId,headers:{'Content-Type':'text/plain'}})
.then(function(r){return r.json();})
.then(function(j){
if(j.ok){btn.textContent='\u2713 Agregado';btn.className='add-btn added';}
else{btn.textContent='Error';btn.disabled=false;}
}).catch(function(){btn.textContent='Error';btn.disabled=false;});
}
fetch('/qr').then(function(r){return r.blob();}).then(function(b){document.getElementById('qrImg').src=URL.createObjectURL(b);}).catch(function(){});
</script></body></html>";
    }
}
