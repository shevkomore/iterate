
using ElectronNET.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text.Json;

namespace iterate.ui.data
{
    public class UIDataHandler<T>
    {
        string identifier;
        public List<BrowserWindow> Listeners = new List<BrowserWindow>();
        public UIDataHandler(string identifier)
        {
            this.identifier = identifier;
        }
        public void Update(T data)
        {
            /*Console.WriteLine("Sending item...");
            Console.WriteLine(data.ToString());
            try
            {
                var obj = JObject.FromObject(data, new Newtonsoft.Json.JsonSerializer()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
                Console.WriteLine(obj.ToString());
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            */
            foreach (BrowserWindow window in Listeners)
            {
                if (window != null)
                    Electron.IpcMain.Send(window, identifier, data);
            }
        }
        /*public void SendToView(WebView2 view, SynchronizationContext context)
        {
            context.Post(_ =>
            {
                view.ExecuteScriptAsync($"console.log('Loaded data:',{JsonConvert.SerializeObject(data)})").ContinueWith(r =>
                {
                    Console.WriteLine(r.Result);
                });
                view.ExecuteScriptAsync($"window.data.{identifier} = {JsonConvert.SerializeObject(data)}");
            }, null);
            
        }
        public void SendToViewWhenInitialized(WebView2 view, SynchronizationContext context)
        {
            view.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (!e.IsSuccess) return;
                view.CoreWebView2.DOMContentLoaded += (s2,e2) => SendToView(view, context);
            };
        }*/
    }
}
